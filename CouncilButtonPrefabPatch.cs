using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace SmallCouncils.UI.KingdomButton
{
    /// <summary>
    /// Injects a "Council" button into the native Kingdom Management screen's
    /// top panel, positioned beside the Abdicate Leadership / Leave Kingdom
    /// button (a stable, uniquely-Id'd anchor point) rather than inside the
    /// tab row, since ROT 8.0 is already known to modify the tab row itself
    /// (it adds a 6th "Factions" tab not present in the vanilla file) and we
    /// don't have visibility into exactly what that looks like. Originally
    /// placed below AbdicateLeadershipButton, but that gap turned out too
    /// tight (TopPanel is only 196px tall and the tab row starts almost
    /// immediately underneath it) — moved to sit beside it on the same row
    /// instead.
    ///
    /// Reuses the native "Header.Tab.Center" brush/size constants already
    /// defined in KingdomManagement.xml, so the button visually matches the
    /// existing tab buttons rather than the alarming red "Leave Kingdom"
    /// style — this directly serves the "should look like it fits in"
    /// requirement while only referencing brushes/constants we've confirmed
    /// exist in this exact file.
    ///
    /// SOURCED (not guessed): the PrefabExtensionInsertPatch pattern, the
    /// [PrefabExtension(Movie, XPath)] attribute, InsertType, and
    /// [PrefabExtensionXmlDocument] were confirmed against UIExtenderEx's own
    /// GitHub source/docs, not assumed.
    ///
    /// CONFIRMED (not guessed) against the uploaded KingdomManagement.xml:
    /// - "Header.Tab.Center" and "Clan.TabControl.Text" are real brushes
    ///   already used identically by the native Fiefs/Policies/Armies tab
    ///   buttons in this exact file.
    /// - AbdicateLeadershipButton's own position (HorizontalAlignment="Right"
    ///   MarginRight="290", no explicit MarginTop) confirms it sits near the
    ///   top of the panel like its siblings — MarginTop="85" below is a
    ///   reasonable estimate for "just under it" given its ~70px height.
    ///
    /// BUGFIX (found via UIExtenderEx's DumpXML output): InsertType.Append
    /// does NOT insert as a new child inside the XPath-matched node — it
    /// inserts as the next XML SIBLING after it. Targeting a &lt;Children&gt;
    /// element directly (as we originally did) placed our widget outside
    /// that Children block entirely, which the renderer silently ignores.
    /// The fix is to target an element that's already INSIDE the desired
    /// Children collection (here, AbdicateLeadershipButton) so our widget
    /// lands as its sibling — i.e. genuinely inside the same collection.
    ///
    /// VERIFY BEFORE COMPILE / IN-GAME:
    /// - "KingdomManagement" as the Movie name — CONFIRMED working (visible
    ///   in-game as of this fix), matching the documented convention seen in
    ///   UIExtenderEx examples (e.g. "MapBar" <-> MapBar.xml).
    /// - The tooltip itself is a plain custom Widget bound to
    ///   IsCouncilHintVisible/CouncilHintText rather than the native
    ///   HintWidget pattern, since that relies on internal hint-system
    ///   plumbing I haven't verified.
    /// - UpdateChildrenStates="true" is back on the ButtonWidget — a fresh
    ///   dump revealed EVERY native tab button has this unconditionally,
    ///   including ones with no dynamic IsEnabled at all (ClanTabButton,
    ///   ArmiesTabButton, DiplomacyTabButton), suggesting it's required for
    ///   child text to render at all, not specifically tied to the
    ///   disabled-state theory it was removed for earlier.
    ///
    /// ROOT CAUSE FOUND (confirmed via A/B comparison, not another guess):
    /// the identical brush/TextWidget setup was used for the Encyclopedia
    /// council button (EncyclopediaCouncilButtonPrefabPatch) with no
    /// IsEnabled binding at all, and its text rendered fine immediately. The
    /// only difference here was IsEnabled="@IsCouncilEnabled" — removing it
    /// entirely (this was the user's very first hypothesis, which turned out
    /// correct) fixed the text. The "can't click when not ruler" behavior
    /// still works correctly, just enforced in ExecuteOpenCouncil's own
    /// logic (returns early if not the ruler) rather than via the widget's
    /// native disabled-state rendering, which was silently eating the text
    /// for reasons never fully diagnosed beyond "IsEnabled is the trigger".
    /// </summary>
    [PrefabExtension("KingdomManagement", "descendant::Widget[@Id='AbdicateLeadershipButton']")]
    public class CouncilButtonPrefabPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;

        private readonly XmlDocument _document;

        public CouncilButtonPrefabPatch()
        {
            _document = new XmlDocument();

            // Grounded in an actual full-header screenshot measurement:
            // AbdicateLeadershipButton occupies roughly TopPanel's top 70 units
            // (matching its own SuggestedHeight="70" with no top margin), and
            // the tab row (anchored VerticalAlignment="Bottom" MarginBottom="38")
            // starts roughly 40 units below that. MarginTop=75 + Height=30 fits
            // inside that gap with a few units of clearance on each side.
            // MarginRight=355 centers this button under AbdicateLeadershipButton:
            // that button's center offset from the parent's right edge is
            // MarginRight(290) + Width/2(140) = 430; ours (width 150) needs
            // MarginRight = 430 - 75 = 355 for the same center.
            _document.LoadXml(
                "<ButtonWidget Id=\"CouncilButton\" DoNotPassEventsToChildren=\"true\" WidthSizePolicy=\"Fixed\" HeightSizePolicy=\"Fixed\" SuggestedWidth=\"150\" SuggestedHeight=\"30\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" MarginRight=\"355\" MarginTop=\"66\" Brush=\"ButtonBrush2\" Command.Click=\"SmallCouncils_OpenCouncil\" UpdateChildrenStates=\"true\" GamepadNavigationIndex=\"0\">" +
                "<Children>" +
                "<TextWidget WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\" Brush=\"Clan.TabControl.Text\" Text=\"Council\" />" +
                "</Children>" +
                "</ButtonWidget>");
        }

        [PrefabExtensionXmlDocument]
        public XmlDocument GetPrefabExtension()
        {
            return _document;
        }
    }
}
