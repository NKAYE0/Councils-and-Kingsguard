using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SmallCouncils.Behaviors;
using SmallCouncils.Models;
using TaleWorlds.CampaignSystem;

namespace SmallCouncils.Services
{
    /// <summary>
    /// Feeds council appointments/removals into AIInfluence's DynamicEvents
    /// system as real, in-world events — so NPCs (via AIInfluence's own
    /// AI-driven statement generation) can become aware of and comment on
    /// who holds which council position, purely in the background. No UI
    /// changes in Small Councils itself.
    ///
    /// Two kinds of event, matching two different needs:
    /// - An "ongoing status" event, created on assignment, deliberately
    ///   long-lived (years, not days) rather than a normal news window —
    ///   this is what lets a council member bring up their position with
    ///   pride whenever the player is talking to them specifically, not
    ///   just in the days right after being appointed. Explicitly expired
    ///   early on unassignment.
    /// - A short-lived "unhappy" news event, created on unassignment (14
    ///   days — 2 weeks), so other NPCs can comment on the removal as
    ///   recent news. Explicitly expired early if the hero receives any new
    ///   assignment before it naturally lapses.
    ///
    /// Reduced-clutter design: every event Id is deterministic (kingdom +
    /// position + hero for the ongoing-status kind, just hero for the
    /// unhappy kind), so before creating anything we always check
    /// GetEventById first. If a matching event already exists (e.g. this
    /// hero previously held this exact position, or already has a pending
    /// unhappy event from an earlier removal), we update its Title/
    /// Description/ExpirationTime in place rather than calling AddEvent
    /// again. AddEvent is only ever called for a genuinely new event. This
    /// doesn't touch anything about how AIInfluence itself decides whether
    /// to show a notification — it just means we ask it to add something
    /// new far less often over a long playthrough with repeated council
    /// churn, which reduces however many notifications that produces
    /// without risking any change to AIInfluence's own behavior.
    ///
    /// AIInfluence is an entirely optional dependency, exactly like
    /// ROT/NavalDLC elsewhere in this mod — everything here is done via
    /// runtime reflection against AIInfluence's assembly rather than a
    /// compile-time reference, so Small Councils builds and runs identically
    /// with or without AIInfluence installed. If it's absent, or if anything
    /// about its API doesn't match what we expect, this class silently does
    /// nothing rather than risk breaking Small Councils' own core logic.
    ///
    /// SOURCED (not guessed) via reflection against your actual AIInfluence.dll:
    /// - AIInfluence.DynamicEvents.DynamicEventsManager.Instance (static)
    /// - .AddEvent(DynamicEvent) and .GetEventById(string) -> DynamicEvent —
    ///   both public instance methods, and confirmed (via a second targeted
    ///   reflection pass) that AddEvent has no other overloads and that
    ///   neither DynamicEvent nor DynamicEventsManager expose any kind of
    ///   "silent"/"suppress notification" flag, public or private.
    /// - AIInfluence.DynamicEvents.DynamicEvent — parameterless constructor;
    ///   Id/Type/Title/Description/PlayerInvolved/Importance (string/string/
    ///   string/string/bool/int, all writable); CharactersInvolved and
    ///   ApplicableNPCs are List&lt;string&gt; (hero StringIds), confirmed
    ///   pre-initialized by the constructor (not null) so we add to them
    ///   directly rather than replacing them; CreationTime/ExpirationTime
    ///   are System.DateTime (real time, not in-game CampaignTime — matches
    ///   AIInfluence's own AI-backend scheduling, which runs on wall-clock
    ///   time); ParticipatingKingdoms is List&lt;string&gt; too.
    ///
    /// VERIFY BEFORE COMPILE (reasonable inference, not reflection-confirmed):
    /// - CreationCampaignDays/ExpirationCampaignDays (float) — assumed to be
    ///   CampaignTime.Now.ToDays-style in-game day counters, paired with the
    ///   DateTime fields above. If AIInfluence doesn't actually read these
    ///   for anything, leaving them at 0 is harmless either way.
    /// - Forcing early/updated expiration by setting ExpirationTime on an
    ///   existing event (found via GetEventById) — reasonable inference
    ///   given ExpirationTime is a plain settable property, but not
    ///   confirmed that AIInfluence re-checks expiration on existing events
    ///   versus only at creation time.
    /// </summary>
    public static class AIInfluenceIntegrationService
    {
        private const int NewsEventLifespanDays = 14;
        private const int OngoingStatusLifespanYears = 5;
        private const string OngoingStatusTypeTag = "SmallCouncils.CouncilPositionOngoing";
        private const string NewsTypeTag = "SmallCouncils.CouncilPositionNews";

        private static bool _initialized;
        private static bool _available;

        private static Type _dynamicEventType;
        private static PropertyInfo _managerInstanceProp;
        private static MethodInfo _addEventMethod;
        private static MethodInfo _getEventByIdMethod;

        private static PropertyInfo _idProp;
        private static PropertyInfo _typeProp;
        private static PropertyInfo _titleProp;
        private static PropertyInfo _descriptionProp;
        private static PropertyInfo _playerInvolvedProp;
        private static PropertyInfo _importanceProp;
        private static PropertyInfo _creationTimeProp;
        private static PropertyInfo _expirationTimeProp;
        private static PropertyInfo _charactersInvolvedProp;
        private static PropertyInfo _applicableNpcsProp;
        private static PropertyInfo _participatingKingdomsProp;

        /// <summary>
        /// True only when the MCM toggle is on AND AIInfluence is actually
        /// installed/detected — used elsewhere (CouncilAssignmentService) to
        /// decide whether Small Councils' own native chat announcement
        /// should be skipped in favor of AIInfluence's own generated
        /// message for the same appointment/removal, avoiding a duplicate.
        /// </summary>
        public static bool IsActive()
        {
            return SmallCouncils.Settings.CouncilSettings.Instance?.EnableAIInfluenceIntegration == true && EnsureInitialized();
        }

        public static void NotifyPositionAssigned(Kingdom kingdom, CouncilPosition position, Hero assignee)
        {
            if (!IsActive() || kingdom == null || assignee == null)
            {
                return;
            }

            try
            {
                object manager = _managerInstanceProp.GetValue(null);

                // A new assignment (to this position or any other) resolves
                // any lingering unhappiness from a previous removal — just
                // expire it early rather than deleting it, since we don't
                // have a delete API and an expired event is inert either way.
                string unhappinessId = BuildUnhappinessEventId(assignee);
                object existingUnhappiness = _getEventByIdMethod.Invoke(manager, new object[] { unhappinessId });
                if (existingUnhappiness != null)
                {
                    _expirationTimeProp.SetValue(existingUnhappiness, DateTime.UtcNow);
                }

                Hero ruler = kingdom.Leader;
                string positionName = CouncilPositionDisplay.GetName(position);
                string title = $"{assignee.Name} named {positionName}";
                string description = ruler != null
                    ? $"{ruler.Name} has named {assignee.Name} as {positionName} of {kingdom.Name}'s small council. {assignee.Name} holds this position with pride."
                    : $"{assignee.Name} has been named {positionName} of {kingdom.Name}'s small council. {assignee.Name} holds this position with pride.";

                List<Hero> involved = BuildInvolvedCircle(kingdom, assignee, ruler);
                string id = BuildOngoingStatusEventId(kingdom, position, assignee);
                DateTime expiration = DateTime.UtcNow.AddYears(OngoingStatusLifespanYears);

                UpdateExistingOrCreateEvent(manager, id, OngoingStatusTypeTag, kingdom, title, description, involved, expiration);
            }
            catch
            {
                // Optional integration — never let a failure here affect Small Councils' own logic.
            }
        }

        public static void NotifyPositionUnassigned(Kingdom kingdom, CouncilPosition position, Hero formerHolder)
        {
            if (!IsActive() || kingdom == null || formerHolder == null)
            {
                return;
            }

            try
            {
                object manager = _managerInstanceProp.GetValue(null);

                // Close out the long-lived "ongoing status" event from when
                // this hero was assigned, so they stop being referenced as
                // currently holding the position.
                string ongoingId = BuildOngoingStatusEventId(kingdom, position, formerHolder);
                object existingOngoing = _getEventByIdMethod.Invoke(manager, new object[] { ongoingId });
                if (existingOngoing != null)
                {
                    _expirationTimeProp.SetValue(existingOngoing, DateTime.UtcNow);
                }

                Hero ruler = kingdom.Leader;
                string positionName = CouncilPositionDisplay.GetName(position);
                string title = $"{formerHolder.Name} removed as {positionName}";
                string description = $"{formerHolder.Name} is no longer {positionName} of {kingdom.Name}'s small council. {formerHolder.Name} is unhappy about this decision.";

                List<Hero> involved = BuildInvolvedCircle(kingdom, formerHolder, ruler);
                string newsId = BuildUnhappinessEventId(formerHolder);
                DateTime expiration = DateTime.UtcNow.AddDays(NewsEventLifespanDays);

                UpdateExistingOrCreateEvent(manager, newsId, NewsTypeTag, kingdom, title, description, involved, expiration);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Deterministic per (kingdom, position, hero) — lets a later call
        /// find and either close or refresh the exact same event, without
        /// needing any separately-tracked, save-fragile state.
        /// </summary>
        private static string BuildOngoingStatusEventId(Kingdom kingdom, CouncilPosition position, Hero hero)
        {
            return $"smallcouncils_{kingdom.StringId}_status_{(int)position}_{hero.StringId}";
        }

        /// <summary>
        /// Deterministic per hero (not per position/kingdom) — a hero can
        /// only ever have one lingering "unhappy about being removed"
        /// event active at a time, and any new assignment (to the same
        /// position, a different one, or a different kingdom entirely)
        /// should resolve it, regardless of which removal caused it.
        /// </summary>
        private static string BuildUnhappinessEventId(Hero hero)
        {
            return $"smallcouncils_unhappy_{hero.StringId}";
        }

        /// <summary>
        /// The heroes who should be aware of / able to comment on this event:
        /// the hero the event is about, the ruler, and every other current
        /// council position holder — a bounded, plausible "court" circle,
        /// rather than the entire kingdom's population.
        /// </summary>
        private static List<Hero> BuildInvolvedCircle(Kingdom kingdom, Hero subject, Hero ruler)
        {
            var result = new List<Hero> { subject };
            if (ruler != null && ruler != subject)
            {
                result.Add(ruler);
            }

            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data != null)
            {
                foreach (CouncilPosition otherPosition in (CouncilPosition[])Enum.GetValues(typeof(CouncilPosition)))
                {
                    Hero holder = data.GetAssignee(otherPosition);
                    if (holder != null && !result.Contains(holder))
                    {
                        result.Add(holder);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checked via GetEventById first — if an event with this exact Id
        /// already exists (this hero previously held this exact position,
        /// or already has a pending unhappy event), its content is updated
        /// in place and AddEvent is never called. Only a genuinely new Id
        /// results in an AddEvent call — this is the actual clutter
        /// reduction, since it means we call AddEvent far less often over a
        /// long playthrough with repeated council churn, without touching
        /// anything about how AIInfluence itself decides whether to notify.
        /// </summary>
        private static void UpdateExistingOrCreateEvent(object manager, string id, string typeTag, Kingdom kingdom, string title, string description, List<Hero> involvedHeroes, DateTime expirationTime)
        {
            object existing = _getEventByIdMethod.Invoke(manager, new object[] { id });
            if (existing != null)
            {
                _titleProp.SetValue(existing, title);
                _descriptionProp.SetValue(existing, description);
                _expirationTimeProp.SetValue(existing, expirationTime);
                _playerInvolvedProp.SetValue(existing, involvedHeroes.Any(h => h == Hero.MainHero));
                return;
            }

            object dynamicEvent = Activator.CreateInstance(_dynamicEventType);

            _idProp.SetValue(dynamicEvent, id);
            _typeProp.SetValue(dynamicEvent, typeTag);
            _titleProp.SetValue(dynamicEvent, title);
            _descriptionProp.SetValue(dynamicEvent, description);
            _playerInvolvedProp.SetValue(dynamicEvent, involvedHeroes.Any(h => h == Hero.MainHero));
            _importanceProp.SetValue(dynamicEvent, 3);
            _creationTimeProp.SetValue(dynamicEvent, DateTime.UtcNow);
            _expirationTimeProp.SetValue(dynamicEvent, expirationTime);

            IList charactersInvolved = (IList)_charactersInvolvedProp.GetValue(dynamicEvent);
            IList applicableNpcs = (IList)_applicableNpcsProp.GetValue(dynamicEvent);
            foreach (Hero hero in involvedHeroes)
            {
                if (!string.IsNullOrEmpty(hero.StringId))
                {
                    charactersInvolved.Add(hero.StringId);
                    applicableNpcs.Add(hero.StringId);
                }
            }

            IList participatingKingdoms = (IList)_participatingKingdomsProp.GetValue(dynamicEvent);
            if (!string.IsNullOrEmpty(kingdom.StringId))
            {
                participatingKingdoms.Add(kingdom.StringId);
            }

            _addEventMethod.Invoke(manager, new[] { dynamicEvent });
        }

        private static bool EnsureInitialized()
        {
            if (_initialized)
            {
                return _available;
            }

            _initialized = true;

            try
            {
                Assembly aiInfluenceAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "AIInfluence");
                if (aiInfluenceAssembly == null)
                {
                    return false;
                }

                _dynamicEventType = aiInfluenceAssembly.GetType("AIInfluence.DynamicEvents.DynamicEvent");
                Type managerType = aiInfluenceAssembly.GetType("AIInfluence.DynamicEvents.DynamicEventsManager");
                if (_dynamicEventType == null || managerType == null)
                {
                    return false;
                }

                _managerInstanceProp = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                _addEventMethod = managerType.GetMethod("AddEvent", BindingFlags.Public | BindingFlags.Instance);
                _getEventByIdMethod = managerType.GetMethod("GetEventById", BindingFlags.Public | BindingFlags.Instance);

                _idProp = _dynamicEventType.GetProperty("Id");
                _typeProp = _dynamicEventType.GetProperty("Type");
                _titleProp = _dynamicEventType.GetProperty("Title");
                _descriptionProp = _dynamicEventType.GetProperty("Description");
                _playerInvolvedProp = _dynamicEventType.GetProperty("PlayerInvolved");
                _importanceProp = _dynamicEventType.GetProperty("Importance");
                _creationTimeProp = _dynamicEventType.GetProperty("CreationTime");
                _expirationTimeProp = _dynamicEventType.GetProperty("ExpirationTime");
                _charactersInvolvedProp = _dynamicEventType.GetProperty("CharactersInvolved");
                _applicableNpcsProp = _dynamicEventType.GetProperty("ApplicableNPCs");
                _participatingKingdomsProp = _dynamicEventType.GetProperty("ParticipatingKingdoms");

                _available = _managerInstanceProp != null && _addEventMethod != null && _getEventByIdMethod != null
                    && _idProp != null && _typeProp != null && _titleProp != null && _descriptionProp != null
                    && _playerInvolvedProp != null && _importanceProp != null
                    && _creationTimeProp != null && _expirationTimeProp != null
                    && _charactersInvolvedProp != null && _applicableNpcsProp != null && _participatingKingdomsProp != null;

                return _available;
            }
            catch
            {
                _available = false;
                return false;
            }
        }
    }
}
