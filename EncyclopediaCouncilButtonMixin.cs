using SmallCouncils.UI;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace SmallCouncils.UI.Encyclopedia
{
    /// <summary>
    /// Adds a "Council" button to the Encyclopedia's Kingdom (Faction) page,
    /// opening a read-only viewer of that kingdom's council — works for ANY
    /// kingdom being viewed, not just the player's own, since this is purely
    /// informational (no ruler restriction, unlike the Kingdom Management
    /// screen's editable version).
    ///
    /// SOURCED (not guessed):
    /// - EncyclopediaFactionPageVM class/namespace and its "RefreshValues"
    ///   method were confirmed via reflection.
    /// - EncyclopediaPageVM.Obj (the base class property holding the actual
    ///   entity being viewed, typed System.Object) was confirmed via
    ///   reflection on the base type.
    ///
    /// COMMAND HANDLING MOVED: like the Kingdom screen button, the click
    /// command never actually fired despite [DataSourceMethod] and a working
    /// UIExtenderEx update — confirmed via testing that hover/click both
    /// reach the widget (native highlight + click sound) but the bound
    /// command itself never executes. This points to a genuine break in
    /// UIExtenderEx's own ExecuteCommand interception on this Bannerlord
    /// version. Click handling now lives in CouncilCommandRoutingPatch, a
    /// direct Harmony patch that bypasses UIExtenderEx's command routing
    /// entirely. This mixin now only carries the properties that DO work
    /// (text, visibility) through normal property binding.
    /// </summary>
    [ViewModelMixin("RefreshValues")]
    public class EncyclopediaCouncilButtonMixin : BaseViewModelMixin<EncyclopediaFactionPageVM>
    {
        private readonly EncyclopediaFactionPageVM _vm;

        private bool _isCouncilVisible;
        private string _councilButtonText;

        public EncyclopediaCouncilButtonMixin(EncyclopediaFactionPageVM vm) : base(vm)
        {
            _vm = vm;
            _councilButtonText = "Council";

            RefreshCouncilVisibility();
        }

        private void RefreshCouncilVisibility()
        {
            IsCouncilVisible = _vm.Obj is Kingdom;
        }

        [DataSourceProperty]
        public bool IsCouncilVisible
        {
            get => _isCouncilVisible;
            set
            {
                if (value != _isCouncilVisible)
                {
                    _isCouncilVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsCouncilVisible));
                }
            }
        }

        [DataSourceProperty]
        public string CouncilButtonText
        {
            get => _councilButtonText;
            set
            {
                if (value != _councilButtonText)
                {
                    _councilButtonText = value;
                    OnPropertyChangedWithValue(value, nameof(CouncilButtonText));
                }
            }
        }
    }
}
