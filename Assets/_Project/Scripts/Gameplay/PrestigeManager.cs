using System;
using NatureBears.Core;
using NatureBears.Data;
using NatureBears.Save;
using NatureBears.Utils;
using UnityEngine;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Prestige ("Hibernation"). Tracks all-time Golden Honey earned this run
    /// (via OnResourceGatheredSignal) and converts it to Slumber Points:
    /// floor(sqrt(honey / 1000)). Hibernate() resets the run — everything except
    /// the Slumber Point balance is wiped — then saves and rehydrates in place.
    /// Fires OnPotentialSlumberPointsChangedSignal only when the floored value
    /// changes, so UI never polls.
    /// </summary>
    public class PrestigeManager : MonoBehaviour
    {
        public static PrestigeManager Instance { get; private set; }

        private const double HoneyPerPointDivisor = 1000.0;

        private ObscuredDouble _totalHoneyThisRun;
        private int _hibernationCount;
        private double _lastFiredPotential = -1;

        /// <summary>Slumber Points a hibernation would award right now.</summary>
        public double PotentialSlumberPoints =>
            Math.Floor(Math.Sqrt((double)_totalHoneyThisRun / HoneyPerPointDivisor));

        public int HibernationCount => _hibernationCount;

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

            SignalBus.Subscribe<OnResourceGatheredSignal>(HandleResourceGathered);
            SignalBus.Subscribe<GameLoadedSignal>(HandleGameLoaded);
            SignalBus.Subscribe<GameSavingSignal>(HandleGameSaving);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            SignalBus.Unsubscribe<OnResourceGatheredSignal>(HandleResourceGathered);
            SignalBus.Unsubscribe<GameLoadedSignal>(HandleGameLoaded);
            SignalBus.Unsubscribe<GameSavingSignal>(HandleGameSaving);
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Executes the prestige reset. Wired to the Hibernate confirm button.
        /// False when fewer than 1 point would be earned (nothing happens).
        /// </summary>
        public bool Hibernate()
        {
            double points = PotentialSlumberPoints;
            if (points < 1 || SaveManager.Instance == null) return false;

            _hibernationCount++;
            _totalHoneyThisRun = 0;

            // 1. Synchronous run reset: ResourceManager zeroes everything except
            //    SlumberPoints, BuildingManager clears levels — all handlers
            //    complete before the next line.
            SignalBus.Fire(new OnHibernationStartedSignal(points, _hibernationCount));

            // 2. Award AFTER the reset so the points can't be wiped by it.
            SignalBus.Fire(new OnResourceGatheredSignal(ResourceType.SlumberPoints, points));
            FirePotentialIfChanged();

            // 3. Persist the post-hibernation state, then rehydrate in place.
            //    ReloadFromMemory delivers OfflineSeconds == 0, so OfflineSimulator
            //    discards any pending pass — no offline re-grant.
            SaveManager.Instance.Save();
            SaveManager.Instance.ReloadFromMemory();
            return true;
        }

#if UNITY_EDITOR
        // ------------------------------------------------------------------
        // Dev helpers (editor-only, stripped from device builds)
        // ------------------------------------------------------------------

        /// <summary>
        /// Right-click the PrestigeManager component header in Play Mode →
        /// "DEV Grant 10K Golden Honey". Flows through the real gather path so
        /// both the stash and the run counter update naturally.
        /// </summary>
        [ContextMenu("DEV Grant 10K Golden Honey")]
        private void DevGrantHoney()
        {
            SignalBus.Fire(new OnResourceGatheredSignal(ResourceType.GoldenHoney, 10_000));
            Debug.Log($"[DEV] Granted 10K Golden Honey — potential is now {PotentialSlumberPoints} Slumber Points.");
        }
#endif

        // ------------------------------------------------------------------
        // Signal handlers
        // ------------------------------------------------------------------

        private void HandleResourceGathered(OnResourceGatheredSignal signal)
        {
            if (signal.Type != ResourceType.GoldenHoney || signal.Amount <= 0) return;

            _totalHoneyThisRun = (double)_totalHoneyThisRun + signal.Amount;
            FirePotentialIfChanged();
        }

        private void HandleGameLoaded(GameLoadedSignal signal)
        {
            _totalHoneyThisRun = signal.Data.totalGoldenHoneyThisRun;
            _hibernationCount = signal.Data.hibernationCount;

            // Force one broadcast so the Hibernate button always hydrates.
            _lastFiredPotential = -1;
            FirePotentialIfChanged();
        }

        private void HandleGameSaving(GameSavingSignal signal)
        {
            signal.Data.totalGoldenHoneyThisRun = _totalHoneyThisRun;
            signal.Data.hibernationCount = _hibernationCount;
        }

        private void FirePotentialIfChanged()
        {
            double potential = PotentialSlumberPoints;
            if (potential == _lastFiredPotential) return;

            _lastFiredPotential = potential;
            SignalBus.Fire(new OnPotentialSlumberPointsChangedSignal(potential));
        }
    }
}
