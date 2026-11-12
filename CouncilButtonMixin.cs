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
    /// UPDATE (Bannerlord 1.4.8 / UIExtenderEx 3.0.3 compatibility fix):
    /// the Harmony-postfix-on-ViewModel.ExecuteCommand workaround that used
    /// to live here (CouncilCommandRoutingPatch, now deleted) stopped firing
    /// entirely once UIExtenderEx's Compiled Prefabs engine became the
    /// default (v3.0+). Under that engine, a mixin's [DataSourceMethod]
    /// commands are resolved via direct, strongly-typed generated calls
    /// (ViewModelMixins.Get&lt;TMixin&gt;(vm).ExecuteX()) rather than by
    /// routing through ViewModel.ExecuteCommand — so a raw Harmony patch on
    /// that method never sees commands that are properly bound to a mixin.
    /// Moving the click handler back onto a real [DataSourceMethod] here is
    /// the path the compiled-prefab pipeline is actually built to resolve.
    ///
    /// handleDerived: true added (2026-11-12) as a defensive fix: this
    /// click still didn't fire even with the command binding corrected,
    /// while the identical pattern worked immediately for the Encyclopedia
    /// button. By default a mixin only attaches to an EXACT runtime-type
    /// match for TViewModel; if anything in the load order (ROT, NavalDLC,
    /// etc.) instantiates a subclass of KingdomManagementVM for this
    /// screen, the mixin would silently never attach at all -- UIExtenderEx
    /// 3.0 specifically made that case fail silently with just a log
    /// warning rather than an error. The TextWidget's Text in
    /// CouncilButtonPrefabPatch.cs was also switched from a hardcoded
    /// literal back to "@CouncilButtonText" so attachment is now visibly
    /// verifiable in-game: if "Council" stops rendering, the mixin isn't
    /// attaching and this theory is confirmed; if it still renders and the
    /// click still silently does nothing, the problem is elsewhere.
    /// </summary>
    [ViewModelMixin("RefreshValues", handleDerived: true)]
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

        /// <summary>
        /// Bound via Command.Click="OpenCouncil" on CouncilButton
        /// (CouncilButtonPrefabPatch.cs). Matches the "Execute" + command
        /// name convention UIExtenderEx's mixin command binding expects.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteOpenCouncil()
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

            CouncilScreenManager.OpenAssignmentScreen(kingdom);
        }
    }
}
