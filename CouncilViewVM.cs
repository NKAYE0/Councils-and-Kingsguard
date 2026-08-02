using SmallCouncils.Behaviors;
using SmallCouncils.Models;
using SmallCouncils.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SmallCouncils.UI.View
{
    /// <summary>
    /// Root ViewModel for the read-only council viewer, opened from the
    /// Encyclopedia's Kingdom page. Works for ANY kingdom (unlike
    /// CouncilAssignmentVM, which is player-ruler-only and editable).
    ///
    /// Kingsguard roster is only shown when viewing the kingdom the player
    /// currently rules (per the spec: the roster is a player-only feature,
    /// stored per the kingdom the player rules — see CouncilData's remarks).
    /// AI kingdoms just show the 7 named positions.
    /// </summary>
    public class CouncilViewVM : ViewModel
    {
        private readonly Kingdom _kingdom;

        private string _titleText;
        private bool _showKingsguard;
        private BannerImageIdentifierVM _kingdomBanner;
        private CharacterImageIdentifierVM _kingPortrait;
        private string _kingName;
        private string _kingSubtitleText;
        private MBBindingList<CouncilViewPositionItemVM> _positionList;
        private MBBindingList<CouncilViewKingsguardItemVM> _kingsguardList;

        public event System.Action CloseRequested;

        public CouncilViewVM(Kingdom kingdom)
        {
            _kingdom = kingdom;
            _titleText = kingdom?.Name?.ToString() != null ? $"{kingdom.Name} — Small Council" : "Small Council";
            _kingdomBanner = kingdom?.Banner != null ? new BannerImageIdentifierVM(kingdom.Banner, true) : null;

            Hero ruler = kingdom?.Leader;
            _kingPortrait = ruler?.CharacterObject != null ? new CharacterImageIdentifierVM(CharacterCode.CreateFrom(ruler.CharacterObject)) : null;
            _kingName = HeroDisplayNameHelper.GetDisplayName(ruler, "Vacant Throne");
            _kingSubtitleText = SmallCouncils.Settings.RulerTitleLookup.GetRulerTitle(kingdom, ruler);

            _positionList = new MBBindingList<CouncilViewPositionItemVM>();
            _kingsguardList = new MBBindingList<CouncilViewKingsguardItemVM>();

            BuildPositionList();
            BuildKingsguardListIfApplicable();
        }

        private void BuildPositionList()
        {
            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(_kingdom);

            PositionList.Clear();
            string handOfTheKingLabel = (_kingdom?.Leader != null && _kingdom.Leader.IsFemale) ? "Hand of the Queen" : "Hand of the King";
            AddPositionRow(data, CouncilPosition.HandOfTheKing, handOfTheKingLabel);
            AddPositionRow(data, CouncilPosition.GrandMaester, "Grand Maester");
            AddPositionRow(data, CouncilPosition.MasterOfCoin, "Master of Coin");
            AddPositionRow(data, CouncilPosition.MasterOfLaws, "Master of Laws");
            AddPositionRow(data, CouncilPosition.MasterOfShips, "Master of Ships");
            AddPositionRow(data, CouncilPosition.MasterOfWhisperers, "Master of Whisperers");
            AddPositionRow(data, CouncilPosition.LordCommanderOfKingsguard, "Lord Commander of the Kingsguard");
        }

        private void AddPositionRow(CouncilData data, CouncilPosition position, string displayName)
        {
            Hero assignee = data?.GetAssignee(position);
            PositionList.Add(new CouncilViewPositionItemVM(position, displayName, assignee));
        }

        private void BuildKingsguardListIfApplicable()
        {
            bool isPlayerKingdom = _kingdom != null && _kingdom.Leader == Hero.MainHero;
            ShowKingsguard = isPlayerKingdom;

            if (!isPlayerKingdom)
            {
                return;
            }

            CouncilData data = CouncilBehavior.Instance?.GetCouncilData(_kingdom);
            Hero lordCommander = data?.GetAssignee(CouncilPosition.LordCommanderOfKingsguard);

            KingsguardList.Clear();
            if (data == null)
            {
                return;
            }

            for (int i = 0; i < data.KingsguardRoster.Count; i++)
            {
                KingsguardList.Add(new CouncilViewKingsguardItemVM(i, data.KingsguardRoster[i], lordCommander));
            }
        }

        public void ExecuteClose()
        {
            CloseRequested?.Invoke();
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value != _titleText)
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value, nameof(TitleText));
                }
            }
        }

        [DataSourceProperty]
        public bool ShowKingsguard
        {
            get => _showKingsguard;
            set
            {
                if (value != _showKingsguard)
                {
                    _showKingsguard = value;
                    OnPropertyChangedWithValue(value, nameof(ShowKingsguard));
                }
            }
        }

        [DataSourceProperty]
        public BannerImageIdentifierVM KingdomBanner
        {
            get => _kingdomBanner;
            set
            {
                if (value != _kingdomBanner)
                {
                    _kingdomBanner = value;
                    OnPropertyChangedWithValue(value, nameof(KingdomBanner));
                }
            }
        }

        [DataSourceProperty]
        public CharacterImageIdentifierVM KingPortrait
        {
            get => _kingPortrait;
            set
            {
                if (value != _kingPortrait)
                {
                    _kingPortrait = value;
                    OnPropertyChangedWithValue(value, nameof(KingPortrait));
                }
            }
        }

        [DataSourceProperty]
        public string KingName
        {
            get => _kingName;
            set
            {
                if (value != _kingName)
                {
                    _kingName = value;
                    OnPropertyChangedWithValue(value, nameof(KingName));
                }
            }
        }

        [DataSourceProperty]
        public string KingSubtitleText
        {
            get => _kingSubtitleText;
            set
            {
                if (value != _kingSubtitleText)
                {
                    _kingSubtitleText = value;
                    OnPropertyChangedWithValue(value, nameof(KingSubtitleText));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CouncilViewPositionItemVM> PositionList
        {
            get => _positionList;
            set
            {
                if (value != _positionList)
                {
                    _positionList = value;
                    OnPropertyChangedWithValue(value, nameof(PositionList));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CouncilViewKingsguardItemVM> KingsguardList
        {
            get => _kingsguardList;
            set
            {
                if (value != _kingsguardList)
                {
                    _kingsguardList = value;
                    OnPropertyChangedWithValue(value, nameof(KingsguardList));
                }
            }
        }
    }
}
