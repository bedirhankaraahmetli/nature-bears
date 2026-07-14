using System.Collections;
using NatureBears.Core;
using UnityEngine;

namespace NatureBears.Monetization
{
    public enum RewardedAdType
    {
        /// <summary>An eagle drops a physical box in camp; watching grants instant resources.</summary>
        EagleDrop,
        /// <summary>Watch to double offline earnings upon return.</summary>
        OfflineMultiplier2x
    }

    /// <summary>
    /// DUMMY ad manager — no SDK. Simulates a rewarded ad with a short delay,
    /// then fires RewardedAdCompletedSignal so reward-granting systems can be
    /// built and tested now; swap the body of ShowRewardedAd for the real
    /// mediation SDK calls later without touching any listener.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [SerializeField] private float simulatedAdSeconds = 1f;

        private bool _removeAdsOwned;
        private bool _adInProgress;

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

            SignalBus.Subscribe<RemoveAdsStateChangedSignal>(OnRemoveAdsChanged);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                SignalBus.Unsubscribe<RemoveAdsStateChangedSignal>(OnRemoveAdsChanged);
        }

        private void OnRemoveAdsChanged(RemoveAdsStateChangedSignal signal)
        {
            _removeAdsOwned = signal.Owned;
        }

        // ------------------------------------------------------------------
        // Rewarded (always available to the player, even with RemoveAds)
        // ------------------------------------------------------------------

        public bool IsRewardedReady(RewardedAdType type)
        {
            // Real SDK: return mediation "ad loaded" state per placement.
            return !_adInProgress;
        }

        public void ShowRewardedAd(RewardedAdType type)
        {
            if (_adInProgress)
            {
                Debug.LogWarning("AdManager: rewarded ad already in progress.");
                return;
            }

            StartCoroutine(SimulateRewardedAd(type));
        }

        private IEnumerator SimulateRewardedAd(RewardedAdType type)
        {
            _adInProgress = true;
            Debug.Log($"AdManager: [DUMMY] showing rewarded ad '{type}'...");

            yield return new WaitForSecondsRealtime(simulatedAdSeconds);

            _adInProgress = false;
            SignalBus.Fire(new RewardedAdCompletedSignal(type, success: true));
        }

        // ------------------------------------------------------------------
        // Interstitial (disabled by RemoveAds)
        // ------------------------------------------------------------------

        public void ShowInterstitial()
        {
            if (_removeAdsOwned)
                return;

            Debug.Log("AdManager: [DUMMY] showing interstitial.");
        }
    }
}
