using NatureBears.Core;
using NatureBears.Data;
using UnityEngine;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Drives per-second automated production for every owned building except
    /// crafting stations (those belong to CraftingManager). Each building
    /// accumulates delta time; every completed cycle produces
    /// amountPerCycle * levelProductionMultiplier * level of each output
    /// (converters first pay their scaled inputs, halting while starved).
    /// Outputs are added by firing OnResourceGatheredSignal — never directly.
    /// Fires OnBuildingProductionStateChangedSignal on producing-state
    /// transitions so visible workers can animate without polling.
    /// </summary>
    public class WorkerBearManager : MonoBehaviour
    {
        public static WorkerBearManager Instance { get; private set; }

        private struct BuildingRuntime
        {
            public float cycleAccum;
            public bool wasProducing;
        }

        // Parallel to BuildingManager.Instance.Buildings; allocated once on load.
        private BuildingRuntime[] _runtime;
        private bool _hydrated;

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

        private void Update()
        {
            if (!_hydrated) return;
            TickAll(Time.deltaTime);
        }

        private void HandleGameLoaded(GameLoadedSignal signal)
        {
            BuildingManager manager = BuildingManager.Instance;
            int count = manager != null ? manager.Buildings.Count : 0;

            _runtime = new BuildingRuntime[count];
            _hydrated = true;

            // Reset views to idle (relevant after an in-play save reset — a fresh
            // runtime array can't fire the false-transition itself). Producing
            // buildings re-fire true on the next tick.
            for (int i = 0; i < count; i++)
            {
                BuildingData data = manager.Buildings[i];
                if (data != null)
                    SignalBus.Fire(new OnBuildingProductionStateChangedSignal(data.id, false));
            }

            // Offline earnings are handled by OfflineSimulator via
            // GetPassiveRatePerSecond (closed-form) — ticking TickAll with hours
            // of offline time would fire one signal per output per cycle.
        }

        /// <summary>
        /// Passive output per second of one resource from PURE producers only
        /// (no inputs, not a crafting station, level > 0). Fever Pitch is
        /// excluded — it never applies offline. Used by OfflineSimulator.
        /// </summary>
        public double GetPassiveRatePerSecond(ResourceType type)
        {
            BuildingManager manager = BuildingManager.Instance;
            if (manager == null) return 0;

            double total = 0;

            for (int i = 0; i < manager.Buildings.Count; i++)
            {
                BuildingData data = manager.Buildings[i];
                if (data == null || data.isCraftingStation || data.consumes.Count > 0) continue;

                int level = manager.GetLevel(data.id);
                if (level <= 0) continue;

                double mult = data.GetProductionMultiplier(level);
                float cycleDuration = Mathf.Max(data.cycleDurationSeconds, 0.05f);

                for (int p = 0; p < data.produces.Count; p++)
                {
                    ResourceRate rate = data.produces[p];
                    if (rate.resource == null || rate.resource.resourceType != type) continue;

                    total += rate.amountPerCycle * mult / cycleDuration;
                }
            }

            return total;
        }

        /// <summary>
        /// Advances every building by <paramref name="deltaSeconds"/>. A future
        /// offline-earnings pass calls this once with the offline duration;
        /// converters clamp naturally because inputs are paid per completed cycle.
        /// </summary>
        private void TickAll(double deltaSeconds)
        {
            BuildingManager manager = BuildingManager.Instance;
            ResourceManager stash = ResourceManager.Instance;
            if (manager == null || stash == null) return;

            double fever = TapManager.Instance != null ? TapManager.Instance.ActiveMultiplier : 1.0;

            for (int i = 0; i < _runtime.Length && i < manager.Buildings.Count; i++)
            {
                BuildingData data = manager.Buildings[i];
                if (data == null || data.isCraftingStation) continue;

                int level = manager.GetLevel(data.id);
                if (level <= 0)
                {
                    SetProducingState(ref _runtime[i], data, false);
                    continue;
                }

                _runtime[i].cycleAccum += (float)deltaSeconds;

                float cycleDuration = Mathf.Max(data.cycleDurationSeconds, 0.05f);
                double mult = data.GetProductionMultiplier(level);
                bool producing = true;

                while (_runtime[i].cycleAccum >= cycleDuration)
                {
                    if (!TryPayInputs(stash, data, mult))
                    {
                        // Starved converter: hold one banked cycle so production
                        // resumes the instant inputs arrive, without owing more.
                        producing = false;
                        _runtime[i].cycleAccum = cycleDuration;
                        break;
                    }

                    _runtime[i].cycleAccum -= cycleDuration;

                    for (int p = 0; p < data.produces.Count; p++)
                    {
                        ResourceRate rate = data.produces[p];
                        if (rate.resource == null) continue;

                        // Fever Pitch boosts outputs only — never input burn.
                        SignalBus.Fire(new OnResourceGatheredSignal(
                            rate.resource.resourceType, rate.amountPerCycle * mult * fever));
                    }
                }

                SetProducingState(ref _runtime[i], data, producing);
            }
        }

        /// <summary>
        /// Pays a converter's scaled inputs for one cycle. Pre-checks every
        /// balance before spending so a later failure can't leave a partial spend.
        /// True for pure producers (no inputs).
        /// </summary>
        private static bool TryPayInputs(ResourceManager stash, BuildingData data, double mult)
        {
            int count = data.consumes.Count;
            if (count == 0) return true;

            for (int c = 0; c < count; c++)
            {
                ResourceRate rate = data.consumes[c];
                if (rate.resource == null) continue;
                if (stash.GetBalance(rate.resource.resourceType) < rate.amountPerCycle * mult)
                    return false;
            }

            for (int c = 0; c < count; c++)
            {
                ResourceRate rate = data.consumes[c];
                if (rate.resource == null) continue;
                stash.TrySpend(rate.resource.resourceType, rate.amountPerCycle * mult);
            }

            return true;
        }

        private static void SetProducingState(ref BuildingRuntime runtime, BuildingData data, bool producing)
        {
            if (runtime.wasProducing == producing) return;

            runtime.wasProducing = producing;
            SignalBus.Fire(new OnBuildingProductionStateChangedSignal(data.id, producing));
        }
    }
}
