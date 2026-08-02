using HarmonyLib;
using SmallCouncils.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Adds the Master of Coin's clan-income bonus (steward skill x the
    /// configured multiplier) as a labeled line in the native clan finance
    /// breakdown tooltip, and — since this is the same calculation the game
    /// uses to actually determine the clan's daily gold change, not just a
    /// display copy — makes the bonus land in the ruling clan's real
    /// treasury rather than the ruler's personal wallet.
    ///
    /// SOURCED (not guessed): CalculateClanIncomeInternal's signature —
    /// (Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals,
    /// bool includeDetails) — was confirmed via reflection against your
    /// assembly. Clan.Gold was also reconfirmed via reflection to still
    /// have no public setter, which is why this goes through the finance
    /// model rather than a direct property write.
    /// </summary>
    [HarmonyPatch(typeof(DefaultClanFinanceModel), "CalculateClanIncomeInternal")]
    public static class DefaultClanFinanceModel_MasterOfCoin_Patch
    {
        private static void Postfix(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
        {
            int bonus = CouncilHealingAndMoraleBonusService.GetMasterOfCoinClanIncomeBonus(clan);
            if (bonus != 0)
            {
                goldChange.Add(bonus, includeDetails ? new TextObject("Master of Coin") : null);
            }
        }
    }
}
