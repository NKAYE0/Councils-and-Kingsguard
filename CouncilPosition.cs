namespace SmallCouncils.Models
{
    /// <summary>
    /// The seven Small Council positions. Numeric values are fixed and must
    /// never be reordered/renumbered once released, since they are persisted
    /// in save games via CouncilSaveDefiner's enum definition.
    /// </summary>
    public enum CouncilPosition
    {
        HandOfTheKing = 0,
        GrandMaester = 1,
        MasterOfCoin = 2,
        MasterOfLaws = 3,
        MasterOfShips = 4,
        MasterOfWhisperers = 5,
        LordCommanderOfKingsguard = 6
    }
}
