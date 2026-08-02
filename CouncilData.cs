using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace SmallCouncils.Models
{
    /// <summary>
    /// Holds the small council position assignments for a single kingdom, plus
    /// the 6-slot Kingsguard roster.
    ///
    /// BUGFIX: the three fields below were missing [SaveableField] attributes,
    /// which meant the save system never actually persisted them — after a
    /// save/load cycle they came back null, causing a NullReferenceException
    /// on the very next daily tick. Every accessor below now also defensively
    /// lazy-initializes if null, so saves made before this fix (which may
    /// still have null fields baked in) recover gracefully instead of
    /// crashing again on load.
    ///
    /// NOTE on Kingsguard roster scope: the design spec ties the roster to the
    /// player's clan (any clan member, regardless of which kingdom the player
    /// currently rules), not strictly to "whichever kingdom this CouncilData
    /// belongs to". Since a player could theoretically abdicate/rejoin a
    /// different kingdom, storing the roster per-kingdom-instance is the
    /// simplest option for Stage 1 and matches "the player's council" being
    /// tied to the kingdom they currently rule. If this causes friction later
    /// (e.g. roster should persist across the player losing/regaining a
    /// kingdom), we can move it to a separate player-clan-scoped save field
    /// in a later stage without breaking this class's public shape.
    /// </summary>
    public class CouncilData
    {
        public const int KingsguardRosterSize = 6;

        [SaveableField(1)]
        private Dictionary<CouncilPosition, Hero> _assignments;

        [SaveableField(2)]
        private List<Hero> _kingsguardRoster;

        [SaveableField(3)]
        private CampaignTime _lastHandReevaluationTime;

        public CouncilData()
        {
            _assignments = new Dictionary<CouncilPosition, Hero>();
            _kingsguardRoster = new List<Hero>(new Hero[KingsguardRosterSize]);
        }

        /// <summary>Guards against null fields from saves made before the SaveableField fix.</summary>
        private void EnsureInitialized()
        {
            if (_assignments == null)
            {
                _assignments = new Dictionary<CouncilPosition, Hero>();
            }

            if (_kingsguardRoster == null)
            {
                _kingsguardRoster = new List<Hero>(new Hero[KingsguardRosterSize]);
            }
        }

        /// <summary>All current position assignments. Missing keys = vacant.</summary>
        public IReadOnlyDictionary<CouncilPosition, Hero> Assignments
        {
            get
            {
                EnsureInitialized();
                return _assignments;
            }
        }

        /// <summary>The 6 Kingsguard roster slots. Null entries = empty slot.</summary>
        public IReadOnlyList<Hero> KingsguardRoster
        {
            get
            {
                EnsureInitialized();
                return _kingsguardRoster;
            }
        }

        /// <summary>Campaign time this kingdom's Hand of the King was last re-evaluated.</summary>
        public CampaignTime LastHandReevaluationTime
        {
            get => _lastHandReevaluationTime;
            set => _lastHandReevaluationTime = value;
        }

        public Hero GetAssignee(CouncilPosition position)
        {
            EnsureInitialized();
            return _assignments.TryGetValue(position, out Hero hero) ? hero : null;
        }

        /// <summary>Returns the position this hero currently holds in this kingdom's council, or null if none.</summary>
        public CouncilPosition? FindPositionOfHero(Hero hero)
        {
            if (hero == null)
            {
                return null;
            }

            EnsureInitialized();

            foreach (KeyValuePair<CouncilPosition, Hero> kvp in _assignments)
            {
                if (kvp.Value == hero)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        public bool IsVacant(CouncilPosition position)
        {
            return GetAssignee(position) == null;
        }

        /// <summary>
        /// Sets or clears (pass null) the assignee for a position. This is a pure
        /// data operation — it does NOT apply relation changes or validate
        /// eligibility. Those belong in the assignment API added in Stage 2, so
        /// that AI logic, player UI, and save-load all share one validated path
        /// rather than mutating this dictionary directly.
        /// </summary>
        public void SetAssigneeRaw(CouncilPosition position, Hero hero)
        {
            EnsureInitialized();

            if (hero == null)
            {
                _assignments.Remove(position);
            }
            else
            {
                _assignments[position] = hero;
            }
        }

        public bool TrySetKingsguardMemberRaw(int slotIndex, Hero hero)
        {
            EnsureInitialized();

            if (slotIndex < 0 || slotIndex >= _kingsguardRoster.Count)
            {
                return false;
            }

            _kingsguardRoster[slotIndex] = hero;
            return true;
        }

        public int IndexOfKingsguardMember(Hero hero)
        {
            EnsureInitialized();
            return _kingsguardRoster.IndexOf(hero);
        }

        /// <summary>
        /// Removes a hero from every position and the Kingsguard roster. Used
        /// for cleanup when a hero dies or leaves the clan — called from
        /// CouncilBehavior's event listeners in a later stage.
        /// </summary>
        public void RemoveHeroEverywhere(Hero hero)
        {
            if (hero == null) return;

            EnsureInitialized();

            List<CouncilPosition> positionsToClear = null;
            foreach (KeyValuePair<CouncilPosition, Hero> kvp in _assignments)
            {
                if (kvp.Value == hero)
                {
                    if (positionsToClear == null)
                    {
                        positionsToClear = new List<CouncilPosition>();
                    }
                    positionsToClear.Add(kvp.Key);
                }
            }

            if (positionsToClear != null)
            {
                foreach (CouncilPosition position in positionsToClear)
                {
                    _assignments.Remove(position);
                }
            }

            for (int i = 0; i < _kingsguardRoster.Count; i++)
            {
                if (_kingsguardRoster[i] == hero)
                {
                    _kingsguardRoster[i] = null;
                }
            }
        }
    }
}
