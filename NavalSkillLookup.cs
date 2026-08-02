using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace SmallCouncils.Services
{
    /// <summary>
    /// Lazily resolves NavalDLC's "Shipmaster" skill by its confirmed string ID
    /// (id="Shipmaster" in NavalDLC's naval_skill_sets.xml) via
    /// MBObjectManager, since — unlike the native skills exposed through
    /// DefaultSkills.* — it isn't a compile-time field anywhere; it's purely
    /// XML-defined data with no C# convenience wrapper.
    ///
    /// SOURCED (not guessed): both the exact ID string and the
    /// MBObjectManager.Instance.GetObject&lt;T&gt;(string) lookup signature were
    /// confirmed via reflection/file search against your installation, not
    /// assumed.
    ///
    /// Returns null gracefully if the lookup fails (e.g. NavalDLC isn't
    /// loaded) — callers already check for null before using this, matching
    /// the project's "fail gracefully if optional dependencies are
    /// unavailable" guidance.
    /// </summary>
    public static class NavalSkillLookup
    {
        private static SkillObject _shipmasterSkill;
        private static bool _attemptedLookup;

        public static SkillObject ShipmasterSkill
        {
            get
            {
                if (!_attemptedLookup)
                {
                    _attemptedLookup = true;
                    _shipmasterSkill = MBObjectManager.Instance?.GetObject<SkillObject>("Shipmaster");
                }

                return _shipmasterSkill;
            }
        }
    }
}
