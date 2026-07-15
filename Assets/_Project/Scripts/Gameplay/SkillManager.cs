using System;
using System.Collections.Generic;
using NatureBears.Core;
using NatureBears.Data;
using NatureBears.Save;
using UnityEngine;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Sole owner of the permanent Slumber Tree state (prestige meta-progression).
    /// Unlocked node ids live ONLY in SaveData.unlockedSkillNodeIds — never in
    /// the ScriptableObjects. Purchases spend Slumber Points via ResourceManager,
    /// force an immediate save (permanent purchases must not be lost to a crash)
    /// and broadcast OnSkillUnlockedSignal. Effect aggregates are cached on
    /// hydration/purchase so consumers (WorkerBearManager reads per tick) get a
    /// plain field — no per-frame summing. Deliberately does NOT subscribe to
    /// OnHibernationStartedSignal: the tree survives hibernation.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        public static SkillManager Instance { get; private set; }

        /// <summary>Building costs are never reduced below this fraction of base (safe math floor).</summary>
        private const double MinCostMultiplier = 0.1;

        [Tooltip("Every Slumber Tree node in the game. Extending the tree = authoring a new asset and adding it here.")]
        [SerializeField] private List<SlumberSkillNode> skills = new List<SlumberSkillNode>();

        // Ids not matching any asset in `skills` (e.g. a node temporarily removed
        // from the list) are preserved here and written back on save — a config
        // mistake must never destroy a player's permanent purchases.
        private readonly HashSet<string> _unlockedIds = new HashSet<string>();

        // Cached aggregates, recomputed only on hydration and purchase.
        private double _productionMultiplier = 1.0;
        private double _offlineEarningsMultiplier = 1.0;
        private double _activeTapMultiplier = 1.0;
        private double _buildingCostMultiplier = 1.0;
        private double _offlineCapBonusSeconds;

        /// <summary>All node definitions, in inspector order (for tree UI).</summary>
        public IReadOnlyList<SlumberSkillNode> Skills => skills;

        /// <summary>1 + Σ(unlocked ProductionRateMultiplier values). Applied to every worker output, online and offline.</summary>
        public double GlobalProductionMultiplier => _productionMultiplier;

        /// <summary>1 + Σ(unlocked OfflineEarningsMultiplier values). Scales offline gains.</summary>
        public double OfflineEarningsMultiplier => _offlineEarningsMultiplier;

        /// <summary>1 + Σ(unlocked ActiveTapMultiplier values). For TapManager (wired in a later pass).</summary>
        public double ActiveTapMultiplier => _activeTapMultiplier;

        /// <summary>Factor applied to building costs: 1 − Σ(unlocked CostReduction values), floored at 0.1 — never free.</summary>
        public double BuildingCostMultiplier => _buildingCostMultiplier;

        /// <summary>Extra offline-cap seconds on top of OfflineSimulator's 24h base: Σ(unlocked OfflineDurationCap values).</summary>
        public double OfflineCapBonusSeconds => _offlineCapBonusSeconds;

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
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            SignalBus.Unsubscribe<GameLoadedSignal>(HandleGameLoaded);
            SignalBus.Unsubscribe<GameSavingSignal>(HandleGameSaving);
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        public bool IsUnlocked(SlumberSkillNode node)
        {
            return node != null && !string.IsNullOrEmpty(node.nodeId) && _unlockedIds.Contains(node.nodeId);
        }

        public bool IsUnlocked(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && _unlockedIds.Contains(nodeId);
        }

        /// <summary>True when every prerequisite node is already unlocked (empty list = root node).</summary>
        public bool ArePrerequisitesMet(SlumberSkillNode node)
        {
            if (node == null) return false;

            for (int i = 0; i < node.prerequisiteNodes.Count; i++)
            {
                SlumberSkillNode prereq = node.prerequisiteNodes[i];
                if (prereq != null && !IsUnlocked(prereq)) return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to buy a node. False when already owned, prerequisites are
        /// missing, the definition is incomplete, or Slumber Points are
        /// insufficient. On success the purchase is saved to disk immediately
        /// and OnSkillUnlockedSignal is fired.
        /// </summary>
        public bool TryPurchaseSkill(SlumberSkillNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.nodeId)) return false;
            if (_unlockedIds.Contains(node.nodeId)) return false;
            if (!ArePrerequisitesMet(node)) return false;

            // Root freebies (cost 0) skip the spend — TrySpend rejects amount <= 0.
            if (node.unlockCost > 0)
            {
                if (ResourceManager.Instance == null ||
                    !ResourceManager.Instance.TrySpend(ResourceType.SlumberPoints, node.unlockCost))
                    return false;
            }

            _unlockedIds.Add(node.nodeId);
            RecomputeAggregates();

            if (SaveManager.Instance != null)
                SaveManager.Instance.Save();

            SignalBus.Fire(new OnSkillUnlockedSignal(node.nodeId, node.effectType, node.effectValue, true));
            return true;
        }

#if UNITY_EDITOR
        // ------------------------------------------------------------------
        // Dev helpers (editor-only, stripped from device builds)
        // ------------------------------------------------------------------

        /// <summary>
        /// Right-click the SkillManager component header in Play Mode →
        /// "DEV Grant 100 Slumber Points". Flows through the real gather path.
        /// </summary>
        [ContextMenu("DEV Grant 100 Slumber Points")]
        private void DevGrantSlumberPoints()
        {
            SignalBus.Fire(new OnResourceGatheredSignal(ResourceType.SlumberPoints, 100));
            Debug.Log("[DEV] Granted 100 Slumber Points.");
        }
#endif

        // ------------------------------------------------------------------
        // Signal handlers
        // ------------------------------------------------------------------

        private void HandleGameLoaded(GameLoadedSignal signal)
        {
            _unlockedIds.Clear();

            List<string> saved = signal.Data.unlockedSkillNodeIds;
            if (saved != null)
            {
                for (int i = 0; i < saved.Count; i++)
                {
                    if (!string.IsNullOrEmpty(saved[i]))
                        _unlockedIds.Add(saved[i]);
                }
            }

            RecomputeAggregates();

            // Broadcast EVERY node (locked ones included) so skill UI hydrates
            // without polling, regardless of Awake/subscription order — including
            // after an in-play save reset re-locking everything.
            for (int i = 0; i < skills.Count; i++)
            {
                SlumberSkillNode node = skills[i];
                if (node == null || string.IsNullOrEmpty(node.nodeId)) continue;

                SignalBus.Fire(new OnSkillUnlockedSignal(
                    node.nodeId, node.effectType, node.effectValue, _unlockedIds.Contains(node.nodeId)));
            }
        }

        private void HandleGameSaving(GameSavingSignal signal)
        {
            List<string> list = signal.Data.unlockedSkillNodeIds;
            list.Clear();

            foreach (string id in _unlockedIds)
                list.Add(id);
        }

        // ------------------------------------------------------------------
        // Effect aggregation
        // ------------------------------------------------------------------

        private void RecomputeAggregates()
        {
            double production = 0, offlineEarnings = 0, tap = 0, costReduction = 0, capBonus = 0;

            for (int i = 0; i < skills.Count; i++)
            {
                SlumberSkillNode node = skills[i];
                if (node == null || string.IsNullOrEmpty(node.nodeId)) continue;
                if (!_unlockedIds.Contains(node.nodeId)) continue;

                // Negative effect values are authoring mistakes — never let them
                // shrink a multiplier below its baseline.
                double value = Math.Max(0, node.effectValue);

                switch (node.effectType)
                {
                    case SkillEffectType.ProductionRateMultiplier: production += value; break;
                    case SkillEffectType.OfflineEarningsMultiplier: offlineEarnings += value; break;
                    case SkillEffectType.ActiveTapMultiplier: tap += value; break;
                    case SkillEffectType.CostReduction: costReduction += value; break;
                    case SkillEffectType.OfflineDurationCap: capBonus += value; break;
                }
            }

            _productionMultiplier = 1.0 + production;
            _offlineEarningsMultiplier = 1.0 + offlineEarnings;
            _activeTapMultiplier = 1.0 + tap;
            _buildingCostMultiplier = Math.Clamp(1.0 - costReduction, MinCostMultiplier, 1.0);
            _offlineCapBonusSeconds = capBonus;
        }
    }
}
