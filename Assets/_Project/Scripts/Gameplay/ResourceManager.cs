using System;
using System.Collections.Generic;
using NatureBears.Core;
using NatureBears.Data;
using NatureBears.Utils;
using UnityEngine;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// The Global Stash. Sole owner of resource balances (held as ObscuredDouble
    /// against memory tampering). Producers fire OnResourceGatheredSignal; this
    /// manager mutates the balance and broadcasts OnResourceBalanceChangedSignal.
    /// Hydrates from / persists to SaveData purely via SignalBus payloads.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        // Cached once; indexed by (int)type — ResourceType values are contiguous from 0.
        private static readonly string[] TypeNames = Enum.GetNames(typeof(ResourceType));
        private static readonly ResourceType[] AllTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));

        private readonly Dictionary<ResourceType, ObscuredDouble> _balances =
            new Dictionary<ResourceType, ObscuredDouble>(TypeNames.Length);

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

        public double GetBalance(ResourceType type)
        {
            return _balances.TryGetValue(type, out ObscuredDouble balance) ? (double)balance : 0;
        }

        /// <summary>Deducts <paramref name="amount"/> if affordable. False on invalid amount or insufficient funds.</summary>
        public bool TrySpend(ResourceType type, double amount)
        {
            if (amount <= 0) return false;

            double current = GetBalance(type);
            if (current < amount) return false;

            double newBalance = current - amount;
            _balances[type] = newBalance;
            FireBalanceChanged(type, newBalance);
            return true;
        }

        // ------------------------------------------------------------------
        // Signal handlers
        // ------------------------------------------------------------------

        private void HandleResourceGathered(OnResourceGatheredSignal signal)
        {
            if (signal.Amount <= 0) return;

            double newBalance = GetBalance(signal.Type) + signal.Amount;
            _balances[signal.Type] = newBalance;
            FireBalanceChanged(signal.Type, newBalance);
        }

        private void HandleGameLoaded(GameLoadedSignal signal)
        {
            _balances.Clear();

            for (int i = 0; i < AllTypes.Length; i++)
            {
                ResourceType type = AllTypes[i];
                signal.Data.resources.TryGetValue(TypeNames[i], out double saved);
                _balances[type] = saved;

                // Broadcast every type (including zeros) so UI labels hydrate.
                FireBalanceChanged(type, saved);
            }
        }

        private void HandleGameSaving(GameSavingSignal signal)
        {
            for (int i = 0; i < AllTypes.Length; i++)
                signal.Data.resources[TypeNames[i]] = GetBalance(AllTypes[i]);
        }

        private static void FireBalanceChanged(ResourceType type, double newBalance)
        {
            SignalBus.Fire(new OnResourceBalanceChangedSignal(type, newBalance));
        }
    }
}
