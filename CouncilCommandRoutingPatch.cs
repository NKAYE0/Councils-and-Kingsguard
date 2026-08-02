using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Library;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Handles both council buttons' clicks directly via Harmony, bypassing
    /// UIExtenderEx's ViewModelMixin command routing entirely.
    ///
    /// WHY THIS EXISTS: extensive testing confirmed that mouse input
    /// genuinely reaches our injected buttons (native hover-highlight and
    /// click-sound both fire, proving the widgets themselves are fully
    /// interactive), but neither Command.Click nor Command.HoverBegin ever
    /// actually invoked our mixin methods, even after adding the required
    /// [DataSourceMethod] attribute and updating UIExtenderEx to the latest
    /// version (2.13.3). This points to a genuine break in UIExtenderEx's
    /// own interception of ViewModel.ExecuteCommand on this exact Bannerlord
    /// version — not something fixable from our side within UIExtenderEx's
    /// own mixin system.
    ///
    /// Since we already reliably patch other native methods via Harmony
    /// throughout this project, patching ExecuteCommand ourselves sidesteps
    /// UIExtenderEx's broken interception entirely: our own patch on the
    /// same method fires independently and doesn't depend on their patch
    /// succeeding.
    ///
    /// SOURCED: ExecuteCommand(string commandName, object[] parameters) on
    /// TaleWorlds.Library.ViewModel was confirmed via reflection.
    ///
    /// The command name strings ("SmallCouncils_OpenCouncil" /
    /// "SmallCouncils_OpenCouncilView") are arbitrary and namespaced
    /// specifically to avoid any chance of colliding with a real native or
    /// other-mod command name, since this patch fires for every VM's
    /// ExecuteCommand call in the game, not just ours.
    /// </summary>
    [HarmonyPatch(typeof(ViewModel), "ExecuteCommand")]
    public static class CouncilCommandRoutingPatch
    {
        private static void Postfix(ViewModel __instance, string commandName, object[] parameters)
        {
            switch (commandName)
            {
                case "SmallCouncils_OpenCouncil" when __instance is KingdomManagementVM:
                    HandleOpenCouncil();
                    break;

                case "SmallCouncils_OpenCouncilView" when __instance is EncyclopediaFactionPageVM pageVm:
                    HandleOpenCouncilView(pageVm);
                    break;
            }
        }

        private static void HandleOpenCouncil()
        {
            Kingdom kingdom = Hero.MainHero?.MapFaction as Kingdom;
            if (kingdom == null)
            {
                return;
            }

            if (kingdom.Leader != Hero.MainHero)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "You must be the ruler of a kingdom to access the Small Council."));
                return;
            }

            SmallCouncils.UI.CouncilScreenManager.OpenAssignmentScreen(kingdom);
        }

        private static void HandleOpenCouncilView(EncyclopediaFactionPageVM pageVm)
        {
            if (pageVm.Obj is Kingdom kingdom)
            {
                SmallCouncils.UI.CouncilScreenManager.OpenViewScreen(kingdom);
            }
        }
    }
}
