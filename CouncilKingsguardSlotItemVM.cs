using System;
using SmallCouncils.Behaviors;
using SmallCouncils.Models;
using SmallCouncils.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SmallCouncils.UI.Assignment
{
    /// <summary>One row in the 6-slot Kingsguard roster list (player-only).</summary>
    public class CouncilKingsguardSlotItemVM : ViewModel
    {
        private readonly Action<CouncilKingsguardSlotItemVM> _onSelect;

        private string _slotLabel;
        private string _memberName;
        private bool _isVacant;
        private ImageIdentifierVM _visual;

        public Kingdom Kingdom { get; }
        public int SlotIndex { get; }

        public CouncilKingsguardSlotItemVM(Kingdom kingdom, int slotIndex, Action<CouncilKingsguardSlotItemVM> onSelect)
        {
            Kingdom = kingdom;
            SlotIndex = slotIndex;
            _onSelect = onSelect;
            _slotLabel = $"Kingsguard {slotIndex + 1}";

            RefreshFromData();
        }

        public void RefreshFromData()
        {
            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(Kingdom);
            Hero member = data != null && SlotIndex < data.KingsguardRoster.Count ? data.KingsguardRoster[SlotIndex] : null;

            IsVacant = member == null;
            MemberName = HeroDisplayNameHelper.GetDisplayName(member, "Vacant");

            if (member != null)
            {
                try
                {
                    Visual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(member.CharacterObject));
                }
                catch
                {
                    Visual = null;
                }
            }
            else
            {
                Visual = null;
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

        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }
    }
}
