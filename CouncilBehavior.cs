using System.Collections.Generic;
using SmallCouncils.Models;
using TaleWorlds.CampaignSystem;

namespace SmallCouncils.Behaviors
{
    /// <summary>
    /// Stage 1: owns the per-kingdom CouncilData store, wires up save/load, and
    /// keeps the store in sync with kingdoms being created/destroyed.
    ///
    /// Assignment logic, AI evaluation, and passive benefit application are
    /// intentionally NOT in this file yet — those are Stage 2 onward, so this
    /// can be tested in isolation first (new game, save, reload, kingdom
    /// destroyed via war/collapse) before any gameplay logic depends on it.
    ///
    /// VERIFY BEFORE FIRST COMPILE:
    /// - IDataStore's namespace (used in SyncData below) — I've placed it under
    ///   TaleWorlds.CampaignSystem.SaveSystem, which is the common location in
    ///   recent versions, but confirm against your installed reference DLLs.
    /// - The exact event names/signatures below: OnNewGameCreatedEvent,
    ///   OnGameLoadFinishedEvent, KingdomCreatedEvent, KingdomDestroyedEvent.
    ///   These are stable, commonly-used CampaignEvents across many mods, but
    ///   I can't verify the exact delegate signatures without the assembly, so
    ///   please compile-check this file first before building on it.
    /// </summary>
    public class CouncilBehavior : CampaignBehaviorBase
    {
        public static CouncilBehavior Instance { get; private set; }

        private Dictionary<Kingdom, CouncilData> _councils;

        public CouncilBehavior()
        {
            Instance = this;
            _councils = new Dictionary<Kingdom, CouncilData>();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
            CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, OnKingdomCreated);
            CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("SmallCouncils_Councils", ref _councils);

            if (_councils == null)
            {
                _councils = new Dictionary<Kingdom, CouncilData>();
            }
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            EnsureAllKingdomsHaveCouncilData();
        }

        private void OnGameLoadFinished()
        {
            EnsureAllKingdomsHaveCouncilData();
        }

        private void OnKingdomCreated(Kingdom kingdom)
        {
            EnsureCouncilData(kingdom);
        }

        private void OnKingdomDestroyed(Kingdom kingdom)
        {
            if (kingdom != null)
            {
                _councils.Remove(kingdom);
            }
        }

        private void EnsureAllKingdomsHaveCouncilData()
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                EnsureCouncilData(kingdom);
            }
        }

        private void EnsureCouncilData(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return;
            }

            if (!_councils.ContainsKey(kingdom))
            {
                _councils[kingdom] = new CouncilData();
            }
        }

        /// <summary>
        /// Public read/write access point for later stages (assignment API,
        /// AI logic, UI). Returns null only if kingdom itself is null.
        /// </summary>
        public CouncilData GetCouncilData(Kingdom kingdom)
        {
            if (kingdom == null)
            {
                return null;
            }

            EnsureCouncilData(kingdom);
            return _councils[kingdom];
        }
    }
}
