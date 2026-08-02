using System.Collections.Generic;
using SmallCouncils.Behaviors;
using SmallCouncils.Models;
using SmallCouncils.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace SmallCouncils.Services
{
    /// <summary>
    /// Cached lookups for several Harmony-patched benefits, following the
    /// same pattern as KingsguardBonusService: recompute cheaply on a daily
    /// cadence, never inside the patched methods themselves (all of these —
    /// GetEffectivePartyMorale, MaxHitpoints, CalculateSecurityChange,
    /// CalculateClanIncomeInternal, CalculateInfluenceChange — are called
    /// very frequently).
    ///
    /// - Grand Maester: extra max hit points for the kingdom's ruler
    ///   personally, equal to the Grand Maester's medicine skill x a
    ///   configurable multiplier — shown as its own line in the ruler's HP
    ///   breakdown tooltip.
    /// - Lord Commander: morale bonus applies ONLY to the kingdom ruler's own
    ///   party (matching "boost morale within the player/kingdom leaders
    ///   party" specifically, not every ruling-clan party).
    /// - Hand of the King: daily influence to the ruler's clan, equal to a
    ///   percentage of the Hand's own clan's current influence.
    ///
    /// VERIFY BEFORE COMPILE:
    /// - PartyBase.MobileParty and MobileParty.ActualClan — my best-confidence
    ///   guesses for getting from a healing-calculation PartyBase to the
    ///   clan that owns it.
    /// - Hero.PartyBelongedTo — my best-confidence guess for getting the
    ///   ruler's own MobileParty.
    /// - ExplainedNumber.Add(float, TextObject) — used by every patch that
    ///   reads this cache; standard pattern across published mods, and now
    ///   directly confirmed via reflection for MaxHitpoints and
    ///   CalculateInfluenceChange specifically.
    /// </summary>
    public static class CouncilHealingAndMoraleBonusService
    {
        private static readonly Dictionary<Hero, int> GrandMaesterHealthBonusPerHero = new Dictionary<Hero, int>();
        private static readonly Dictionary<MobileParty, float> LordCommanderMoraleBonusPerParty = new Dictionary<MobileParty, float>();
        private static readonly Dictionary<Kingdom, float> MasterOfLawsSecurityBonusPerKingdom = new Dictionary<Kingdom, float>();
        private static readonly Dictionary<Kingdom, float> MasterOfShipsSpeedBonusPerKingdom = new Dictionary<Kingdom, float>();
        private static readonly Dictionary<Clan, int> MasterOfCoinClanIncomeBonusPerClan = new Dictionary<Clan, int>();
        private static readonly Dictionary<Clan, float> HandOfTheKingInfluenceBonusPerClan = new Dictionary<Clan, float>();

        public static void RecomputeAll()
        {
            GrandMaesterHealthBonusPerHero.Clear();
            LordCommanderMoraleBonusPerParty.Clear();
            MasterOfLawsSecurityBonusPerKingdom.Clear();
            MasterOfShipsSpeedBonusPerKingdom.Clear();
            MasterOfCoinClanIncomeBonusPerClan.Clear();
            HandOfTheKingInfluenceBonusPerClan.Clear();

            foreach (Kingdom kingdom in Kingdom.All)
            {
                CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
                if (data == null)
                {
                    continue;
                }

                RecomputeGrandMaester(kingdom, data);
                RecomputeLordCommanderMorale(kingdom, data);
                RecomputeMasterOfLaws(kingdom, data);
                RecomputeMasterOfShips(kingdom, data);
                RecomputeMasterOfCoin(kingdom, data);
                RecomputeHandOfTheKing(kingdom, data);
            }
        }

        /// <summary>
        /// Grand Maester now grants extra max hit points to the kingdom's
        /// ruler personally (medicine skill x the configured multiplier),
        /// rather than boosting party healing — read by
        /// DefaultCharacterStatsModel_GrandMaester_Patch, which shows this
        /// as its own labeled line in the ruler's HP breakdown tooltip.
        /// </summary>
        private static void RecomputeGrandMaester(Kingdom kingdom, CouncilData data)
        {
            Hero grandMaester = data.GetAssignee(CouncilPosition.GrandMaester);
            Hero ruler = kingdom.Leader;
            if (grandMaester == null || !grandMaester.IsAlive || ruler == null || !ruler.IsAlive)
            {
                return;
            }

            int medicineSkill = grandMaester.GetSkillValue(DefaultSkills.Medicine);
            float multiplier = CouncilSettings.Instance?.GrandMaesterMedicineBonusMultiplier ?? 0.2f;
            int bonus = (int)System.Math.Round(medicineSkill * multiplier);
            if (bonus > 0)
            {
                GrandMaesterHealthBonusPerHero[ruler] = bonus;
            }
        }

        private static void RecomputeLordCommanderMorale(Kingdom kingdom, CouncilData data)
        {
            Hero lordCommander = data.GetAssignee(CouncilPosition.LordCommanderOfKingsguard);
            MobileParty rulerParty = kingdom.Leader?.PartyBelongedTo;
            if (lordCommander == null || !lordCommander.IsAlive || rulerParty == null)
            {
                return;
            }

            float vigor = KingsguardBonusService.GetVigorSkillValue(lordCommander);
            float multiplier = CouncilSettings.Instance?.LordCommanderMoraleBonusMultiplier ?? 0.05f;
            float moraleBonus = vigor * multiplier;
            if (moraleBonus > 0f)
            {
                LordCommanderMoraleBonusPerParty[rulerParty] = moraleBonus;
            }
        }

        private static void RecomputeMasterOfLaws(Kingdom kingdom, CouncilData data)
        {
            Hero masterOfLaws = data.GetAssignee(CouncilPosition.MasterOfLaws);
            if (masterOfLaws == null || !masterOfLaws.IsAlive)
            {
                return;
            }

            int leadershipSkill = masterOfLaws.GetSkillValue(DefaultSkills.Leadership);
            float multiplier = CouncilSettings.Instance?.MasterOfLawsSecurityBonusMultiplier ?? 0.1f;
            float securityBonus = leadershipSkill * multiplier;
            if (securityBonus > 0f)
            {
                MasterOfLawsSecurityBonusPerKingdom[kingdom] = securityBonus;
            }
        }

        private static void RecomputeMasterOfShips(Kingdom kingdom, CouncilData data)
        {
            Hero masterOfShips = data.GetAssignee(CouncilPosition.MasterOfShips);
            SkillObject shipmasterSkill = NavalSkillLookup.ShipmasterSkill;
            if (masterOfShips == null || !masterOfShips.IsAlive || shipmasterSkill == null)
            {
                return;
            }

            int shipmasterValue = masterOfShips.GetSkillValue(shipmasterSkill);
            float multiplier = CouncilSettings.Instance?.MasterOfShipsSpeedBonusMultiplier ?? 0.1f;
            float speedBonus = (shipmasterValue / 100f) * multiplier;
            if (speedBonus > 0f)
            {
                MasterOfShipsSpeedBonusPerKingdom[kingdom] = speedBonus;
            }
        }

        /// <summary>
        /// Master of Coin's "ruler income" bonus (steward skill x the
        /// configured multiplier) now flows through the ruling clan's
        /// actual income calculation instead of going to the ruler's
        /// personal gold directly — this makes it show up as its own
        /// labeled line in the native clan finance breakdown tooltip, and
        /// land in the clan's real treasury rather than the ruler's
        /// personal wallet, which better matches "clan income" as a
        /// concept. The officeholder's own flat personal income is
        /// unaffected by this and still applies via ChangeHeroGold in
        /// CouncilBenefitsBehavior, since that's specifically personal
        /// income to the officeholder, not clan income.
        /// </summary>
        private static void RecomputeMasterOfCoin(Kingdom kingdom, CouncilData data)
        {
            Hero masterOfCoin = data.GetAssignee(CouncilPosition.MasterOfCoin);
            Clan rulingClan = kingdom.RulingClan;
            if (masterOfCoin == null || !masterOfCoin.IsAlive || rulingClan == null)
            {
                return;
            }

            int stewardSkill = masterOfCoin.GetSkillValue(DefaultSkills.Steward);
            float multiplier = CouncilSettings.Instance?.MasterOfCoinClanIncomeMultiplier ?? 20f;
            int clanIncome = (int)System.Math.Floor(stewardSkill * multiplier);
            if (clanIncome > 0)
            {
                MasterOfCoinClanIncomeBonusPerClan[rulingClan] = clanIncome;
            }
        }

        /// <summary>
        /// Hand of the King gives the ruler's clan daily influence equal to
        /// a configurable percentage of the Hand's OWN clan's current
        /// influence — read by DefaultClanPoliticsModel_HandOfTheKing_Patch,
        /// which shows this as its own labeled line in the clan influence
        /// breakdown tooltip. This generates new influence for the ruler
        /// rather than transferring/deducting from the Hand's clan, matching
        /// every other council benefit's "generate, don't drain" pattern.
        /// </summary>
        private static void RecomputeHandOfTheKing(Kingdom kingdom, CouncilData data)
        {
            Hero handOfTheKing = data.GetAssignee(CouncilPosition.HandOfTheKing);
            Clan rulingClan = kingdom.RulingClan;
            if (handOfTheKing?.Clan == null || rulingClan == null)
            {
                return;
            }

            int percentPoints = CouncilSettings.Instance?.HandOfTheKingInfluencePercentPoints ?? 1;
            float bonus = handOfTheKing.Clan.Influence * (percentPoints / 100f);
            if (bonus > 0f)
            {
                HandOfTheKingInfluenceBonusPerClan[rulingClan] = bonus;
            }
        }

        /// <summary>Called by the Grand Maester Harmony patch — cheap lookup only.</summary>
        public static int GetGrandMaesterHealthBonus(Hero hero)
        {
            if (hero == null)
            {
                return 0;
            }

            return GrandMaesterHealthBonusPerHero.TryGetValue(hero, out int bonus) ? bonus : 0;
        }

        /// <summary>Called by the Lord Commander Harmony patch — cheap lookup only.</summary>
        public static float GetLordCommanderMoraleBonus(MobileParty party)
        {
            if (party == null)
            {
                return 0f;
            }

            return LordCommanderMoraleBonusPerParty.TryGetValue(party, out float bonus) ? bonus : 0f;
        }

        /// <summary>Called by the Master of Laws Harmony patch — cheap lookup only.</summary>
        public static float GetMasterOfLawsSecurityBonus(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return 0f;
            }

            return MasterOfLawsSecurityBonusPerKingdom.TryGetValue(kingdom, out float bonus) ? bonus : 0f;
        }

        /// <summary>Called by the Master of Ships Harmony patch — cheap lookup only.</summary>
        public static float GetMasterOfShipsSpeedBonus(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return 0f;
            }

            return MasterOfShipsSpeedBonusPerKingdom.TryGetValue(kingdom, out float bonus) ? bonus : 0f;
        }

        /// <summary>Called by the clan finance Harmony patch — cheap lookup only.</summary>
        public static int GetMasterOfCoinClanIncomeBonus(Clan clan)
        {
            if (clan == null)
            {
                return 0;
            }

            return MasterOfCoinClanIncomeBonusPerClan.TryGetValue(clan, out int bonus) ? bonus : 0;
        }

        /// <summary>Called by the clan politics/influence Harmony patch — cheap lookup only.</summary>
        public static float GetHandOfTheKingInfluenceBonus(Clan clan)
        {
            if (clan == null)
            {
                return 0f;
            }

            return HandOfTheKingInfluenceBonusPerClan.TryGetValue(clan, out float bonus) ? bonus : 0f;
        }
    }
}
