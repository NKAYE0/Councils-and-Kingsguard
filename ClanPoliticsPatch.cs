using HarmonyLib;
using SmallCouncils.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Adds the Hand of the King's influence bonus (a configurable
    /// percentage of the Hand's own clan's current influence) as a labeled
    /// line in the native clan influence breakdown tooltip, and — since
    /// this is the same calculation the game uses to actually determine the
    /// clan's daily influence change, not just a display copy — makes the
    /// bonus land in the ruling clan's real influence total.
    ///
    /// SOURCED (not guessed): CalculateInfluenceChange's signature — (Clan
    /// clan, bool includeDescriptions) -> ExplainedNumber — was confirmed
    /// via reflection against your assembly.
    /// </summary>
    [HarmonyPatch(typeof(DefaultClanPoliticsModel), "CalculateInfluenceChange")]
    public static class DefaultClanPoliticsModel_HandOfTheKing_Patch
    {
        private static void Postfix(Clan clan, bool includeDescriptions, ref ExplainedNumber __result)
        {
            float bonus = CouncilHealingAndMoraleBonusService.GetHandOfTheKingInfluenceBonus(clan);
            if (bonus != 0f)
            {
                __result.Add(bonus, includeDescriptions ? new TextObject("Hand of the King") : null);
            }
        }
    }
}
