using HarmonyLib;
using SmallCouncils.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Adds the Master of Laws' leadership-based security bonus as a labeled
    /// line item in the settlement security breakdown, replacing the earlier
    /// direct-write-to-Town.Security approach (which worked numerically but
    /// didn't show up as its own line in the tooltip).
    ///
    /// SOURCED (not guessed): DefaultSettlementSecurityModel.CalculateSecurityChange's
    /// signature — (Town town, bool includeDescriptions) returning
    /// ExplainedNumber — was confirmed via reflection against your assembly.
    /// </summary>
    [HarmonyPatch(typeof(DefaultSettlementSecurityModel), "CalculateSecurityChange")]
    public static class DefaultSettlementSecurityModel_MasterOfLaws_Patch
    {
        private static void Postfix(Town town, bool includeDescriptions, ref ExplainedNumber __result)
        {
            Kingdom kingdom = town?.Settlement?.OwnerClan?.Kingdom;
            if (kingdom == null)
            {
                return;
            }

            float bonus = CouncilHealingAndMoraleBonusService.GetMasterOfLawsSecurityBonus(kingdom);
            if (bonus != 0f)
            {
                __result.Add(bonus, includeDescriptions ? new TextObject("Master of Laws") : null);
            }
        }
    }
}
