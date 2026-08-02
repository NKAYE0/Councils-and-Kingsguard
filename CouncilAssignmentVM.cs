using SmallCouncils.Models;
using SmallCouncils.Services;
using SmallCouncils.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SmallCouncils.UI.Assignment
{
    /// <summary>
    /// Root ViewModel for the interactive council assignment screen. This is
    /// the player-only editable view — always constructed for the kingdom
    /// the player currently rules (the calling code, Stage 5's screen/button
    /// wiring, is responsible for only ever opening this when the player is
    /// actually the ruler). The read-only "view any kingdom's council" flow
    /// for the encyclopedia (Stage 6) is a separate, simpler VM.
    ///
    /// Interaction model: clicking a position row or Kingsguard slot opens a
    /// candidate-picker panel (IsPickerVisible + CandidateList); picking a
    /// candidate assigns them and closes the picker; ExecuteUnassignSelected
    /// clears whichever row/slot is currently open in the picker.
    /// </summary>
    public class CouncilAssignmentVM : ViewModel
    {
        private readonly Kingdom _kingdom;

        private CouncilPositionItemVM _selectedPosition;
        private CouncilKingsguardSlotItemVM _selectedSlot;

        private string _titleText;
        private string _pickerTitleText;
        private bool _isPickerVisible;
        private BannerImageIdentifierVM _kingdomBanner;
        private CharacterImageIdentifierVM _kingPortrait;
        private string _kingName;
        private string _kingSubtitleText;
        private MBBindingList<CouncilPositionItemVM> _positionList;
        private MBBindingList<CouncilKingsguardSlotItemVM> _kingsguardSlotList;
        private MBBindingList<CouncilCandidateItemVM> _candidateList;

        /// <summary>Raised when the player clicks the Close button. The hosting
        /// Screen subscribes to this and pops itself — the VM deliberately has
        /// no knowledge of screens/ScreenManager.</summary>
        public event System.Action CloseRequested;

        public CouncilAssignmentVM(Kingdom kingdom)
        {
            _kingdom = kingdom;
            _titleText = kingdom?.Name?.ToString() != null ? $"{kingdom.Name} — Small Council" : "Small Council";
            _pickerTitleText = string.Empty;

            _kingdomBanner = kingdom?.Banner != null ? new BannerImageIdentifierVM(kingdom.Banner, true) : null;

            Hero ruler = kingdom?.Leader;
            _kingPortrait = ruler?.CharacterObject != null ? new CharacterImageIdentifierVM(CharacterCode.CreateFrom(ruler.CharacterObject)) : null;
            _kingName = HeroDisplayNameHelper.GetDisplayName(ruler, "Vacant Throne");
            _kingSubtitleText = SmallCouncils.Settings.RulerTitleLookup.GetRulerTitle(kingdom, ruler);

            _positionList = new MBBindingList<CouncilPositionItemVM>();
            _kingsguardSlotList = new MBBindingList<CouncilKingsguardSlotItemVM>();
            _candidateList = new MBBindingList<CouncilCandidateItemVM>();

            BuildPositionList();
            BuildKingsguardList();
        }

        private void BuildPositionList()
        {
            PositionList.Clear();
            string handOfTheKingLabel = (_kingdom?.Leader != null && _kingdom.Leader.IsFemale) ? "Hand of the Queen" : "Hand of the King";
            PositionList.Add(new CouncilPositionItemVM(_kingdom, CouncilPosition.HandOfTheKing, handOfTheKingLabel, OnPositionSelected));
            PositionList.Add(new CouncilPositionItemVM(_kingdom, CouncilPosition.GrandMaester, "Grand Maester", OnPositionSelected));
            PositionList.Add(new CouncilPositionItemVM(_kingdom, CouncilPosition.MasterOfCoin, "Master of Coin", OnPositionSelected));
            PositionList.Add(new CouncilPositionItemVM(_kingdom, CouncilPosition.MasterOfLaws, "Master of Laws", OnPositionSelected));
            PositionList.Add(new CouncilPositionItemVM(_kingdom, CouncilPosition.MasterOfShips, "Master of Ships", OnPositionSelected));
            PositionList.Add(new CouncilPositionItemVM(_kingdom, CouncilPosition.MasterOfWhisperers, "Master of Whisperers", OnPositionSelected));
            PositionList.Add(new CouncilPositionItemVM(_kingdom, CouncilPosition.LordCommanderOfKingsguard, "Lord Commander of the Kingsguard", OnPositionSelected));
        }

        private void BuildKingsguardList()
        {
            KingsguardSlotList.Clear();
            for (int i = 0; i < CouncilData.KingsguardRosterSize; i++)
            {
                KingsguardSlotList.Add(new CouncilKingsguardSlotItemVM(_kingdom, i, OnKingsguardSlotSelected));
            }
        }

        // ============================================================
        // Selection / picker flow
        // ============================================================

        private void OnPositionSelected(CouncilPositionItemVM item)
        {
            _selectedPosition = item;
            _selectedSlot = null;
            OpenPickerForPosition(item.Position);
        }

        private void OnKingsguardSlotSelected(CouncilKingsguardSlotItemVM item)
        {
            _selectedSlot = item;
            _selectedPosition = null;
            OpenPickerForKingsguard(item.SlotIndex);
        }

        private void OpenPickerForPosition(CouncilPosition position)
        {
            CandidateList.Clear();

            System.Collections.Generic.List<Hero> candidates = CouncilAssignmentService.GetEligibleCandidatesForPosition(_kingdom, position);
            candidates.Sort((a, b) => GetRelevantSkillValue(position, b).CompareTo(GetRelevantSkillValue(position, a)));

            foreach (Hero hero in candidates)
            {
                CandidateList.Add(new CouncilCandidateItemVM(hero, GetRelevantSkillText(position, hero), OnCandidateSelected));
            }

            PickerTitleText = "Select a candidate";
            IsPickerVisible = true;
        }

        private void OpenPickerForKingsguard(int slotIndex)
        {
            CandidateList.Clear();

            System.Collections.Generic.List<Hero> candidates = CouncilAssignmentService.GetEligibleCandidatesForKingsguard(_kingdom);
            candidates.Sort((a, b) => GetVigorSkillValue(b).CompareTo(GetVigorSkillValue(a)));

            foreach (Hero hero in candidates)
            {
                CandidateList.Add(new CouncilCandidateItemVM(hero, $"Vigor: {GetVigorSkillValue(hero)}", OnCandidateSelected));
            }

            PickerTitleText = "Select a Kingsguard member";
            IsPickerVisible = true;
        }

        private void OnCandidateSelected(CouncilCandidateItemVM candidate)
        {
            if (_selectedPosition != null)
            {
                CouncilAssignmentService.TryAssignPositionAsPlayer(_kingdom, _selectedPosition.Position, candidate.Hero, out string _);
            }
            else if (_selectedSlot != null)
            {
                CouncilAssignmentService.TryAssignKingsguardMemberAsPlayer(_kingdom, _selectedSlot.SlotIndex, candidate.Hero, out string _);
            }

            // NOTE: failure reasons (e.g. ineligible hero, not ruler) are currently
            // discarded here rather than shown to the player. Candidates offered by
            // GetEligibleCandidatesFor* should already be valid, so a failure here
            // would indicate a state change between opening the picker and clicking
            // (e.g. the hero died mid-selection) — worth a proper on-screen message
            // in a later polish pass, but not a blocking issue for now.
            RefreshAllRows();
            ClosePicker();
        }

        public void ExecuteUnassignSelected()
        {
            if (_selectedPosition != null)
            {
                CouncilAssignmentService.TryUnassignPositionAsPlayer(_kingdom, _selectedPosition.Position, out string _);
            }
            else if (_selectedSlot != null)
            {
                CouncilAssignmentService.TryUnassignKingsguardMemberAsPlayer(_kingdom, _selectedSlot.SlotIndex, out string _);
            }

            RefreshAllRows();
            ClosePicker();
        }

        public void ExecuteClosePicker()
        {
            ClosePicker();
        }

        public void ExecuteClose()
        {
            CloseRequested?.Invoke();
        }

        private void ClosePicker()
        {
            IsPickerVisible = false;
            CandidateList.Clear();
            _selectedPosition = null;
            _selectedSlot = null;
        }

        private void RefreshAllRows()
        {
            foreach (CouncilPositionItemVM item in PositionList)
            {
                item.RefreshFromData();
            }

            foreach (CouncilKingsguardSlotItemVM item in KingsguardSlotList)
            {
                item.RefreshFromData();
            }
        }

        private static string GetRelevantSkillText(CouncilPosition position, Hero hero)
        {
            switch (position)
            {
                case CouncilPosition.GrandMaester:
                    return $"Medicine: {hero.GetSkillValue(DefaultSkills.Medicine)}";
                case CouncilPosition.MasterOfCoin:
                    return $"Steward: {hero.GetSkillValue(DefaultSkills.Steward)}";
                case CouncilPosition.MasterOfLaws:
                    return $"Leadership: {hero.GetSkillValue(DefaultSkills.Leadership)}";
                case CouncilPosition.MasterOfWhisperers:
                    return $"Roguery: {hero.GetSkillValue(DefaultSkills.Roguery)}";
                case CouncilPosition.MasterOfShips:
                    SkillObject shipmaster = SmallCouncils.Services.NavalSkillLookup.ShipmasterSkill;
                    return shipmaster != null ? $"Shipmaster: {hero.GetSkillValue(shipmaster)}" : string.Empty;
                case CouncilPosition.LordCommanderOfKingsguard:
                    return $"Vigor: {GetVigorSkillValue(hero)}";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Numeric value of whichever skill matters for this position, used
        /// to sort the candidate picker with the best-suited heroes first.
        /// Positions with no relevant skill (Hand of the King) sort as 0 —
        /// GetRelevantCandidatesForPosition doesn't offer a picker sorted by
        /// skill for those anyway, so this is just a safe default.
        /// </summary>
        private static int GetRelevantSkillValue(CouncilPosition position, Hero hero)
        {
            switch (position)
            {
                case CouncilPosition.GrandMaester:
                    return hero.GetSkillValue(DefaultSkills.Medicine);
                case CouncilPosition.MasterOfCoin:
                    return hero.GetSkillValue(DefaultSkills.Steward);
                case CouncilPosition.MasterOfLaws:
                    return hero.GetSkillValue(DefaultSkills.Leadership);
                case CouncilPosition.MasterOfWhisperers:
                    return hero.GetSkillValue(DefaultSkills.Roguery);
                case CouncilPosition.MasterOfShips:
                    SkillObject shipmaster = SmallCouncils.Services.NavalSkillLookup.ShipmasterSkill;
                    return shipmaster != null ? hero.GetSkillValue(shipmaster) : 0;
                case CouncilPosition.LordCommanderOfKingsguard:
                    return GetVigorSkillValue(hero);
                default:
                    return 0;
            }
        }

        private static int GetVigorSkillValue(Hero hero)
        {
            return System.Math.Max(hero.GetSkillValue(DefaultSkills.OneHanded),
                System.Math.Max(hero.GetSkillValue(DefaultSkills.TwoHanded), hero.GetSkillValue(DefaultSkills.Polearm)));
        }

        // ============================================================
        // Bindable properties
        // ============================================================

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
        public string PickerTitleText
        {
            get => _pickerTitleText;
            set
            {
                if (value != _pickerTitleText)
                {
                    _pickerTitleText = value;
                    OnPropertyChangedWithValue(value, nameof(PickerTitleText));
                }
            }
        }

        [DataSourceProperty]
        public bool IsPickerVisible
        {
            get => _isPickerVisible;
            set
            {
                if (value != _isPickerVisible)
                {
                    _isPickerVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsPickerVisible));
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
        public MBBindingList<CouncilPositionItemVM> PositionList
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
        public MBBindingList<CouncilKingsguardSlotItemVM> KingsguardSlotList
        {
            get => _kingsguardSlotList;
            set
            {
                if (value != _kingsguardSlotList)
                {
                    _kingsguardSlotList = value;
                    OnPropertyChangedWithValue(value, nameof(KingsguardSlotList));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CouncilCandidateItemVM> CandidateList
        {
            get => _candidateList;
            set
            {
                if (value != _candidateList)
                {
                    _candidateList = value;
                    OnPropertyChangedWithValue(value, nameof(CandidateList));
                }
            }
        }
    }
}
