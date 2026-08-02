using SmallCouncils.Models;

namespace SmallCouncils.Settings
{
    /// <summary>
    /// Central lookup for the numeric values driving council behaviour.
    ///
    /// Stage 7: now backed by CouncilSettings (MCM) instead of hardcoded
    /// dictionaries — nothing that calls into CouncilConstants needed to
    /// change, since the public method signatures stayed the same. Falls
    /// back to the original hardcoded defaults if CouncilSettings.Instance
    /// is ever null (e.g. very early in load order, before MCM finishes
    /// initializing).
    /// </summary>
    public static class CouncilConstants
    {
        public static int GetAssignRelationGain(CouncilPosition position)
        {
            CouncilSettings s = CouncilSettings.Instance;
            switch (position)
            {
                case CouncilPosition.HandOfTheKing:
                    return s?.RelationGain_HandOfTheKing ?? 100;
                case CouncilPosition.GrandMaester:
                    return s?.RelationGain_GrandMaester ?? 20;
                case CouncilPosition.MasterOfCoin:
                    return s?.RelationGain_MasterOfCoin ?? 30;
                case CouncilPosition.MasterOfLaws:
                    return s?.RelationGain_MasterOfLaws ?? 30;
                case CouncilPosition.MasterOfShips:
                    return s?.RelationGain_MasterOfShips ?? 30;
                case CouncilPosition.MasterOfWhisperers:
                    return s?.RelationGain_MasterOfWhisperers ?? 20;
                case CouncilPosition.LordCommanderOfKingsguard:
                    return s?.RelationGain_LordCommanderOfKingsguard ?? 100;
                default:
                    return 0;
            }
        }

        public static int GetUnassignRelationLoss(CouncilPosition position)
        {
            CouncilSettings s = CouncilSettings.Instance;
            switch (position)
            {
                case CouncilPosition.HandOfTheKing:
                    return s?.RelationLoss_HandOfTheKing ?? -50;
                case CouncilPosition.GrandMaester:
                    return s?.RelationLoss_GrandMaester ?? -20;
                case CouncilPosition.MasterOfCoin:
                    return s?.RelationLoss_MasterOfCoin ?? -30;
                case CouncilPosition.MasterOfLaws:
                    return s?.RelationLoss_MasterOfLaws ?? -30;
                case CouncilPosition.MasterOfShips:
                    return s?.RelationLoss_MasterOfShips ?? -30;
                case CouncilPosition.MasterOfWhisperers:
                    return s?.RelationLoss_MasterOfWhisperers ?? -20;
                case CouncilPosition.LordCommanderOfKingsguard:
                    return s?.RelationLoss_LordCommanderOfKingsguard ?? -5;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Relation with the ruler at or below this value marks a council
        /// member eligible for AI-side removal (Stage 3). Per spec, relation
        /// ranges -100 to 100 and "negative reputation" means below zero.
        /// </summary>
        public static int NegativeRelationThreshold => CouncilSettings.Instance?.NegativeRelationThreshold ?? 0;
    }
}
