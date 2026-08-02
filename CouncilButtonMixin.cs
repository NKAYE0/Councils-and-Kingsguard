using SmallCouncils.UI;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Library;

namespace SmallCouncils.UI.KingdomButton
{
    /// <summary>
    /// Adds the Council button's bindable state (enabled/tooltip/click) to the
    /// native Kingdom Management screen's ViewModel via UIExtenderEx, without
    /// modifying the native class itself.
    ///
    /// VERIFY BEFORE COMPILE:
    /// - Confirmed via reflection: KingdomManagementVM lives in
    ///   TaleWorlds.CampaignSystem.ViewModelCollection.dll, namespace
    ///   TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.
    /// - CONFIRMED via reflection: KingdomManagementVM has a real
    ///   RefreshValues method, now passed to [ViewModelMixin("RefreshValues")]
    ///   below — this was likely the actual cause of the button text not
    ///   rendering (bindings may not have been wired up correctly without it,
    ///   even though the button's own default values happened to look
    ///   plausible without it).
    ///
    /// ACTUAL ROOT CAUSE OF THE CLICK NOT FIRING (found via real published
    /// examples, not another guess): mixin COMMAND methods need a
    /// [DataSourceMethod] attribute, the method-equivalent of
    /// [DataSourceProperty] on properties. We had that on every property but
    /// never on the Execute* methods, so UIExtenderEx never registered them
    /// as bindable commands.
    ///
    /// ...HOWEVER, adding [DataSourceMethod] still didn't fix it. Confirmed
    /// via testing that mouse input genuinely reaches the widget (hover
    /// highlight and click sound both fire — native ButtonWidget behavior,
    /// independent of command binding), but our bound commands never
    /// execute (neither Command.Click nor Command.HoverBegin). This points
    /// to UIExtenderEx's own interception of ViewModel.ExecuteCommand being
    /// broken on this exact Bannerlord version, unrelated to anything on our
    /// side — updating to UIExtenderEx 2.13.3 didn't help either.
    ///
    /// WORKAROUND: command handling has been moved entirely to
    /// CouncilCommandRoutingPatch, a direct Harmony patch on
    /// ViewModel.ExecuteCommand that doesn't depend on UIExtenderEx's mixin
    /// command routing at all. This mixin now only carries the button TEXT
    /// (which — unlike commands — has been confirmed working through the
    /// normal property-binding path). The hover-tooltip mechanism has been
    /// dropped entirely in favor of a simple on-click information message,
    /// since it depended on the same broken hover-command pathway.
    /// </summary>
    [ViewModelMixin("RefreshValues")]
    public class CouncilButtonMixin : BaseViewModelMixin<KingdomManagementVM>
    {
        private string _councilButtonText;

        public CouncilButtonMixin(KingdomManagementVM vm) : base(vm)
        {
            _councilButtonText = "Council";
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
