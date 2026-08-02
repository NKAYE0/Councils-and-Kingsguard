using System.Collections.Generic;
using System.Linq;
using SmallCouncils.Models;
using TaleWorlds.CampaignSystem;

namespace SmallCouncils.Services
{
    /// <summary>
    /// Lore-specific council assignments that should hold for certain
    /// kingdoms/rulers, checked every daily tick alongside the normal AI
    /// logic in CouncilAIBehavior. Each rule is scoped to a kingdom name
    /// (case-sensitive, matching RulerTitleLookup's convention) and
    /// optionally a specific ruler — if the ruler condition doesn't match,
    /// or the target hero is dead/not found, the rule simply doesn't apply
    /// and normal AI candidate-selection logic handles that position
    /// instead (per your "fall back to original logic" requirement — no
    /// special handling needed for this, since ApplyScriptedAssignments
    /// only ever assigns a position when its specific rule's conditions are
    /// actually met, and CouncilAIBehavior's FillVacantPositions runs
    /// afterward and fills whatever's still vacant).
    ///
    /// Only checked for AI-ruled kingdoms (see CouncilAIBehavior's call
    /// site) — if the player ends up ruling one of these kingdoms, these
    /// rules stop applying entirely, since a scripted assignment silently
    /// overriding the player's own deliberate council choices would be a
    /// bad experience. If a kingdom in this list isn't installed (ROT not
    /// present, or the kingdom name differs), the kingdom-name lookup
    /// simply never matches and this class does nothing — same
    /// no-ROT-required compatibility approach as RulerTitleLookup.
    ///
    /// Target heroes are looked up by exact Name match against
    /// Hero.AllAliveHeroes each time (cheap — only 4 distinct target names,
    /// checked once per day, not per frame), so a hero's death is picked
    /// up automatically on the very next daily tick without any extra
    /// bookkeeping.
    /// </summary>
    public static class ScriptedAssignmentService
    {
        private class Rule
        {
            public string KingdomName;
            public string RulerNameStartsWith; // null = applies regardless of who currently rules
            public CouncilPosition Position;
            public string TargetHeroName;
        }

        private static readonly List<Rule> Rules = new List<Rule>
        {
            new Rule { KingdomName = "Dragonstone", RulerNameStartsWith = "Stannis", Position = CouncilPosition.HandOfTheKing, TargetHeroName = "Davos Seaworth" },
            new Rule { KingdomName = "Dragonstone", RulerNameStartsWith = "Stannis", Position = CouncilPosition.GrandMaester, TargetHeroName = "Melisandre" },
            new Rule { KingdomName = "House Baratheon of King's Landing", RulerNameStartsWith = null, Position = CouncilPosition.HandOfTheKing, TargetHeroName = "Tywin Lannister" },
            new Rule { KingdomName = "Dorne", RulerNameStartsWith = "Doran Martell", Position = CouncilPosition.LordCommanderOfKingsguard, TargetHeroName = "Areo Hotah" },
        };

        public static void ApplyScriptedAssignments(Kingdom kingdom)
        {
            if (kingdom?.Name?.ToString() == null)
            {
                return;
            }

            string kingdomName = kingdom.Name.ToString();
            string rulerName = kingdom.Leader?.Name?.ToString();

            foreach (Rule rule in Rules)
            {
                if (rule.KingdomName != kingdomName)
                {
                    continue;
                }

                if (rule.RulerNameStartsWith != null && (rulerName == null || !rulerName.StartsWith(rule.RulerNameStartsWith)))
                {
                    continue;
                }

                Hero target = Hero.AllAliveHeroes.FirstOrDefault(h => h.Name?.ToString() == rule.TargetHeroName);
                if (target == null || !target.IsAlive)
                {
                    continue; // dead/not found — normal AI logic fills this position instead
                }

                if (target == kingdom.Leader)
                {
                    continue; // rulers can never hold a council position — safety net, shouldn't occur in practice
                }

                CouncilAssignmentService.AssignPositionInternal(kingdom, rule.Position, target);
            }
        }
    }
}
