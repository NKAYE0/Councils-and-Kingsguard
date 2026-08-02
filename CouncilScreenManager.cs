using TaleWorlds.CampaignSystem;
using TaleWorlds.ScreenSystem;

namespace SmallCouncils.UI
{
    public static class CouncilScreenManager
    {
        public static void OpenAssignmentScreen(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return;
            }

            ScreenManager.PushScreen(new CouncilAssignmentScreen(kingdom));
        }

        /// <summary>Opens the read-only council viewer for any kingdom.</summary>
        public static void OpenViewScreen(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return;
            }

            ScreenManager.PushScreen(new CouncilViewScreen(kingdom));
        }
    }
}
