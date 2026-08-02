using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace SmallCouncils.UI.Encyclopedia
{
    /// <summary>
    /// Injects a "Council" button next to the Leader element on the
    /// Encyclopedia's Kingdom (Faction) page.
    ///
    /// CONFIRMED against the uploaded EncyclopediaFactionPage.xml:
    /// - Movie name "EncyclopediaFactionPage" matches the prefab filename.
    /// - The Leader element is:
    ///   &lt;EncyclopediaSubPageElement Id="Leader" DataSource="{Leader}"
    ///   HorizontalAlignment="Left" SuggestedHeight="89" SuggestedWidth="123"
    ///   MarginLeft="20" MarginTop="10" .../&gt;
    ///   — sitting inside a ListPanel with
    ///   StackLayout.LayoutMethod="VerticalBottomToTop".
    ///
    /// GENUINELY UNCERTAIN (flagging honestly rather than presenting as
    /// solved): because the Leader element lives in a VERTICALLY stacking
    /// panel, a plain sibling-after insertion (per our confirmed
    /// Append-inserts-as-sibling behavior) would normally stack our button
    /// BELOW the whole Leader row, not beside it. I'm using
    /// HorizontalAlignment="Left" with MarginLeft="160" (clearing Leader's
    /// own 123 width + 20 left margin, plus a small gap) combined with a
    /// negative PositionYOffset to pull it back up into the same visual row
    /// as Leader — but the exact offset needed depends on stack spacing I
    /// can't determine from the XML alone. Treat MarginTop/PositionYOffset
    /// here as a first attempt requiring visual confirmation, exactly like
    /// the Kingdom screen button did.
    ///
    /// Reuses ButtonBrush2 / Kingdom.GeneralButtons.Text — real, confirmed
    /// brushes from Diplomacy's own shipped EncyclopediaHeroPageInject.xml
    /// and FactionButtonInject.xml, per your request to match that look.
    /// </summary>
    [PrefabExtension("EncyclopediaFactionPage", "descendant::EncyclopediaSubPageElement[@Id='Leader']")]
    public class EncyclopediaCouncilButtonPrefabPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;

        private readonly XmlDocument _document;

        public EncyclopediaCouncilButtonPrefabPatch()
        {
            _document = new XmlDocument();

            _document.LoadXml(
                "<ButtonWidget Id=\"EncyclopediaCouncilButton\" IsVisible=\"@IsCouncilVisible\" DoNotPassEventsToChildren=\"true\" WidthSizePolicy=\"Fixed\" HeightSizePolicy=\"Fixed\" SuggestedWidth=\"227\" SuggestedHeight=\"40\" HorizontalAlignment=\"Left\" MarginLeft=\"160\" MarginTop=\"10\" PositionYOffset=\"-70\" Brush=\"ButtonBrush2\" Command.Click=\"SmallCouncils_OpenCouncilView\" UpdateChildrenStates=\"true\" GamepadNavigationIndex=\"0\">" +
                "<Children>" +
                "<TextWidget WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\" Brush=\"Kingdom.GeneralButtons.Text\" Text=\"@CouncilButtonText\" />" +
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
