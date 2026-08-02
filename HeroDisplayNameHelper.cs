using TaleWorlds.CampaignSystem;

namespace SmallCouncils.UI
{
    /// <summary>
    /// The player character's Name (set once at character creation) has no
    /// family/clan name attached to it, unlike AI-generated lords (whose
    /// Name already includes their surname, e.g. "Axell Florent"). This
    /// appends the clan name specifically for the player so "Duran"
    /// displays as "Duran Baratheon" — doing this unconditionally for every
    /// hero would double up AI lords' names, which already have a surname.
    /// </summary>
    public static class HeroDisplayNameHelper
    {
        public static string GetDisplayName(Hero hero, string fallbackIfNull = "Vacant")
        {
            if (hero == null)
            {
                return fallbackIfNull;
            }

            if (hero == Hero.MainHero && hero.Clan != null)
            {
                return $"{hero.Name} {hero.Clan.Name}";
            }

            return hero.Name?.ToString() ?? fallbackIfNull;
        }
    }
}
