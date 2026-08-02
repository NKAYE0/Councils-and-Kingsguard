using HarmonyLib;
using SmallCouncils.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Adds the Lord Commander of the Kingsguard's vigor-based morale bonus
    /// to the kingdom ruler's own party.
    ///
    /// WHY THIS PATCH EXISTS: MobileParty.Morale is confirmed read-only via
    /// reflection, and DefaultPartyMoraleModel.GetEffectivePartyMorale is the
    /// model that produces the value it's computed from. Pure additive
    /// Postfix — never suppresses the original calculation.
    /// </summary>
    [HarmonyPatch(typeof(DefaultPartyMoraleModel), "GetEffectivePartyMorale")]
    public static class DefaultPartyMoraleModel_LordCommander_Patch
    {
        private static void Postfix(MobileParty mobileParty, bool includeDescription, ref ExplainedNumber __result)
        {
            float bonus = CouncilHealingAndMoraleBonusService.GetLordCommanderMoraleBonus(mobileParty);
            if (bonus > 0f)
            {
                __result.Add(bonus, includeDescription ? new TextObject("Lord Commander of the Kingsguard") : null);
            }
        }
    }
}
