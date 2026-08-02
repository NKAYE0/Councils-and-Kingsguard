using System;
using System.Reflection;
using HarmonyLib;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Defensive Harmony Finalizer patch suppressing a crash confirmed via
    /// two separate crash reports: opening our CouncilViewScreen while at
    /// sea/on the campaign map causes NavalDLC's own
    /// GauntletNavalMapBarGlobalLayer.HandlePanelSwitchingInput to throw
    /// ArgumentOutOfRangeException from IsGameKeyReleased(37) — an internal
    /// game-key array bounds check deep in NavalDLC/engine code.
    ///
    /// This is NOT caused by our screen-pushing logic — an earlier fix to a
    /// genuine bug we found in our own code (SetInputRestrictions was
    /// calling a nonexistent overload) did not resolve this crash, which
    /// happened identically before and after that fix. Given how many mods
    /// in a typical install touch game-key/hotkey registration, this looks
    /// like a pre-existing latent bug in this specific mod combination that
    /// pushing any new screen while at sea happens to surface — not
    /// something we can safely fix at the root (it's deep in NavalDLC's own
    /// compiled code), so we suppress the specific symptom instead.
    ///
    /// This Finalizer catches ONLY ArgumentOutOfRangeException from this one
    /// method and returns false (meaning "no panel-switch hotkey was
    /// pressed this frame") instead of crashing — a safe default given the
    /// method's only job is checking a hotkey and reacting to it. Any other
    /// exception type is left to propagate normally.
    ///
    /// Applied manually (not via [HarmonyPatch] attribute scanning) so it
    /// gracefully does nothing if NavalDLC isn't installed, matching its
    /// listing as an optional dependency in our own SubModule.xml.
    /// </summary>
    public static class NavalMapBarCrashSuppressionPatch
    {
        public static void ApplyIfApplicable(Harmony harmony)
        {
            try
            {
                Type targetType = AccessTools.TypeByName("NavalDLC.GauntletUI.Map.GauntletNavalMapBarGlobalLayer");
                if (targetType == null)
                {
                    return;
                }

                Type inputContextType = AccessTools.TypeByName("TaleWorlds.InputSystem.InputContext");
                if (inputContextType == null)
                {
                    return;
                }

                MethodInfo original = AccessTools.Method(targetType, "HandlePanelSwitchingInput", new[] { inputContextType });
                if (original == null)
                {
                    return;
                }

                MethodInfo finalizer = typeof(NavalMapBarCrashSuppressionPatch).GetMethod(
                    nameof(Finalizer), BindingFlags.Static | BindingFlags.NonPublic);

                harmony.Patch(original, finalizer: new HarmonyMethod(finalizer));
            }
            catch
            {
                // This is a defensive, best-effort patch — if anything about
                // applying it fails, skip silently rather than risk breaking
                // mod load entirely over an optional crash workaround.
            }
        }

        private static Exception Finalizer(Exception __exception, ref bool __result)
        {
            if (__exception is ArgumentOutOfRangeException)
            {
                __result = false;
                return null;
            }

            return __exception;
        }
    }
}
