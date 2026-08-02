using System.Collections.Generic;
using SmallCouncils.Behaviors;
using SmallCouncils.Models;
using SmallCouncils.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SmallCouncils.Services
{
    /// <summary>
    /// Computes and caches skill XP multipliers for the Lord Commander and
    /// the ordinary Kingsguard roster members they lead. Replaces the
    /// earlier flat vigor-skill-bonus system entirely, per your request —
    /// Kingsguard membership (and the Lord Commander role specifically) now
    /// speeds up skill leveling instead of granting a flat skill bump.
    ///
    /// Performance note: HeroDeveloper.AddSkillXp (the patched method) is
    /// called extremely often — every skill-granting action in the game —
    /// so the Harmony patch that reads this cache must NOT recompute
    /// anything on every call. This class recomputes only on a cheap
    /// cadence (daily tick) and the patch does a plain dictionary lookup.
    ///
    /// GetVigorSkillValue is unrelated to the XP multiplier system — it's a
    /// shared utility (max of OneHanded/TwoHanded/Polearm) still used by
    /// the Lord Commander's separate morale-bonus benefit and the
    /// assignment screen's candidate sorting, so it's kept here unchanged.
    /// </summary>
    public static class KingsguardBonusService
    {
        private static readonly Dictionary<Hero, float> XpMultiplierPerHero = new Dictionary<Hero, float>();

        public static void RecomputeAll()
        {
            XpMultiplierPerHero.Clear();

            float kingsguardMultiplier = CouncilSettings.Instance?.KingsguardXpMultiplier ?? 2f;
            float lordCommanderMultiplier = CouncilSettings.Instance?.LordCommanderXpMultiplier ?? 3f;

            foreach (Kingdom kingdom in Kingdom.All)
            {
                CouncilData data = CouncilBehavior.Instance?.GetCouncilData(kingdom);
                if (data == null)
                {
                    continue;
                }

                Hero lordCommander = data.GetAssignee(CouncilPosition.LordCommanderOfKingsguard);
                if (lordCommander != null && lordCommander.IsAlive)
                {
                    XpMultiplierPerHero[lordCommander] = lordCommanderMultiplier;
                }

                foreach (Hero member in data.KingsguardRoster)
                {
                    if (member != null && member.IsAlive)
                    {
                        // A hero can't simultaneously be Lord Commander and an
                        // ordinary roster member (enforced elsewhere), so this
                        // never overwrites the Lord Commander's own entry —
                        // this check is just a safety net.
                        if (!XpMultiplierPerHero.ContainsKey(member))
                        {
                            XpMultiplierPerHero[member] = kingsguardMultiplier;
                        }
                    }
                }
            }
        }

        /// <summary>Called by the skill XP Harmony patch — cheap lookup only, no recomputation.</summary>
        public static float GetXpMultiplier(Hero hero)
        {
            if (hero == null)
            {
                return 1f;
            }

            return XpMultiplierPerHero.TryGetValue(hero, out float multiplier) ? multiplier : 1f;
        }

        public static float GetVigorSkillValue(Hero hero)
        {
            int oneHanded = hero.GetSkillValue(DefaultSkills.OneHanded);
            int twoHanded = hero.GetSkillValue(DefaultSkills.TwoHanded);
            int polearm = hero.GetSkillValue(DefaultSkills.Polearm);
            return System.Math.Max(oneHanded, System.Math.Max(twoHanded, polearm));
        }
    }
}
