using System;
using SmallCouncils.Behaviors;
using SmallCouncils.Models;
using SmallCouncils.Services;
using SmallCouncils.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SmallCouncils.UI.Assignment
{
    /// <summary>One row in the main 7-position council list.</summary>
    public class CouncilPositionItemVM : ViewModel
    {
        private readonly Action<CouncilPositionItemVM> _onSelect;

        private string _positionName;
        private string _assigneeName;
        private bool _isVacant;
        private ImageIdentifierVM _visual;

        public Kingdom Kingdom { get; }
        public CouncilPosition Position { get; }

        public CouncilPositionItemVM(Kingdom kingdom, CouncilPosition position, string displayName, Action<CouncilPositionItemVM> onSelect)
        {
            Kingdom = kingdom;
            Position = position;
            _onSelect = onSelect;
            _positionName = displayName;

            RefreshFromData();
        }

        /// <summary>Re-reads the current assignee from CouncilData. Call after any assignment change.</summary>
        public void RefreshFromData()
        {
            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(Kingdom);
            Hero assignee = data?.GetAssignee(Position);

            IsVacant = assignee == null;
            AssigneeName = HeroDisplayNameHelper.GetDisplayName(assignee, "Vacant");

            if (assignee != null)
            {
                try
                {
                    Visual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(assignee.CharacterObject));
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
