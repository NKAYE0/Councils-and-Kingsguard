using SmallCouncils.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SmallCouncils.UI.View
{
    /// <summary>Read-only row for one Kingsguard roster slot — no click/select behavior.</summary>
    public class CouncilViewKingsguardItemVM : ViewModel
    {
        private string _slotLabel;
        private string _memberName;
        private string _benefitText;
        private bool _isVacant;
        private ImageIdentifierVM _visual;

        public CouncilViewKingsguardItemVM(int slotIndex, Hero member, Hero lordCommander)
        {
            _slotLabel = $"Kingsguard {slotIndex + 1}";
            _isVacant = member == null;
            _memberName = HeroDisplayNameHelper.GetDisplayName(member, "Vacant");
            _benefitText = member != null ? CouncilBenefitDisplay.GetKingsguardBenefitText(lordCommander) : string.Empty;

            if (member != null)
            {
                try
                {
                    _visual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(member.CharacterObject));
                }
                catch
                {
                    _visual = null;
                }
            }
        }

        [DataSourceProperty]
        public string SlotLabel
        {
            get => _slotLabel;
            set
            {
                if (value != _slotLabel)
                {
                    _slotLabel = value;
                    OnPropertyChangedWithValue(value, nameof(SlotLabel));
                }
            }
        }

        [DataSourceProperty]
        public string MemberName
        {
            get => _memberName;
            set
            {
                if (value != _memberName)
                {
                    _memberName = value;
                    OnPropertyChangedWithValue(value, nameof(MemberName));
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
