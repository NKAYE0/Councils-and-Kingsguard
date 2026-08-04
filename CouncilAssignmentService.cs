using SmallCouncils.Behaviors;
using SmallCouncils.Models;
using SmallCouncils.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace SmallCouncils.Services
{
    /// <summary>
    /// Single validated entry point for changing council/Kingsguard state.
    /// Both the player-facing UI (Stage 5) and the AI evaluation logic
    /// (Stage 3) should go through this class rather than touching
    /// CouncilData directly, so relation changes and cleanup rules are only
    /// implemented once.
    ///
    /// DESIGN NOTES:
    /// 1. A hero may hold only ONE role at a time — a single council
    ///    position, OR a single Kingsguard roster slot, never more than one
    ///    simultaneously. Assigning a hero to a new role automatically
    ///    vacates whatever role they previously held (in any kingdom),
    ///    mirroring the player's unrestricted "reassign whenever they
    ///    please" ability. This is enforced centrally in
    ///    EnforceSingleRoleForHero so both player actions and future AI
    ///    logic (Stage 3) get it for free.
    /// 2. Death/clan-departure cleanup (HandleHeroRemoval) clears roles
    ///    silently, with no relation-change side effect, since that penalty
    ///    is for deliberate unassignment, not death.
    ///
    /// VERIFY BEFORE COMPILE:
    /// - Hero.IsLord is my best-confidence guess for "is this hero a noble
    ///   with a clan, as opposed to a wanderer/companion". If this property
    ///   doesn't exist under that name, compile errors will show exactly
    ///   where to fix it.
    /// - ChangeRelationAction.ApplyRelationChangeBetweenHeroes signature —
    ///   commonly used across many published mods in this shape, but not
    ///   verified against your specific assembly version.
    /// </summary>
    public static class CouncilAssignmentService
    {
        // ============================================================
        // Council position assignment (7 named positions)
        // ============================================================

        public static bool TryAssignPositionAsPlayer(Kingdom kingdom, CouncilPosition position, Hero hero, out string failReason)
        {
            failReason = null;

            if (kingdom == null)
            {
                failReason = "No kingdom specified.";
                return false;
            }

            if (hero == null)
            {
                failReason = "No hero specified.";
                return false;
            }

            if (!IsPlayerRulerOf(kingdom))
            {
                failReason = "You must be the ruler of this kingdom to assign council positions.";
                return false;
            }

            if (!hero.IsAlive)
            {
                failReason = $"{hero.Name} is dead and cannot be assigned.";
                return false;
            }

            if (hero == kingdom.Leader)
            {
                failReason = "The ruler cannot be assigned to a council position.";
                return false;
            }

            bool eligible = position == CouncilPosition.LordCommanderOfKingsguard
                ? IsMemberOfPlayerClan(hero)
                : IsEligibleLordOrLadyForKingdom(hero, kingdom);

            if (!eligible)
            {
                failReason = position == CouncilPosition.LordCommanderOfKingsguard
                    ? $"{hero.Name} must be a member of your clan to become Lord Commander."
                    : $"{hero.Name} must be a lord/lady of your kingdom or clan to hold this position.";
                return false;
            }

            AssignPositionInternal(kingdom, position, hero);
            return true;
        }

        public static bool TryUnassignPositionAsPlayer(Kingdom kingdom, CouncilPosition position, out string failReason)
        {
            failReason = null;

            if (kingdom == null)
            {
                failReason = "No kingdom specified.";
                return false;
            }

            if (!IsPlayerRulerOf(kingdom))
            {
                failReason = "You must be the ruler of this kingdom to change council positions.";
                return false;
            }

            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data == null || data.IsVacant(position))
            {
                failReason = "This position is already vacant.";
                return false;
            }

            UnassignPositionInternal(kingdom, position);
            return true;
        }

        /// <summary>
        /// Unvalidated assignment path for AI logic (Stage 3) and internal
        /// use by the player-facing methods above. Applies the position's
        /// relation-gain to the new holder and, if someone else already held
        /// the position, the relation-loss to the outgoing holder first.
        /// Callers are responsible for eligibility checks — this method only
        /// enforces data integrity, not policy.
        /// </summary>
        public static void AssignPositionInternal(Kingdom kingdom, CouncilPosition position, Hero hero)
        {
            if (kingdom == null || hero == null)
            {
                return;
            }

            // Safety net: rulers can never hold a council position, regardless of
            // entry path (player UI already blocks this with a message; AI candidate
            // selection already excludes the ruler from its pool — this is a final
            // defensive guard, silent no-op since a policy-level caller should never
            // legitimately reach this).
            if (hero == kingdom.Leader)
            {
                return;
            }

            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data == null)
            {
                return;
            }

            Hero previousHolder = data.GetAssignee(position);
            if (previousHolder == hero)
            {
                return;
            }

            if (previousHolder != null)
            {
                UnassignPositionInternal(kingdom, position);
            }

            // Enforce single-role restriction: pull the hero out of any other
            // council position or Kingsguard slot they currently hold, in any
            // kingdom, before placing them here.
            EnforceSingleRoleForHero(hero, kingdom, position, null);

            data.SetAssigneeRaw(position, hero);

            Hero ruler = kingdom.Leader;
            if (ruler != null && ruler != hero)
            {
                int gain = CouncilConstants.GetAssignRelationGain(position);
                if (gain != 0)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, ruler, gain, true);
                }
            }

            AnnounceAssignment(ruler, hero, position);

            AIInfluenceIntegrationService.NotifyPositionAssigned(kingdom, position, hero);
        }

        /// <summary>
        /// Posts a native-style chat-log notification, e.g. "Stannis Baratheon
        /// has named Davos Seaworth as his Hand of the King."
        ///
        /// VERIFY BEFORE COMPILE: InformationManager.DisplayMessage /
        /// InformationMessage — extremely standard across the modding
        /// ecosystem for exactly this kind of native-look green chat-log
        /// text, but not yet compiled against your assembly.
        /// </summary>
        private static void AnnounceAssignment(Hero ruler, Hero hero, CouncilPosition position)
        {
            if (ruler == null || hero == null)
            {
                return;
            }

            string pronoun = ruler.IsFemale ? "her" : "his";
            string positionName = CouncilPositionDisplay.GetName(position);
            string text = $"{ruler.Name} has named {hero.Name} as {pronoun} {positionName}.";

            TaleWorlds.Library.InformationManager.DisplayMessage(new TaleWorlds.Library.InformationMessage(text));
        }

        /// <summary>Unvalidated unassignment path — see AssignPositionInternal remarks.</summary>
        public static void UnassignPositionInternal(Kingdom kingdom, CouncilPosition position)
        {
            if (kingdom == null)
            {
                return;
            }

            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data == null)
            {
                return;
            }

            Hero previousHolder = data.GetAssignee(position);
            if (previousHolder == null)
            {
                return;
            }

            data.SetAssigneeRaw(position, null);

            Hero ruler = kingdom.Leader;
            if (ruler != null && ruler != previousHolder)
            {
                int loss = CouncilConstants.GetUnassignRelationLoss(position);
                if (loss != 0)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(previousHolder, ruler, loss, true);
                }
            }

            AIInfluenceIntegrationService.NotifyPositionUnassigned(kingdom, position, previousHolder);
        }

        // ============================================================
        // Kingsguard roster (player-only, 6 slots, no relation changes)
        // ============================================================

        public static bool TryAssignKingsguardMemberAsPlayer(Kingdom kingdom, int slotIndex, Hero hero, out string failReason)
        {
            failReason = null;

            if (kingdom == null)
            {
                failReason = "No kingdom specified.";
                return false;
            }

            if (hero == null)
            {
                failReason = "No hero specified.";
                return false;
            }

            if (!IsPlayerRulerOf(kingdom))
            {
                failReason = "You must be the ruler of this kingdom to assign the Kingsguard.";
                return false;
            }

            if (!hero.IsAlive)
            {
                failReason = $"{hero.Name} is dead and cannot be assigned.";
                return false;
            }

            if (!IsMemberOfPlayerClan(hero))
            {
                failReason = $"{hero.Name} must be a member of your clan to join the Kingsguard.";
                return false;
            }

            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data == null)
            {
                failReason = "Council data unavailable.";
                return false;
            }

            if (slotIndex < 0 || slotIndex >= CouncilData.KingsguardRosterSize)
            {
                failReason = "Invalid Kingsguard slot.";
                return false;
            }

            int existingIndex = data.IndexOfKingsguardMember(hero);
            if (existingIndex == slotIndex)
            {
                // Already in this exact slot — no-op.
                return true;
            }

            // Enforce single-role restriction: pull the hero out of any other
            // council position or Kingsguard slot they currently hold, in any
            // kingdom, before placing them here.
            EnforceSingleRoleForHero(hero, kingdom, null, slotIndex);

            data.TrySetKingsguardMemberRaw(slotIndex, hero);
            return true;
        }

        public static bool TryUnassignKingsguardMemberAsPlayer(Kingdom kingdom, int slotIndex, out string failReason)
        {
            failReason = null;

            if (kingdom == null)
            {
                failReason = "No kingdom specified.";
                return false;
            }

            if (!IsPlayerRulerOf(kingdom))
            {
                failReason = "You must be the ruler of this kingdom to change the Kingsguard.";
                return false;
            }

            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            if (data == null)
            {
                failReason = "Council data unavailable.";
                return false;
            }

            data.TrySetKingsguardMemberRaw(slotIndex, null);
            return true;
        }

        // ============================================================
        // Cleanup
        // ============================================================

        /// <summary>
        /// Clears a hero from every council position and Kingsguard slot
        /// across every kingdom, with no relation-change side effects.
        /// Intended to be called from death/clan-change event listeners once
        /// those are wired up in Stage 3.
        /// </summary>
        public static void HandleHeroRemoval(Hero hero)
        {
            if (hero == null)
            {
                return;
            }

            foreach (Kingdom kingdom in Kingdom.All)
            {
                CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
                data?.RemoveHeroEverywhere(hero);
            }
        }

        // ============================================================
        // Single-role enforcement
        // ============================================================

        /// <summary>
        /// Removes the hero from any council position or Kingsguard slot they
        /// currently hold, across every kingdom, EXCEPT the specific slot
        /// they're about to be placed into (so re-assigning to the same spot
        /// is a no-op rather than an unassign-then-reassign).
        ///
        /// Exactly one of targetPosition/targetKingsguardSlot should be set,
        /// matching whichever kind of role the hero is being placed into.
        /// </summary>
        private static void EnforceSingleRoleForHero(Hero hero, Kingdom targetKingdom, CouncilPosition? targetPosition, int? targetKingsguardSlot)
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
                if (data == null)
                {
                    continue;
                }

                CouncilPosition? existingPosition = data.FindPositionOfHero(hero);
                if (existingPosition.HasValue)
                {
                    bool isTargetSlot = kingdom == targetKingdom && targetPosition.HasValue && existingPosition.Value == targetPosition.Value;
                    if (!isTargetSlot)
                    {
                        UnassignPositionInternal(kingdom, existingPosition.Value);
                    }
                }

                int existingSlot = data.IndexOfKingsguardMember(hero);
                if (existingSlot >= 0)
                {
                    bool isTargetSlot = kingdom == targetKingdom && targetKingsguardSlot.HasValue && existingSlot == targetKingsguardSlot.Value;
                    if (!isTargetSlot)
                    {
                        data.TrySetKingsguardMemberRaw(existingSlot, null);
                    }
                }
            }
        }

        // ============================================================
        // Candidate lookup (for UI selection lists — Stage 5)
        // ============================================================

        /// <summary>
        /// Returns every hero eligible to be assigned to the given position
        /// by the player right now (already excludes the current holder, if
        /// any, so the UI can show "who could I switch to").
        /// </summary>
        public static System.Collections.Generic.List<Hero> GetEligibleCandidatesForPosition(Kingdom kingdom, CouncilPosition position)
        {
            var result = new System.Collections.Generic.List<Hero>();
            if (kingdom == null)
            {
                return result;
            }

            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            Hero currentHolder = data?.GetAssignee(position);

            foreach (Clan clan in kingdom.Clans)
            {
                foreach (Hero hero in clan.Heroes)
                {
                    if (hero == null || !hero.IsAlive || hero == currentHolder)
                    {
                        continue;
                    }

                    bool eligible;
                    if (position == CouncilPosition.LordCommanderOfKingsguard)
                    {
                        eligible = IsMemberOfPlayerClan(hero);
                    }
                    else if (position == CouncilPosition.HandOfTheKing)
                    {
                        eligible = IsEligibleLordOrLadyForKingdom(hero, kingdom) && hero == hero.Clan.Leader;
                    }
                    else
                    {
                        eligible = IsEligibleLordOrLadyForKingdom(hero, kingdom);
                    }

                    if (eligible)
                    {
                        result.Add(hero);
                    }
                }
            }

            // The player's own clan heroes are always in-pool per spec, even if
            // (in unusual setups) the player's clan somehow isn't in kingdom.Clans.
            if (Hero.MainHero?.Clan != null && !kingdom.Clans.Contains(Hero.MainHero.Clan))
            {
                foreach (Hero hero in Hero.MainHero.Clan.Heroes)
                {
                    if (hero != null && hero.IsAlive && hero != currentHolder && !result.Contains(hero))
                    {
                        bool eligible = position == CouncilPosition.LordCommanderOfKingsguard || hero.IsLord;
                        if (eligible)
                        {
                            result.Add(hero);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>Returns every hero in the player's clan eligible for a Kingsguard roster slot right now.</summary>
        public static System.Collections.Generic.List<Hero> GetEligibleCandidatesForKingsguard(Kingdom kingdom)
        {
            var result = new System.Collections.Generic.List<Hero>();
            if (Hero.MainHero?.Clan == null)
            {
                return result;
            }

            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            Hero lordCommander = data?.GetAssignee(CouncilPosition.LordCommanderOfKingsguard);

            foreach (Hero hero in Hero.MainHero.Clan.Heroes)
            {
                if (hero == null || !hero.IsAlive)
                {
                    continue;
                }

                if (hero == Hero.MainHero)
                {
                    continue; // the player character can't serve as an ordinary Kingsguard member
                }

                if (hero == lordCommander)
                {
                    continue; // the Lord Commander leads the Kingsguard, doesn't also occupy a regular roster slot
                }

                if (data != null && data.IndexOfKingsguardMember(hero) >= 0)
                {
                    continue; // already on the roster somewhere
                }

                result.Add(hero);
            }

            return result;
        }

        // ============================================================
        // Eligibility helpers
        // ============================================================

        private static bool IsPlayerRulerOf(Kingdom kingdom)
        {
            return kingdom.Leader == Hero.MainHero;
        }

        private static bool IsMemberOfPlayerClan(Hero hero)
        {
            return hero != Hero.MainHero && hero.Clan != null && hero.Clan == Hero.MainHero.Clan;
        }

        private static bool IsEligibleLordOrLadyForKingdom(Hero hero, Kingdom kingdom)
        {
            if (hero.Clan == null || !hero.IsLord || hero.Clan.IsMinorFaction || hero.Clan.IsClanTypeMercenary)
            {
                return false;
            }

            return hero.Clan == Hero.MainHero.Clan || hero.Clan.Kingdom == kingdom;
        }
    }
}
