using SmallCouncils.Models;
using SmallCouncils.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SmallCouncils.UI.View
{
    /// <summary>
    /// Formats the "what does this position actually give right now" preview
    /// text shown in the read-only council viewer. Deliberately recomputes
    /// fresh from the hero's current skill values (rather than reading the
    /// cached bonus services) so the preview is always exactly accurate at
    /// the moment the viewer is opened, not just as fresh as the last daily
    /// tick.
    ///
    /// All multipliers now read from CouncilSettings.Instance (Stage 7) so
    /// this stays in sync with the MCM menu automatically — previously this
    /// file had its own hardcoded copies that could drift out of sync with
    /// the actual applied bonuses, which is exactly what happened.
    /// </summary>
    public static class CouncilBenefitDisplay
    {
        public static string GetPositionBenefitText(CouncilPosition position, Hero holder)
        {
            if (holder == null)
            {
                return string.Empty;
            }

            CouncilSettings s = CouncilSettings.Instance;

            switch (position)
            {
                case CouncilPosition.HandOfTheKing:
                {
                    int percentPoints = s?.HandOfTheKingInfluencePercentPoints ?? 1;
                    float handInfluence = holder.Clan?.Influence ?? 0f;
                    float influenceBonus = handInfluence * (percentPoints / 100f);
                    return $"+5 influence/day (self), +{influenceBonus:0.#} influence/day to ruler ({percentPoints}% of Hand's clan influence)";
                }

                case CouncilPosition.GrandMaester:
                {
                    int medicine = holder.GetSkillValue(DefaultSkills.Medicine);
                    float medicineMultiplier = s?.GrandMaesterMedicineBonusMultiplier ?? 0.2f;
                    float healthBonus = medicine * medicineMultiplier;
                    return $"+{healthBonus:0.#} max hit points to ruler";
                }

                case CouncilPosition.MasterOfCoin:
                {
                    int steward = holder.GetSkillValue(DefaultSkills.Steward);
                    int personalGold = s?.MasterOfCoinPersonalGold ?? 10000;
                    float clanIncomeMultiplier = s?.MasterOfCoinClanIncomeMultiplier ?? 20f;
                    int rulerIncome = (int)System.Math.Floor(steward * clanIncomeMultiplier);
                    return $"+{personalGold:N0} gold/day (self), +{rulerIncome} gold/day to clan treasury";
                }

                case CouncilPosition.MasterOfLaws:
                {
                    int leadership = holder.GetSkillValue(DefaultSkills.Leadership);
                    float securityMultiplier = s?.MasterOfLawsSecurityBonusMultiplier ?? 999f; // TEMP DIAGNOSTIC — was ?? 0.1f
                    float security = leadership * securityMultiplier;
                    return $"+{security:0.#} security to all settlements";
                }

                case CouncilPosition.MasterOfShips:
                {
                    SkillObject shipmaster = SmallCouncils.Services.NavalSkillLookup.ShipmasterSkill;
                    if (shipmaster == null)
                    {
                        return "Naval speed bonus (Warsails not detected)";
                    }

                    int shipmasterSkill = holder.GetSkillValue(shipmaster);
                    float speedMultiplier = s?.MasterOfShipsSpeedBonusMultiplier ?? 0.1f;
                    float speedBonus = (shipmasterSkill / 100f) * speedMultiplier;
                    return $"+{speedBonus:0.##} speed for naval travel";
                }

                case CouncilPosition.MasterOfWhisperers:
                {
                    int roguery = holder.GetSkillValue(DefaultSkills.Roguery);
                    float relationMultiplier = s?.MasterOfWhisperersRelationGainMultiplier ?? 0.05f;
                    int relationGain = (int)System.Math.Round(roguery * relationMultiplier);
                    return $"+{relationGain} reputation weekly (random lord)";
                }

                case CouncilPosition.LordCommanderOfKingsguard:
                {
                    float vigor = System.Math.Max(holder.GetSkillValue(DefaultSkills.OneHanded),
                        System.Math.Max(holder.GetSkillValue(DefaultSkills.TwoHanded), holder.GetSkillValue(DefaultSkills.Polearm)));
                    float moraleMultiplier = s?.LordCommanderMoraleBonusMultiplier ?? 0.05f;
                    int moraleBonus = (int)System.Math.Round(vigor * moraleMultiplier);
                    float lordCommanderXpMultiplier = s?.LordCommanderXpMultiplier ?? 3f;
                    return $"+{moraleBonus} morale to ruler's party, {lordCommanderXpMultiplier:0.#}x skill XP (self)";
                }

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Ordinary Kingsguard roster members' skill XP multiplier — a
        /// flat, configurable value now, no longer derived from the Lord
        /// Commander's stats (that's how the old flat-vigor-bonus system
        /// worked; the lordCommander parameter is kept for call-site
        /// compatibility but the multiplier itself doesn't depend on it).
        /// </summary>
        public static string GetKingsguardBenefitText(Hero lordCommander)
        {
            float multiplier = CouncilSettings.Instance?.KingsguardXpMultiplier ?? 2f;
            return $"{multiplier:0.#}x skill XP";
        }
    }
}
