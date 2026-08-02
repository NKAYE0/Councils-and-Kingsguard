using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace SmallCouncils.Models
{
    /// <summary>
    /// Registers this mod's saveable types with the campaign save system.
    ///
    /// VERIFY BEFORE FIRST COMPILE:
    /// - The base ID passed to the constructor (559_600_100) must not collide
    ///   with another installed mod's SaveableTypeDefiner base ID. There is no
    ///   central registry for this; the safe convention is picking a large,
    ///   distinctive number and documenting it. Worth double-checking against
    ///   ROT 8.0 and other commonly-run mods' known ranges if that information
    ///   is available, since a collision causes save corruption, not just a
    ///   compile error.
    /// - Confirm the exact base class name/namespace for SaveableTypeDefiner
    ///   and the AddClassDefinition / AddEnumDefinition / ConstructContainerDefinition
    ///   method signatures against your referenced TaleWorlds.SaveSystem.dll,
    ///   as I do not have access to decompile it in this environment.
    /// </summary>
    public class CouncilSaveDefiner : SaveableTypeDefiner
    {
        private const int BaseId = 559_600_100;

        public CouncilSaveDefiner() : base(BaseId)
        {
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(CouncilData), 1);
        }

        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(CouncilPosition), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            // Dictionary keyed on the kingdom itself, one CouncilData per kingdom.
            ConstructContainerDefinition(typeof(Dictionary<Kingdom, CouncilData>));

            // Used internally by CouncilData for position assignments and roster.
            ConstructContainerDefinition(typeof(Dictionary<CouncilPosition, Hero>));
            ConstructContainerDefinition(typeof(List<Hero>));
        }
    }
}
