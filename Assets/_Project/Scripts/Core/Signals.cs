using NatureBears.Data;
using NatureBears.Monetization;
using NatureBears.Save;

namespace NatureBears.Core
{
    // ---------------------------------------------------------------------
    // Global signal definitions. Signals are immutable plain structs fired
    // through SignalBus.
    // ---------------------------------------------------------------------

    /// <summary>Fired by SaveManager once a save file has been loaded (or a new game created).</summary>
    public readonly struct GameLoadedSignal
    {
        public readonly bool IsNewGame;
        /// <summary>Seconds since the last save. 0 when offline earnings are capped (time cheat).</summary>
        public readonly double OfflineSeconds;
        /// <summary>The loaded save state — subscribers hydrate their runtime state from it.</summary>
        public readonly SaveData Data;

        public GameLoadedSignal(bool isNewGame, double offlineSeconds, SaveData data)
        {
            IsNewGame = isNewGame;
            OfflineSeconds = offlineSeconds;
            Data = data;
        }
    }

    /// <summary>
    /// Fired by SaveManager immediately BEFORE serializing to disk.
    /// Subscribers write their runtime state into <see cref="Data"/> so it is
    /// captured in the save — SaveManager never references gameplay systems.
    /// </summary>
    public readonly struct GameSavingSignal
    {
        public readonly SaveData Data;

        public GameSavingSignal(SaveData data)
        {
            Data = data;
        }
    }

    /// <summary>Fired by SaveManager after a successful write to disk.</summary>
    public readonly struct GameSavedSignal
    {
    }

    /// <summary>
    /// Fired by any producer (TapManager, worker buildings, ...) when resources
    /// are earned. ResourceManager is the sole subscriber that mutates balances.
    /// </summary>
    public readonly struct OnResourceGatheredSignal
    {
        public readonly ResourceType Type;
        public readonly double Amount;

        public OnResourceGatheredSignal(ResourceType type, double amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    /// <summary>Fired ONLY by ResourceManager after any balance mutation (gain or spend).</summary>
    public readonly struct OnResourceBalanceChangedSignal
    {
        public readonly ResourceType Type;
        public readonly double NewBalance;

        public OnResourceBalanceChangedSignal(ResourceType type, double newBalance)
        {
            Type = type;
            NewBalance = newBalance;
        }
    }

    /// <summary>
    /// Fired by BuildingManager after a successful purchase/upgrade, and once per
    /// building (owned or not, level 0 included) on load-hydration (with
    /// <see cref="UpgradeCost"/> = 0) so UI and views refresh without polling.
    /// </summary>
    public readonly struct OnBuildingUpgradedSignal
    {
        public readonly string BuildingId;
        public readonly int NewLevel;
        /// <summary>The cost that was just paid. 0 for the load-hydration broadcast.</summary>
        public readonly double UpgradeCost;

        public OnBuildingUpgradedSignal(string buildingId, int newLevel, double upgradeCost)
        {
            BuildingId = buildingId;
            NewLevel = newLevel;
            UpgradeCost = upgradeCost;
        }
    }

    /// <summary>
    /// Fired by WorkerBearManager when a building starts or stops actually
    /// producing (level reached 1, or a converter starved / regained inputs).
    /// Fired on state TRANSITIONS only — worker views animate from this.
    /// </summary>
    public readonly struct OnBuildingProductionStateChangedSignal
    {
        public readonly string BuildingId;
        public readonly bool IsProducing;

        public OnBuildingProductionStateChangedSignal(string buildingId, bool isProducing)
        {
            BuildingId = buildingId;
            IsProducing = isProducing;
        }
    }

    /// <summary>Fired by CraftingManager when a cook cycle begins (ingredients already spent).</summary>
    public readonly struct OnCraftingStartedSignal
    {
        /// <summary>BuildingData.id of the crafting station.</summary>
        public readonly string RecipeId;
        public readonly float Duration;

        public OnCraftingStartedSignal(string recipeId, float duration)
        {
            RecipeId = recipeId;
            Duration = duration;
        }
    }

    /// <summary>Fired by CraftingManager when a cook cycle finishes (output granted and auto-sold).</summary>
    public readonly struct OnCraftingCompletedSignal
    {
        /// <summary>BuildingData.id of the crafting station.</summary>
        public readonly string RecipeId;
        public readonly int Amount;

        public OnCraftingCompletedSignal(string recipeId, int amount)
        {
            RecipeId = recipeId;
            Amount = amount;
        }
    }

    /// <summary>
    /// Fired on every campfire tap — including blocked ones (fever already
    /// active or daily limit reached), so the UI can give deny feedback.
    /// Gauge fill fraction = GaugeTaps / TapsToFill.
    /// </summary>
    public readonly struct OnCampfireTappedSignal
    {
        public readonly int GaugeTaps;
        public readonly int TapsToFill;

        public OnCampfireTappedSignal(int gaugeTaps, int tapsToFill)
        {
            GaugeTaps = gaugeTaps;
            TapsToFill = tapsToFill;
        }
    }

    /// <summary>
    /// Fired by TapManager on fever start, on each whole-second boundary of the
    /// countdown, and on fever end — the UI renders these instead of polling.
    /// </summary>
    public readonly struct OnFeverPitchStateChangedSignal
    {
        public readonly bool IsActive;
        public readonly float RemainingSeconds;

        public OnFeverPitchStateChangedSignal(bool isActive, float remainingSeconds)
        {
            IsActive = isActive;
            RemainingSeconds = remainingSeconds;
        }
    }

    /// <summary>
    /// Fired when the device clock is behind the last save time (time-skip cheat).
    /// First offense triggers the one-time "Güzel denemeydi!" achievement UI.
    /// </summary>
    public readonly struct TimeCheatDetectedSignal
    {
        public readonly bool IsFirstOffense;

        public TimeCheatDetectedSignal(bool isFirstOffense)
        {
            IsFirstOffense = isFirstOffense;
        }
    }

    /// <summary>Fired by AdManager when a rewarded ad finishes (dummy flow until SDK integration).</summary>
    public readonly struct RewardedAdCompletedSignal
    {
        public readonly RewardedAdType Type;
        public readonly bool Success;

        public RewardedAdCompletedSignal(RewardedAdType type, bool success)
        {
            Type = type;
            Success = success;
        }
    }

    /// <summary>Fired by IAPManager after a (dummy) purchase completes.</summary>
    public readonly struct IapPurchaseCompletedSignal
    {
        public readonly string ProductId;

        public IapPurchaseCompletedSignal(string productId)
        {
            ProductId = productId;
        }
    }

    /// <summary>Fired by IAPManager after a restore-purchases flow finishes.</summary>
    public readonly struct PurchasesRestoredSignal
    {
    }

    /// <summary>Fired whenever RemoveAds ownership changes (purchase or restore).</summary>
    public readonly struct RemoveAdsStateChangedSignal
    {
        public readonly bool Owned;

        public RemoveAdsStateChangedSignal(bool owned)
        {
            Owned = owned;
        }
    }

    /// <summary>Fired by ObscuredDouble/ObscuredLong when in-memory tampering is detected.</summary>
    public readonly struct CurrencyTamperDetectedSignal
    {
    }
}
