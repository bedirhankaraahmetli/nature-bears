using System;
using System.Collections.Generic;
using NatureBears.Core;
using NatureBears.Data;
using UnityEngine;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Grants offline earnings on load. GameLoadedSignal already delivers
    /// anti-time-skip-capped offline seconds; this simulator caps them at 24h,
    /// computes closed-form gains for pure raw producers (converters and
    /// crafting never run offline) and grants them via OnResourceGatheredSignal.
    /// Processing is deferred one frame past the load signal so every manager
    /// is guaranteed hydrated regardless of Awake/subscription order.
    /// </summary>
    public class OfflineSimulator : MonoBehaviour
    {
        public static OfflineSimulator Instance { get; private set; }

        // TODO (skill tree): SkillEffectType.OfflineDurationCap raises this.
        private const double MaxOfflineSeconds = 24.0 * 3600.0;
        // Below this the pass is skipped entirely (new game, hibernation reload).
        private const double MinProcessSeconds = 1.0;
        // TODO (skill tree): SkillEffectType.OfflineEarningsMultiplier scales this;
        // the OfflineMultiplier2x rewarded ad hooks in here too.
        private const double OfflineEarningsMultiplier = 1.0;

        // Only raw gathering happens offline — matches the pure-producer filter
        // in WorkerBearManager.GetPassiveRatePerSecond.
        private static readonly ResourceType[] OfflineTypes =
        {
            ResourceType.Timberwood,
            ResourceType.FreshSalmon,
            ResourceType.RareWildflowers,
            ResourceType.ForestMushrooms,
        };

        private double _pendingOfflineSeconds;
        private bool _hasPending;

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
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            SignalBus.Unsubscribe<GameLoadedSignal>(HandleGameLoaded);
        }

        private void HandleGameLoaded(GameLoadedSignal signal)
        {
            // The latest load always overwrites any unprocessed pass — a
            // hibernation reload delivers 0 seconds and correctly discards it.
            _pendingOfflineSeconds = signal.OfflineSeconds;
            _hasPending = signal.OfflineSeconds >= MinProcessSeconds;
        }

        private void Update()
        {
            if (!_hasPending) return;

            // Safe here: SignalBus.Fire is synchronous and SaveManager loads in
            // Start(), so every GameLoadedSignal handler (resource/building
            // hydration included) completed before this frame's Update.
            _hasPending = false;
            Process(Math.Min(_pendingOfflineSeconds, MaxOfflineSeconds));
        }

        private void Process(double cappedSeconds)
        {
            var gains = new Dictionary<ResourceType, double>(OfflineTypes.Length);

            for (int i = 0; i < OfflineTypes.Length; i++)
            {
                double rate = WorkerBearManager.Instance != null
                    ? WorkerBearManager.Instance.GetPassiveRatePerSecond(OfflineTypes[i])
                    : 0;

                double gain = rate * cappedSeconds * OfflineEarningsMultiplier;
                if (gain <= 0) continue;

                gains[OfflineTypes[i]] = gain;
                SignalBus.Fire(new OnResourceGatheredSignal(OfflineTypes[i], gain));
            }

            SignalBus.Fire(new OnOfflineGainsCalculatedSignal(gains, cappedSeconds));
        }
    }
}
