using NatureBears.Core;
using NatureBears.Data;
using UnityEngine;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Chef Panda's kitchen. Auto-crafts whenever the station is owned and every
    /// ingredient of the (data-driven) recipe is in the Global Stash: ingredients
    /// are spent up front, OnCraftingStartedSignal fires, and after
    /// cycleDurationSeconds the output is granted (OnResourceGatheredSignal),
    /// OnCraftingCompletedSignal fires, and the meal auto-sells for Golden Honey
    /// at the output resource's baseValue. The whole recipe — inputs, output,
    /// cook time, sell price — lives on the BuildingData / ResourceData assets.
    /// A cook in progress is intentionally not persisted (≤ one cycle of value).
    /// </summary>
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance { get; private set; }

        [Tooltip("The crafting-station BuildingData (Building_ChefPanda, isCraftingStation = true).")]
        [SerializeField] private BuildingData chefPanda;

        private bool _cooking;
        private float _cookRemaining;
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
            if (!_hydrated || chefPanda == null) return;

            if (_cooking)
            {
                _cookRemaining -= Time.deltaTime;
                if (_cookRemaining > 0f) return;

                _cooking = false;
                CompleteCook();
            }

            if (!_cooking)
                TryStartCook();
        }

        private void HandleGameLoaded(GameLoadedSignal signal)
        {
            // Interrupted cooks don't survive a restart (fever-doesn't-persist
            // precedent). Amount-0 completion unsticks the view's cooking
            // animation after an in-play save reset.
            if (_cooking && chefPanda != null)
                SignalBus.Fire(new OnCraftingCompletedSignal(chefPanda.id, 0));

            _cooking = false;
            _cookRemaining = 0f;
            _hydrated = true;
        }

        private void TryStartCook()
        {
            BuildingManager buildings = BuildingManager.Instance;
            ResourceManager stash = ResourceManager.Instance;
            if (buildings == null || stash == null) return;
            if (buildings.GetLevel(chefPanda.id) <= 0) return;

            // Pre-check every ingredient before spending — no partial spends.
            for (int i = 0; i < chefPanda.consumes.Count; i++)
            {
                ResourceRate rate = chefPanda.consumes[i];
                if (rate.resource == null) continue;
                if (stash.GetBalance(rate.resource.resourceType) < rate.amountPerCycle)
                    return;
            }

            for (int i = 0; i < chefPanda.consumes.Count; i++)
            {
                ResourceRate rate = chefPanda.consumes[i];
                if (rate.resource == null) continue;
                stash.TrySpend(rate.resource.resourceType, rate.amountPerCycle);
            }

            _cooking = true;
            _cookRemaining = chefPanda.cycleDurationSeconds;
            SignalBus.Fire(new OnCraftingStartedSignal(chefPanda.id, chefPanda.cycleDurationSeconds));
        }

        private void CompleteCook()
        {
            if (chefPanda.produces.Count == 0 || chefPanda.produces[0].resource == null) return;

            ResourceRate output = chefPanda.produces[0];
            double amount = output.amountPerCycle;

            // Grant the meal first so lifetime "gathered" stats stay truthful,
            // then auto-sell it for Golden Honey at the meal's baseValue.
            SignalBus.Fire(new OnResourceGatheredSignal(output.resource.resourceType, amount));
            SignalBus.Fire(new OnCraftingCompletedSignal(chefPanda.id, (int)amount));

            ResourceManager stash = ResourceManager.Instance;
            if (stash != null && stash.TrySpend(output.resource.resourceType, amount))
            {
                SignalBus.Fire(new OnResourceGatheredSignal(
                    ResourceType.GoldenHoney, amount * output.resource.baseValue));
            }
        }
    }
}
