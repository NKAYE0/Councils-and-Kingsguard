using Bannerlord.UIExtenderEx;
using HarmonyLib;
using SmallCouncils.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SmallCouncils
{
    /// <summary>
    /// VERIFY BEFORE FIRST COMPILE:
    /// - Confirm MBSubModuleBase and its OnGameStart/OnSubModuleLoad override
    ///   signatures against your TaleWorlds.MountAndBlade.dll reference.
    /// - Confirm CampaignGameStarter is the correct starter object type passed
    ///   into a Campaign game's OnGameStart, and that AddBehavior lives on it.
    /// This mirrors the standard SubModule pattern used across nearly all
    /// Bannerlord mods, but I have not compiled it against your project.
    /// - UIExtender.Create(id) / .Register(Assembly) / .Enable() — standard
    ///   UIExtenderEx initialization pattern, sourced from its documentation
    ///   alongside the PrefabExtension pattern, not a blind guess, but still
    ///   worth a compile-check against your installed version.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        private const string HarmonyDomainId = "com.smallcouncils.mod";
        private const string UIExtenderId = "SmallCouncils";

        private Harmony _harmony;
        private UIExtender _uiExtender;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            _harmony = new Harmony(HarmonyDomainId);
            _harmony.PatchAll();
            SmallCouncils.HarmonyPatches.NavalMapBarCrashSuppressionPatch.ApplyIfApplicable(_harmony);

            _uiExtender = UIExtender.Create(UIExtenderId);
            _uiExtender.Register(typeof(SubModule).Assembly);
            _uiExtender.Enable();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (game.GameType is Campaign)
            {
                var campaignStarter = (CampaignGameStarter)gameStarterObject;
                campaignStarter.AddBehavior(new CouncilBehavior());
                campaignStarter.AddBehavior(new CouncilAIBehavior());
                campaignStarter.AddBehavior(new CouncilBenefitsBehavior());
            }
        }
    }
}
