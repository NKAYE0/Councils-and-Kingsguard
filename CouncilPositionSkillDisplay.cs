using SmallCouncils.Models;
using SmallCouncils.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SmallCouncils.UI.Assignment
{
    /// <summary>
    /// Shared skill/stat lookup used by both the candidate picker and the
    /// main position rows on the assignment screen. Deliberately a
    /// standalone class rather than living inside either VM — an earlier
    /// version had this logic as a public static method on
    /// CouncilAssignmentVM, called cross-class from CouncilPositionItemVM;
    /// that specific cross-class call reliably failed to populate its
    /// target property (confirmed via isolated testing: the property/
    /// binding mechanism worked fine with a hardcoded value, and this exact
    /// method worked fine when called from CouncilAssignmentVM's own
    /// candidate-picker code — only the CouncilPositionItemVM-calling-
    /// CouncilAssignmentVM path failed, for reasons that weren't pinned
    /// down). Moving the logic here, with both VMs calling this same
    /// independent class identically, sidesteps that failure mode entirely.
    /// </summary>
    public static class CouncilPositionSkillDisplay
    {
        public static string GetRelevantSkillText(CouncilPosition position, Hero hero)
        {
            switch (position)
            {
                case CouncilPosition.HandOfTheKing:
                    return $"Clan Strength: {(int)System.Math.Round(hero.Clan?.CurrentTotalStrength ?? 0f)}";
                case CouncilPosition.GrandMaester:
                    return $"Medicine: {hero.GetSkillValue(DefaultSkills.Medicine)}";
                case CouncilPosition.MasterOfCoin:
                    return $"Steward: {hero.GetSkillValue(DefaultSkills.Steward)}";
                case CouncilPosition.MasterOfLaws:
                    return $"Leadership: {hero.GetSkillValue(DefaultSkills.Leadership)}";
                case CouncilPosition.MasterOfWhisperers:
                    return $"Roguery: {hero.GetSkillValue(DefaultSkills.Roguery)}";
                case CouncilPosition.MasterOfShips:
                    SkillObject shipmaster = NavalSkillLookup.ShipmasterSkill;
                    return shipmaster != null ? $"Shipmaster: {hero.GetSkillValue(shipmaster)}" : string.Empty;
                case CouncilPosition.LordCommanderOfKingsguard:
                    return $"Vigor: {GetVigorSkillValue(hero)}";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Numeric value of whichever skill matters for this position, used
        /// to sort the candidate picker with the best-suited heroes first.
        /// Positions with no relevant skill (Hand of the King) sort as 0 —
        /// GetRelevantCandidatesForPosition doesn't offer a picker sorted by
        /// skill for those anyway, so this is just a safe default.
        /// </summary>
        public static int GetRelevantSkillValue(CouncilPosition position, Hero hero)
        {
            switch (position)
            {
                case CouncilPosition.HandOfTheKing:
                    return (int)System.Math.Round(hero.Clan?.CurrentTotalStrength ?? 0f);
                case CouncilPosition.GrandMaester:
                    return hero.GetSkillValue(DefaultSkills.Medicine);
                case CouncilPosition.MasterOfCoin:
                    return hero.GetSkillValue(DefaultSkills.Steward);
                case CouncilPosition.MasterOfLaws:
                    return hero.GetSkillValue(DefaultSkills.Leadership);
                case CouncilPosition.MasterOfWhisperers:
                    return hero.GetSkillValue(DefaultSkills.Roguery);
                case CouncilPosition.MasterOfShips:
                    SkillObject shipmaster = NavalSkillLookup.ShipmasterSkill;
                    return shipmaster != null ? hero.GetSkillValue(shipmaster) : 0;
                case CouncilPosition.LordCommanderOfKingsguard:
                    return GetVigorSkillValue(hero);
                default:
                    return 0;
            }
        }

        public static int GetVigorSkillValue(Hero hero)
        {
            return System.Math.Max(hero.GetSkillValue(DefaultSkills.OneHanded),
                System.Math.Max(hero.GetSkillValue(DefaultSkills.TwoHanded), hero.GetSkillValue(DefaultSkills.Polearm)));
        }
    }
}
