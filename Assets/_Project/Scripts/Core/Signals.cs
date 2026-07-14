using NatureBears.Monetization;

namespace NatureBears.Core
{
    // ---------------------------------------------------------------------
    // Global signal definitions. Signals are immutable plain structs fired
    // through SignalBus. Gameplay signals (OnTimberwoodGathered,
    // OnFeverPitchStarted, ...) will be added alongside their systems.
    // ---------------------------------------------------------------------

    /// <summary>Fired by SaveManager once a save file has been loaded (or a new game created).</summary>
    public readonly struct GameLoadedSignal
    {
        public readonly bool IsNewGame;
        /// <summary>Seconds since the last save. 0 when offline earnings are capped (time cheat).</summary>
        public readonly double OfflineSeconds;

        public GameLoadedSignal(bool isNewGame, double offlineSeconds)
        {
            IsNewGame = isNewGame;
            OfflineSeconds = offlineSeconds;
        }
    }

    /// <summary>Fired by SaveManager after a successful write to disk.</summary>
    public readonly struct GameSavedSignal
    {
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
