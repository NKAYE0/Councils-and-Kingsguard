using System.Collections.Generic;
using System.Linq;
using SmallCouncils.Models;
using SmallCouncils.Services;
using SmallCouncils.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;

namespace SmallCouncils.Behaviors
{
    /// <summary>
    /// Drives AI-ruled kingdoms' council assignments. The player's own
    /// kingdom is never touched by this class — the player assigns everyone
    /// manually via CouncilAssignmentService's *AsPlayer methods.
    ///
    /// Cadence, by design:
    /// - DAILY: cheap, targeted checks only — clear dead/negative-relation
    ///   holders, and fill any currently-vacant position. This keeps seats
    ///   from sitting empty for a full week while staying cheap (small
    ///   number of kingdoms x 7 positions, no heavy scanning).
    /// - WEEKLY: the more expensive "is there a better candidate than the
    ///   current holder" comparison for the five skill-based positions, plus
    ///   the Hand of the King's 3-month re-evaluation check.
    ///
    /// Lord Commander of the Kingsguard is intentionally excluded from the
    /// "overtaken" and "negative relation" removal logic — per spec, once
    /// assigned the AI keeps them until death. It only participates in the
    /// vacancy-fill pass.
    ///
    /// Master of Ships is now fully managed like the other skill-based
    /// positions, using NavalSkillLookup to resolve NavalDLC's "Shipmaster"
    /// skill (which isn't a compile-time field — see that class's remarks).
    ///
    /// DESIGN ASSUMPTIONS / VERIFY BEFORE COMPILE:
    /// - "Clan power" for Hand of the King uses Clan.CurrentTotalStrength,
    ///   confirmed via reflection to match the "Clan Strength" stat shown on the clan encyclopedia page (you
    ///   confirmed a value of 2030 there). Property name not yet verified
    ///   against your assembly.
    /// - Kingdom.RulingClan, Clan.Leader, Hero.IsLord, Hero.IsFemale,
    ///   Hero.GetRelation(Hero) are all my best-confidence guesses at
    ///   standard, commonly-used API names — not yet compiled against your
    ///   project.
    /// - CampaignEvents.DailyTickEvent / WeeklyTickEvent / HeroKilledEvent
    ///   and the HeroKilledEvent delegate signature (Hero, Hero,
    ///   KillCharacterAction.KillCharacterActionDetail, bool) are the
    ///   standard shape used across many published mods, but likewise not
    ///   yet verified here.
    /// - A hero already holding ANY council/Kingsguard role (in any kingdom)
    ///   is excluded from candidate pools for other positions, so the AI
    ///   doesn't cannibalize its own appointments via the single-role rule.
    ///   This felt like the only sane behavior given that restriction, but
    ///   flagging it as a judgment call.
    /// </summary>
    public class CouncilAIBehavior : CampaignBehaviorBase
    {
        private static CampaignTime HandReevaluationInterval => CampaignTime.Weeks(CouncilSettings.Instance?.HandReevaluationIntervalWeeks ?? 12);

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No additional state owned by this class — everything it needs
            // lives in CouncilData (Stage 1), already synced by CouncilBehavior.
        }

        // ============================================================
        // Event handlers
        // ============================================================

        private void OnDailyTick()
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (!IsAiRuled(kingdom))
                {
                    continue;
                }

                RemoveInvalidHolders(kingdom);
                ScriptedAssignmentService.ApplyScriptedAssignments(kingdom);
                FillVacantPositions(kingdom);
            }
        }

        private void OnWeeklyTick()
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (!IsAiRuled(kingdom))
                {
                    continue;
                }

                EvaluateSkillPositionOvertakes(kingdom);
                EvaluateHandReassessment(kingdom);
            }
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            // Silent cleanup, no relation penalty — see CouncilAssignmentService.HandleHeroRemoval remarks.
            CouncilAssignmentService.HandleHeroRemoval(victim);

            // Give AI kingdoms a chance to immediately refill whatever just opened up,
            // rather than waiting for the next daily tick.
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (IsAiRuled(kingdom))
                {
                    FillVacantPositions(kingdom);
                }
            }
        }

        // ============================================================
        // Daily: invalid-holder removal + vacancy filling
        // ============================================================

        private void RemoveInvalidHolders(Kingdom kingdom)
        {
            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data == null)
            {
                return;
            }

            Hero ruler = kingdom.Leader;

            foreach (CouncilPosition position in AllManagedPositions())
            {
                Hero holder = data.GetAssignee(position);
                if (holder == null)
                {
                    continue;
                }

                if (!holder.IsAlive)
                {
                    // Death is handled via OnHeroKilled/HandleHeroRemoval already in the
                    // normal case, but this covers deaths from causes that might not
                    // route through HeroKilledEvent (defensive, cheap to check).
                    data.SetAssigneeRaw(position, null);
                    continue;
                }

                // Lord Commander is never removed for negative relation — assigned till death, per spec.
                if (position == CouncilPosition.LordCommanderOfKingsguard)
                {
                    continue;
                }

                if (ruler == null || holder == ruler)
                {
                    continue;
                }

                int relation = holder.GetRelation(ruler);
                if (relation < CouncilConstants.NegativeRelationThreshold)
                {
                    CouncilAssignmentService.UnassignPositionInternal(kingdom, position);
                }
            }
        }

        private void FillVacantPositions(Kingdom kingdom)
        {
            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data == null || kingdom.Leader == null)
            {
                return;
            }

            foreach (CouncilPosition position in AllManagedPositions())
            {
                if (!data.IsVacant(position))
                {
                    continue;
                }

                Hero candidate = FindBestCandidate(kingdom, position);
                if (candidate != null)
                {
                    CouncilAssignmentService.AssignPositionInternal(kingdom, position, candidate);
                }
            }
        }

        // ============================================================
        // Weekly: skill-based "overtaken" comparisons + Hand reassessment
        // ============================================================

        private void EvaluateSkillPositionOvertakes(Kingdom kingdom)
        {
            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data == null)
            {
                return;
            }

            foreach (CouncilPosition position in SkillBasedPositions())
            {
                Hero currentHolder = data.GetAssignee(position);
                if (currentHolder == null)
                {
                    continue; // vacancy filling is the daily pass's job
                }

                Hero bestCandidate = FindBestCandidate(kingdom, position);
                if (bestCandidate != null && bestCandidate != currentHolder)
                {
                    SkillObject skill = GetSkillForPosition(position);
                    if (skill != null && currentHolder.GetSkillValue(skill) < bestCandidate.GetSkillValue(skill))
                    {
                        CouncilAssignmentService.AssignPositionInternal(kingdom, position, bestCandidate);
                    }
                }
            }
        }

        private void EvaluateHandReassessment(Kingdom kingdom)
        {
            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data == null)
            {
                return;
            }

            CampaignTime elapsed = CampaignTime.Now - data.LastHandReevaluationTime;
            if (elapsed < HandReevaluationInterval)
            {
                return;
            }

            Hero currentHand = data.GetAssignee(CouncilPosition.HandOfTheKing);
            Hero bestCandidate = FindBestCandidate(kingdom, CouncilPosition.HandOfTheKing);

            if (bestCandidate != null && bestCandidate != currentHand)
            {
                CouncilAssignmentService.AssignPositionInternal(kingdom, CouncilPosition.HandOfTheKing, bestCandidate);
            }

            data.LastHandReevaluationTime = CampaignTime.Now;
        }

        // ============================================================
        // Candidate selection
        // ============================================================

        private static Hero FindBestCandidate(Kingdom kingdom, CouncilPosition position)
        {
            switch (position)
            {
                case CouncilPosition.HandOfTheKing:
                    return FindBestHandCandidate(kingdom);
                case CouncilPosition.LordCommanderOfKingsguard:
                    return FindBestLordCommanderCandidate(kingdom);
                default:
                    SkillObject skill = GetSkillForPosition(position);
                    return skill == null ? null : FindBestSkillCandidate(kingdom, skill);
            }
        }

        private static Hero FindBestHandCandidate(Kingdom kingdom)
        {
            Clan rulingClan = kingdom.RulingClan;
            Clan bestClan = null;
            float bestStrength = -1f;

            foreach (Clan clan in kingdom.Clans)
            {
                if (clan == rulingClan || clan.IsMinorFaction || clan.IsClanTypeMercenary || clan.Leader == null || !clan.Leader.IsAlive)
                {
                    continue;
                }

                if (!IsHeroFreeForCouncilRole(clan.Leader))
                {
                    continue;
                }

                if (clan.CurrentTotalStrength > bestStrength)
                {
                    bestStrength = clan.CurrentTotalStrength;
                    bestClan = clan;
                }
            }

            return bestClan?.Leader;
        }

        private static Hero FindBestLordCommanderCandidate(Kingdom kingdom)
        {
            Clan rulingClan = kingdom.RulingClan;
            if (rulingClan == null || rulingClan.IsMinorFaction || rulingClan.IsClanTypeMercenary)
            {
                return null;
            }

            Hero best = null;
            float bestVigor = -1f;

            foreach (Hero hero in rulingClan.Heroes)
            {
                if (hero == null || !hero.IsAlive || hero.IsFemale || !hero.IsLord || hero == kingdom.Leader)
                {
                    continue;
                }

                if (!IsHeroFreeForCouncilRole(hero))
                {
                    continue;
                }

                float vigor = System.Math.Max(hero.GetSkillValue(DefaultSkills.OneHanded), hero.GetSkillValue(DefaultSkills.TwoHanded));
                if (vigor > bestVigor)
                {
                    bestVigor = vigor;
                    best = hero;
                }
            }

            return best;
        }

        private static Hero FindBestSkillCandidate(Kingdom kingdom, SkillObject skill)
        {
            Hero best = null;
            int bestValue = -1;

            foreach (Clan clan in kingdom.Clans)
            {
                if (clan.IsMinorFaction || clan.IsClanTypeMercenary)
                {
                    continue;
                }

                foreach (Hero hero in clan.Heroes)
                {
                    if (hero == null || !hero.IsAlive || !hero.IsLord || hero == kingdom.Leader)
                    {
                        continue;
                    }

                    if (!IsHeroFreeForCouncilRole(hero))
                    {
                        continue;
                    }

                    int value = hero.GetSkillValue(skill);
                    if (value > bestValue)
                    {
                        bestValue = value;
                        best = hero;
                    }
                }
            }

            return best;
        }

        // ============================================================
        // Shared helpers
        // ============================================================

        private static bool IsHeroFreeForCouncilRole(Hero hero)
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
                if (data == null)
                {
                    continue;
                }

                if (data.FindPositionOfHero(hero).HasValue || data.IndexOfKingsguardMember(hero) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAiRuled(Kingdom kingdom)
        {
            return kingdom?.Leader != null && kingdom.Leader != Hero.MainHero;
        }

        private static IEnumerable<CouncilPosition> AllManagedPositions()
        {
            yield return CouncilPosition.HandOfTheKing;
            yield return CouncilPosition.GrandMaester;
            yield return CouncilPosition.MasterOfCoin;
            yield return CouncilPosition.MasterOfLaws;
            yield return CouncilPosition.MasterOfShips;
            yield return CouncilPosition.MasterOfWhisperers;
            yield return CouncilPosition.LordCommanderOfKingsguard;
        }

        private static IEnumerable<CouncilPosition> SkillBasedPositions()
        {
            yield return CouncilPosition.GrandMaester;
            yield return CouncilPosition.MasterOfCoin;
            yield return CouncilPosition.MasterOfLaws;
            yield return CouncilPosition.MasterOfShips;
            yield return CouncilPosition.MasterOfWhisperers;
        }

        private static SkillObject GetSkillForPosition(CouncilPosition position)
        {
            switch (position)
            {
                case CouncilPosition.GrandMaester:
                    return DefaultSkills.Medicine;
                case CouncilPosition.MasterOfCoin:
                    return DefaultSkills.Steward;
                case CouncilPosition.MasterOfLaws:
                    return DefaultSkills.Leadership;
                case CouncilPosition.MasterOfShips:
                    return NavalSkillLookup.ShipmasterSkill;
                case CouncilPosition.MasterOfWhisperers:
                    return DefaultSkills.Roguery;
                default:
                    return null;
            }
        }
    }
}
