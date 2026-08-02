using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace SmallCouncils.Settings
{
    /// <summary>
    /// Maps a kingdom's exact (case-sensitive) name to the title its ruler
    /// should display in the council header, e.g. "Lord of the Vale"
    /// instead of "King". Kingdom names not in this table (including every
    /// kingdom in a non-ROT setup) fall back to plain King/Queen — this is
    /// the compatibility mechanism that lets the mod work fine without ROT
    /// installed at all, per your request.
    ///
    /// The player's own kingdom is a special case, always King/Queen
    /// regardless of which kingdom that happens to be — handled in
    /// GetRulerTitle below, not in this table.
    ///
    /// Where you gave a single title with no gender split (e.g. "Khal",
    /// "Lord Reaper", "Magister"), both entries below are identical. Where
    /// a title clearly followed the same King/Queen-style pattern as its
    /// neighbors but you only wrote one form (Free Folk's "King beyond the
    /// wall"), I've added the parallel female form for consistency — flagging
    /// this rather than silently assuming, since it's the one place I
    /// extrapolated beyond exactly what you listed.
    /// </summary>
    public static class RulerTitleLookup
    {
        private static readonly Dictionary<string, (string Male, string Female)> Titles = new Dictionary<string, (string, string)>
        {
            { "Dragonstone", ("King", "Queen") },
            { "Dorne", ("Prince of Dorne", "Princess of Dorne") },
            { "House Baratheon of King's Landing", ("King", "Queen") },
            { "The North", ("King in the North", "Queen in the North") },
            { "The Vale", ("Lord of the Vale", "Lady of the Vale") },
            { "Iron Islands", ("Lord Reaper", "Lord Reaper") },
            { "Riverlands", ("Lord of the Riverlands", "Lady of the Riverlands") },
            { "House Targaryen", ("Dragon King", "Dragon Queen") },
            { "Myr", ("Prince-Admiral of Myr", "Prince-Admiral of Myr") },
            { "Dothraki Horde", ("Khal", "Khal") },
            { "Braavos", ("Sealord of Braavos", "Sealord of Braavos") },
            { "Volantis", ("Triarch", "Triarch") },
            { "Tyrosh", ("Archon", "Archon") },
            { "Norvos", ("Magister", "Magister") },
            { "Qohor", ("The Black Goat", "The Black Goat") },
            { "Free Folk", ("King beyond the wall", "Queen beyond the wall") },
            { "Nights Watch", ("Lord Commander", "Lord Commander") },
            { "Lorath", ("Prince of Lorath", "Princess of Lorath") },
            { "Summer Isles", ("Prince", "Princess") },
            { "Yi Ti Exiles", ("Emperor", "Empress") },
            { "Skagos", ("Lord Magnar", "Lord Magnar") },
            { "House Targaryen,Aegon", ("King", "Queen") },
            { "Lys", ("First Magister", "First Magister") },
            { "Pentos", ("Magister", "Magister") },
            { "Sarnor", ("High King", "High Queen") },
            { "Stormlands", ("King", "Queen") },
            { "The Reach", ("Lord of the Reach", "Lady of the Reach") },
        };

        /// <summary>
        /// The player always displays as King/Queen regardless of which
        /// kingdom they rule; every other ruler gets their kingdom's ASOIAF
        /// title if one is defined, or plain King/Queen otherwise (the
        /// no-ROT-installed fallback).
        /// </summary>
        public static string GetRulerTitle(Kingdom kingdom, Hero ruler)
        {
            if (ruler == null)
            {
                return string.Empty;
            }

            if (ruler == Hero.MainHero)
            {
                return ruler.IsFemale ? "Queen" : "King";
            }

            if (OwnsTheCapital(kingdom))
            {
                return ruler.IsFemale ? "Queen" : "King";
            }

            if (kingdom?.Name?.ToString() is string kingdomName && Titles.TryGetValue(kingdomName, out (string Male, string Female) title))
            {
                return ruler.IsFemale ? title.Female : title.Male;
            }

            return ruler.IsFemale ? "Queen" : "King";
        }

        /// <summary>
        /// Controlling both King's Landing and the Red Keep marks a kingdom
        /// as holding the Iron Throne, regardless of what its own name/title
        /// would otherwise be — matching the lore reasoning that whoever
        /// holds the capital is the true King/Queen. VERIFY BEFORE COMPILE:
        /// Kingdom.Fiefs (IEnumerable&lt;Town&gt;) is a well-established,
        /// commonly-used Bannerlord API, but hasn't been reflection-verified
        /// against your assembly specifically.
        /// </summary>
        private static bool OwnsTheCapital(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return false;
            }

            bool ownsKingsLanding = false;
            bool ownsRedKeep = false;

            foreach (TaleWorlds.CampaignSystem.Settlements.Town fief in kingdom.Fiefs)
            {
                string name = fief?.Name?.ToString();
                if (name == "King's Landing")
                {
                    ownsKingsLanding = true;
                }
                else if (name == "Red Keep")
                {
                    ownsRedKeep = true;
                }
            }

            return ownsKingsLanding && ownsRedKeep;
        }
    }
}
