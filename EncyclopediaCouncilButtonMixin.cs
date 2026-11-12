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
    /// UPDATE (Bannerlord 1.4.8 / UIExtenderEx 3.0.3 compatibility fix):
    /// the Harmony-postfix-on-ViewModel.ExecuteCommand workaround that used
    /// to live here (CouncilCommandRoutingPatch, now deleted) stopped firing
    /// once UIExtenderEx's Compiled Prefabs engine became the default
    /// (v3.0+): mixin [DataSourceMethod] commands are now resolved via
    /// direct generated calls (ViewModelMixins.Get&lt;TMixin&gt;(vm).ExecuteX())
    /// rather than by routing through ViewModel.ExecuteCommand, so a plain
    /// Harmony patch on that method never saw them. Restoring a real
    /// [DataSourceMethod] here is the path the compiled-prefab pipeline
    /// actually resolves, and confirmed fixed (2026-11-12) against 1.4.8.
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

        /// <summary>
        /// Bound via Command.Click="OpenCouncilView" on
        /// EncyclopediaCouncilButton (EncyclopediaCouncilButtonPrefabPatch.cs).
        /// Works for any kingdom being viewed, not just the player's own —
        /// purely informational, unlike the Kingdom Management button.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteOpenCouncilView()
        {
            if (_vm.Obj is Kingdom kingdom)
            {
                CouncilScreenManager.OpenViewScreen(kingdom);
            }
        }
    }
}
