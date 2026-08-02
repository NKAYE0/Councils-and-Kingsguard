using System.Collections.Generic;
using System.Linq;
using SmallCouncils.Behaviors;
using SmallCouncils.Models;
using SmallCouncils.Services;
using SmallCouncils.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SmallCouncils.Behaviors
{
    /// <summary>
    /// Applies the passive daily/weekly gameplay benefits of holding a
    /// council position. Runs for every kingdom (player and AI alike) — the
    /// benefits themselves aren't restricted to AI, only the assignment
    /// logic in CouncilAIBehavior is.
    ///
    /// IMPLEMENTED:
    /// - Master of Coin: daily personal gold to the officeholder, daily gold
    ///   to the ruling clan leader personally (Clan.Gold has no public
    ///   setter, confirmed via reflection — see ApplyMasterOfCoinIncome),
    ///   scaled by the officeholder's steward skill.
    /// - Master of Whisperers: weekly relation gain between the ruler and a
    ///   random lord/lady in the kingdom, scaled by the officeholder's
    ///   roguery skill.
    /// - Master of Laws: settlement security, via CouncilHealingAndMoraleBonusService
    ///   + a Harmony postfix on DefaultSettlementSecurityModel.CalculateSecurityChange
    ///   (shows as its own labeled line in the security breakdown tooltip).
    /// - Kingsguard roster vigor bonus: via KingsguardBonusService + a
    ///   Harmony postfix on CharacterObject.GetSkillValue.
    /// - Grand Maester: via CouncilHealingAndMoraleBonusService + a Harmony
    ///   postfix on DefaultPartyHealingModel.GetDailyHealingForRegulars.
    /// - Lord Commander: via CouncilHealingAndMoraleBonusService + a Harmony
    ///   postfix on DefaultPartyMoraleModel.GetEffectivePartyMorale.
    /// - Master of Ships: via CouncilHealingAndMoraleBonusService + a Harmony
    ///   postfix on DefaultPartySpeedCalculatingModel.CalculateFinalSpeed,
    ///   only applied to parties currently at sea (MobileParty.IsCurrentlyAtSea).
    ///   The "Shipmaster" skill isn't a compile-time field like the native
    ///   skills — see NavalSkillLookup for how it's resolved.
    /// (See the HarmonyPatches and Services folders for the four
    /// Harmony-dependent ones — they aren't implemented in this file.)
    ///
    /// All seven positions now have their passive benefit implemented — no
    /// positions remain deferred.
    ///
    /// VERIFY BEFORE COMPILE:
    /// - Hero.ChangeHeroGold(int) — my best-confidence guess for adjusting a
    ///   hero's personal gold.
    /// - MBRandom.RandomInt(int) — standard TaleWorlds.Core random helper,
    ///   used to pick the Whisperers' weekly relation-gain target.
    /// </summary>
    public class CouncilBenefitsBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No additional state owned by this class.
        }

        private void OnDailyTick()
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                ApplyMasterOfCoinIncome(kingdom);
            }

            // Cheap recompute (small kingdom count x 6 roster slots) — see
            // KingsguardBonusService remarks on why this isn't done per skill query.
            // Also recomputes the Master of Laws security cache, read by
            // DefaultSettlementSecurityModel_MasterOfLaws_Patch.
            KingsguardBonusService.RecomputeAll();
            CouncilHealingAndMoraleBonusService.RecomputeAll();
        }

        private void OnWeeklyTick()
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                ApplyMasterOfWhisperersRelationGain(kingdom);
            }
        }

        // ============================================================
        // Master of Coin
        // ============================================================

        private static void ApplyMasterOfCoinIncome(Kingdom kingdom)
        {
            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            Hero masterOfCoin = data?.GetAssignee(CouncilPosition.MasterOfCoin);
            if (masterOfCoin == null || !masterOfCoin.IsAlive)
            {
                return;
            }

            // Flat personal income to the officeholder only. The "ruler
            // income" portion now flows through
            // DefaultClanFinanceModel_MasterOfCoin_Patch instead of a direct
            // ChangeHeroGold call, landing in the ruling clan's actual
            // treasury and showing up as its own line in the native clan
            // finance breakdown tooltip — see
            // CouncilHealingAndMoraleBonusService.RecomputeMasterOfCoin.
            int personalGold = CouncilSettings.Instance?.MasterOfCoinPersonalGold ?? 10000;
            masterOfCoin.ChangeHeroGold(personalGold);
        }

        // ============================================================
        // Master of Whisperers
        // ============================================================

        private static void ApplyMasterOfWhisperersRelationGain(Kingdom kingdom)
        {
            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
            Hero whisperer = data?.GetAssignee(CouncilPosition.MasterOfWhisperers);
            Hero ruler = kingdom.Leader;
            if (whisperer == null || !whisperer.IsAlive || ruler == null)
            {
                return;
            }

            List<Hero> candidates = kingdom.Clans
                .SelectMany(clan => clan.Heroes)
                .Where(hero => hero != null && hero.IsAlive && hero != ruler)
                .ToList();

            if (candidates.Count == 0)
            {
                return;
            }

            Hero target = candidates[MBRandom.RandomInt(candidates.Count)];

            int roguerySkill = whisperer.GetSkillValue(DefaultSkills.Roguery);
            float relationGainMultiplier = CouncilSettings.Instance?.MasterOfWhisperersRelationGainMultiplier ?? 0.05f;
            int relationGain = (int)System.Math.Round(roguerySkill * relationGainMultiplier);
            if (relationGain != 0)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(ruler, target, relationGain, true);

                // Only surface this for the player's own kingdom — AI
                // kingdoms apply the same logic silently in the background.
                if (Hero.MainHero?.Clan?.Kingdom == kingdom)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"The Master of Whisperers has gained {relationGain} relation with {target.Name} on behalf of {ruler.Name}."));
                }
            }
        }

    }
}
