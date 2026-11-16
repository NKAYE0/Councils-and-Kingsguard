using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace SmallCouncils.HarmonyPatches
{
    /// <summary>
    /// Applies a council relation change through the native ChangeRelationAction
    /// (relation, notification and campaign event all unchanged) while blocking
    /// the Charm XP that the game would normally award for it.
    /// </summary>
    public static class CouncilRelationChange
    {
        // Greater than zero while a council relation change is being applied.
        private static int _suppressDepth;

        public static bool IsSuppressingCharmXp => _suppressDepth > 0;

        public static void ApplyWithoutCharmXp(Hero hero, Hero gainedRelationWith, int relationChange, bool showQuickNotification)
        {
            _suppressDepth++;
            try
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, gainedRelationWith, relationChange, showQuickNotification);
            }
            finally
            {
                _suppressDepth--;
            }
        }
    }

    /// <summary>
    /// Skips the relation-gain Charm XP award only while a council relation
    /// change is in progress; every other relation gain behaves as normal.
    /// </summary>
    [HarmonyPatch(typeof(SkillLevelingManager), nameof(SkillLevelingManager.OnGainRelation))]
    public static class SkillLevelingManager_CouncilCharmXp_Patch
    {
        private static bool Prefix()
        {
            return !CouncilRelationChange.IsSuppressingCharmXp;
        }
    }
}
