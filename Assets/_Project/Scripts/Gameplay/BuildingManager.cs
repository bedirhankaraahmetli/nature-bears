using System.Collections.Generic;
using NatureBears.Core;
using NatureBears.Data;
using UnityEngine;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Sole owner of building/tent levels. Purchases and upgrades are paid via
    /// ResourceManager.TrySpend using the building's cost curve
    /// (cost = baseCost * growth^level); every level change is broadcast with
    /// OnBuildingUpgradedSignal. Hydrates from / persists to
    /// SaveData.buildingLevels purely via SignalBus payloads.
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [Tooltip("Every purchasable building/tent in the game. Source of truth for WorkerBearManager's tick order.")]
        [SerializeField] private List<BuildingData> buildings = new List<BuildingData>();

        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>(8);

        /// <summary>All building definitions, in inspector order.</summary>
        public IReadOnlyList<BuildingData> Buildings => buildings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SignalBus.Subscribe<GameLoadedSignal>(HandleGameLoaded);
            SignalBus.Subscribe<GameSavingSignal>(HandleGameSaving);
            SignalBus.Subscribe<OnHibernationStartedSignal>(HandleHibernationStarted);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            SignalBus.Unsubscribe<GameLoadedSignal>(HandleGameLoaded);
            SignalBus.Unsubscribe<GameSavingSignal>(HandleGameSaving);
            SignalBus.Unsubscribe<OnHibernationStartedSignal>(HandleHibernationStarted);
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>Current level of a building. 0 = not yet purchased.</summary>
        public int GetLevel(string buildingId)
        {
            return buildingId != null && _levels.TryGetValue(buildingId, out int level) ? level : 0;
        }

        /// <summary>
        /// Cost of the next level (purchase when unowned, upgrade otherwise),
        /// after the Slumber Tree CheaperTents discount.
        /// </summary>
        public double GetUpgradeCost(BuildingData data)
        {
            if (data == null) return 0;

            double costMult = SkillManager.Instance != null
                ? SkillManager.Instance.BuildingCostMultiplier
                : 1.0;

            return data.GetCostForLevel(GetLevel(data.id)) * costMult;
        }

        public bool IsMaxLevel(BuildingData data)
        {
            return data != null && data.maxLevel > 0 && GetLevel(data.id) >= data.maxLevel;
        }

        /// <summary>
        /// Attempts to buy the next level. False when maxed, unaffordable, or
        /// the definition is incomplete. Fires OnBuildingUpgradedSignal on success.
        /// </summary>
        public bool TryUpgrade(BuildingData data)
        {
            if (data == null || data.costResource == null || IsMaxLevel(data)) return false;

            int currentLevel = GetLevel(data.id);
            double cost = GetUpgradeCost(data);

            if (ResourceManager.Instance == null ||
                !ResourceManager.Instance.TrySpend(data.costResource.resourceType, cost))
                return false;

            int newLevel = currentLevel + 1;
            _levels[data.id] = newLevel;
            SignalBus.Fire(new OnBuildingUpgradedSignal(data.id, newLevel, cost));
            return true;
        }

        // ------------------------------------------------------------------
        // Signal handlers
        // ------------------------------------------------------------------

        private void HandleGameLoaded(GameLoadedSignal signal)
        {
            _levels.Clear();

            for (int i = 0; i < buildings.Count; i++)
            {
                BuildingData data = buildings[i];
                if (data == null) continue;

                signal.Data.buildingLevels.TryGetValue(data.id, out int level);
                if (level > 0)
                    _levels[data.id] = level;

                // Broadcast EVERY building (cost 0, level 0 included) so UI and
                // views hydrate without polling — including after an in-play reset.
                SignalBus.Fire(new OnBuildingUpgradedSignal(data.id, level, 0));
            }
        }

        private void HandleHibernationStarted(OnHibernationStartedSignal signal)
        {
            _levels.Clear();

            // Mirror the load-hydration broadcast so upgrade buttons and worker
            // views refresh to level 0 without polling.
            for (int i = 0; i < buildings.Count; i++)
            {
                BuildingData data = buildings[i];
                if (data == null) continue;

                SignalBus.Fire(new OnBuildingUpgradedSignal(data.id, 0, 0));
            }
        }

        private void HandleGameSaving(GameSavingSignal signal)
        {
            // Rebuild from live state — stale keys (e.g. levels cleared by a
            // hibernation) must not survive in the save file.
            signal.Data.buildingLevels.Clear();

            foreach (KeyValuePair<string, int> pair in _levels)
                signal.Data.buildingLevels[pair.Key] = pair.Value;
        }
    }
}
