using HarmonyLib;
using SmallCouncils.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Adds the Grand Maester's medicine-skill bonus as extra max hit points
    /// for the kingdom's ruler personally — shows as its own labeled line
    /// in the ruler's HP breakdown tooltip (the same one showing "Base",
    /// perks like Doctor's Oath, etc.).
    ///
    /// SOURCED (not guessed): DefaultCharacterStatsModel.MaxHitpoints's
    /// signature — (CharacterObject character, bool includeDescriptions) ->
    /// ExplainedNumber — was confirmed via reflection against your
    /// assembly. CharacterObject.HeroObject (for getting from the character
    /// parameter to the Hero it represents, so we can check "is this the
    /// ruler") is the standard, extremely well-established API for this and
    /// wasn't separately reflection-checked, unlike the method signature
    /// itself.
    /// </summary>
    [HarmonyPatch(typeof(DefaultCharacterStatsModel), "MaxHitpoints")]
    public static class DefaultCharacterStatsModel_GrandMaester_Patch
    {
        private static void Postfix(CharacterObject character, bool includeDescriptions, ref ExplainedNumber __result)
        {
            Hero hero = character?.HeroObject;
            if (hero == null)
            {
                return;
            }

            int bonus = CouncilHealingAndMoraleBonusService.GetGrandMaesterHealthBonus(hero);
            if (bonus != 0)
            {
                __result.Add(bonus, includeDescriptions ? new TextObject("Grand Maester") : null);
            }
        }
    }
}
