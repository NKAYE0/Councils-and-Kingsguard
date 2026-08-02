using HarmonyLib;
using SmallCouncils.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Multiplies skill XP gained by the Lord Commander and the ordinary
    /// Kingsguard roster members they lead (separately configurable
    /// multipliers, 1x-10x, defaulting to 3x and 2x respectively).
    ///
    /// Unlike every other benefit in this mod, this can't be done as an
    /// additive ExplainedNumber postfix — a multiplier has to change the
    /// XP amount itself before the original method applies it, which is
    /// exactly what a Harmony Prefix is for.
    ///
    /// SOURCED (not guessed): HeroDeveloper.AddSkillXp's signature —
    /// (SkillObject skill, float rawXp, bool isAffectedByFocusFactor, bool
    /// shouldNotify) — and HeroDeveloper.Hero (get/set) were both confirmed
    /// via reflection against your assembly. Harmony Prefix patches can
    /// modify a parameter's value via `ref` even when the original method
    /// didn't itself declare that parameter as ref/out — a standard,
    /// well-established Harmony capability for exactly this kind of
    /// "adjust an argument before the original runs" patch.
    /// </summary>
    [HarmonyPatch(typeof(HeroDeveloper), "AddSkillXp")]
    public static class HeroDeveloper_KingsguardXp_Patch
    {
        private static void Prefix(HeroDeveloper __instance, SkillObject skill, ref float rawXp, bool isAffectedByFocusFactor, bool shouldNotify)
        {
            Hero hero = __instance?.Hero;
            if (hero == null)
            {
                return;
            }

            float multiplier = KingsguardBonusService.GetXpMultiplier(hero);
            if (multiplier != 1f)
            {
                rawXp *= multiplier;
            }
        }
    }
}
