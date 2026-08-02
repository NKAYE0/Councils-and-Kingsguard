using HarmonyLib;
using SmallCouncils.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Adds the Master of Ships' shipmaster-skill-based speed bonus to
    /// parties currently at sea in their kingdom.
    ///
    /// SOURCED (not guessed): DefaultPartySpeedCalculatingModel.CalculateFinalSpeed's
    /// signature — (MobileParty mobileParty, ExplainedNumber finalSpeed)
    /// returning ExplainedNumber — and MobileParty.IsCurrentlyAtSea were both
    /// confirmed via reflection against your assembly.
    /// </summary>
    [HarmonyPatch(typeof(DefaultPartySpeedCalculatingModel), "CalculateFinalSpeed")]
    public static class DefaultPartySpeedCalculatingModel_MasterOfShips_Patch
    {
        private static void Postfix(MobileParty mobileParty, ExplainedNumber finalSpeed, ref ExplainedNumber __result)
        {
            if (mobileParty == null || !mobileParty.IsCurrentlyAtSea)
            {
                return;
            }

            Kingdom kingdom = mobileParty.MapFaction as Kingdom;
            if (kingdom == null)
            {
                return;
            }

            float bonus = CouncilHealingAndMoraleBonusService.GetMasterOfShipsSpeedBonus(kingdom);
            if (bonus > 0f)
            {
                __result.Add(bonus, new TextObject("Master of Ships"));
            }
        }
    }
}
