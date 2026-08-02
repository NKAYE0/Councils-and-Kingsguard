namespace SmallCouncils.Models
{
    public static class CouncilPositionDisplay
    {
        public static string GetName(CouncilPosition position)
        {
            switch (position)
            {
                case CouncilPosition.HandOfTheKing:
                    return "Hand of the King";
                case CouncilPosition.GrandMaester:
                    return "Grand Maester";
                case CouncilPosition.MasterOfCoin:
                    return "Master of Coin";
                case CouncilPosition.MasterOfLaws:
                    return "Master of Laws";
                case CouncilPosition.MasterOfShips:
                    return "Master of Ships";
                case CouncilPosition.MasterOfWhisperers:
                    return "Master of Whisperers";
                case CouncilPosition.LordCommanderOfKingsguard:
                    return "Lord Commander of the Kingsguard";
                default:
                    return position.ToString();
            }
        }
    }
}
