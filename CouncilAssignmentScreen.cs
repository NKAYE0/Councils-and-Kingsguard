using SmallCouncils.UI.Assignment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace SmallCouncils.UI
{
    /// <summary>
    /// Hosts the council assignment screen as a standalone Gauntlet layer
    /// pushed on top of the map (or wherever the player opened it from).
    /// This is a self-contained custom screen — it doesn't modify any native
    /// file, so mistakes here are isolated to our own screen rather than
    /// risking native UI breakage.
    ///
    /// VERIFY BEFORE COMPILE: this follows the standard minimal
    /// ScreenBase + GauntletLayer pattern used across many simple custom
    /// Bannerlord screens (OnInitialize/OnFinalize overrides, GauntletLayer
    /// construction, LoadMovie, AddLayer, input restrictions). I'm reasonably
    /// confident in the overall shape but have not compiled it against your
    /// project — expect possible signature mismatches on GauntletLayer's
    /// constructor or ScreenBase's override signatures.
    /// </summary>
    public class CouncilAssignmentScreen : ScreenBase
    {
        private readonly Kingdom _kingdom;
        private CouncilAssignmentVM _dataSource;
        private GauntletLayer _gauntletLayer;

        public CouncilAssignmentScreen(Kingdom kingdom)
        {
            _kingdom = kingdom;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _dataSource = new CouncilAssignmentVM(_kingdom);
            _dataSource.CloseRequested += OnCloseRequested;

            _gauntletLayer = new GauntletLayer("GauntletLayer", 1, false);
            _gauntletLayer.LoadMovie("CouncilAssignmentScreen", _dataSource);
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
