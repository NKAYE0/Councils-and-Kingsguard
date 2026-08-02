using SmallCouncils.Models;
using SmallCouncils.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SmallCouncils.UI.View
{
    /// <summary>Read-only row for one council position — no click/select behavior.</summary>
    public class CouncilViewPositionItemVM : ViewModel
    {
        private string _positionName;
        private string _assigneeName;
        private string _benefitText;
        private bool _isVacant;
        private ImageIdentifierVM _visual;

        public CouncilViewPositionItemVM(CouncilPosition position, string displayName, Hero assignee)
        {
            _positionName = displayName;
            _isVacant = assignee == null;
            _assigneeName = HeroDisplayNameHelper.GetDisplayName(assignee, "Vacant");
            _benefitText = CouncilBenefitDisplay.GetPositionBenefitText(position, assignee);

            if (assignee != null)
            {
                try
                {
                    _visual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(assignee.CharacterObject));
                }
                catch
                {
                    _visual = null;
                }
            }
        }

        [DataSourceProperty]
        public string PositionName
        {
            get => _positionName;
            set
            {
                if (value != _positionName)
                {
                    _positionName = value;
                    OnPropertyChangedWithValue(value, nameof(PositionName));
                }
            }
        }

        [DataSourceProperty]
        public string AssigneeName
        {
            get => _assigneeName;
            set
            {
                if (value != _assigneeName)
                {
                    _assigneeName = value;
                    OnPropertyChangedWithValue(value, nameof(AssigneeName));
                }
            }
        }

        [DataSourceProperty]
        public string BenefitText
        {
            get => _benefitText;
            set
            {
                if (value != _benefitText)
                {
                    _benefitText = value;
                    OnPropertyChangedWithValue(value, nameof(BenefitText));
                }
            }
        }

        [DataSourceProperty]
        public bool IsVacant
        {
            get => _isVacant;
            set
            {
                if (value != _isVacant)
                {
                    _isVacant = value;
                    OnPropertyChangedWithValue(value, nameof(IsVacant));
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
    }
}
