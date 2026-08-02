using SmallCouncils.UI.View;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace SmallCouncils.UI
{
    /// <summary>
    /// Hosts the read-only council viewer, mirroring CouncilAssignmentScreen's
    /// structure but for CouncilViewVM/CouncilViewScreen.xml instead.
    ///
    /// BUGFIX: SetInputRestrictions() was originally called with no
    /// arguments — but reflection confirmed that overload doesn't actually
    /// exist; the only real signature is
    /// SetInputRestrictions(bool isMouseVisible, InputUsageMask mask).
    /// Whatever the no-arg call actually resolved to wasn't properly
    /// blocking input to the underlying map screen, which let a dormant,
    /// unrelated NavalDLC bug in its own hotkey-checking code
    /// (GauntletNavalMapBarGlobalLayer.HandlePanelSwitchingInput) get hit —
    /// that code should never tick while a modal screen like this one has
    /// focus. Fixed to use the real two-argument overload, matching the
    /// common (true, InputUsageMask.All) pattern for a modal Gauntlet screen
    /// that needs a visible, fully input-capturing mouse cursor.
    /// </summary>
    public class CouncilViewScreen : ScreenBase
    {
        private readonly Kingdom _kingdom;
        private CouncilViewVM _dataSource;
        private GauntletLayer _gauntletLayer;

        public CouncilViewScreen(Kingdom kingdom)
        {
            _kingdom = kingdom;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _dataSource = new CouncilViewVM(_kingdom);
            _dataSource.CloseRequested += OnCloseRequested;

            _gauntletLayer = new GauntletLayer("GauntletLayer", 1, false);
            _gauntletLayer.LoadMovie("CouncilViewScreen", _dataSource);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, TaleWorlds.Library.InputUsageMask.All);

            AddLayer(_gauntletLayer);
            _gauntletLayer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(_gauntletLayer);
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();

            if (_dataSource != null)
            {
                _dataSource.CloseRequested -= OnCloseRequested;
            }

            _gauntletLayer = null;
            _dataSource = null;
        }

        private void OnCloseRequested()
        {
            ScreenManager.PopScreen();
        }
    }
}
