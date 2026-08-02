using System;
using SmallCouncils.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SmallCouncils.UI.Assignment
{
    /// <summary>
    /// One selectable hero in the candidate-picker list shown when the
    /// player clicks a vacant/occupied position or Kingsguard slot.
    ///
    /// VERIFY BEFORE COMPILE: ImageIdentifierVM construction from a Hero is
    /// one of the fiddlier parts of Bannerlord UI modding — there are a few
    /// different constructor overloads across versions. I've used the
    /// CharacterCode-based one as my best guess; if this doesn't compile or
    /// renders a blank portrait, that's the first thing to check.
    /// </summary>
    public class CouncilCandidateItemVM : ViewModel
    {
        private readonly Action<CouncilCandidateItemVM> _onSelect;

        private string _name;
        private string _relevantSkillText;
        private ImageIdentifierVM _visual;

        public Hero Hero { get; }

        public CouncilCandidateItemVM(Hero hero, string relevantSkillText, Action<CouncilCandidateItemVM> onSelect)
        {
            Hero = hero;
            _onSelect = onSelect;

            _name = HeroDisplayNameHelper.GetDisplayName(hero, "Unknown");
            _relevantSkillText = relevantSkillText;

            try
            {
                _visual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(hero.CharacterObject));
            }
            catch
            {
                // Defensive: if portrait construction fails for any reason, fall back
                // to a null visual rather than crashing the whole screen. The prefab
                // should handle a null/placeholder image gracefully.
                _visual = null;
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, nameof(Name));
                }
            }
        }

        [DataSourceProperty]
        public string RelevantSkillText
        {
            get => _relevantSkillText;
            set
            {
                if (value != _relevantSkillText)
                {
                    _relevantSkillText = value;
                    OnPropertyChangedWithValue(value, nameof(RelevantSkillText));
                }
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM Visual
        {
            get => _visual;
            set
            {
                if (value != _visual)
                {
                    _visual = value;
                    OnPropertyChangedWithValue(value, nameof(Visual));
                }
            }
        }

        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }
    }
}
