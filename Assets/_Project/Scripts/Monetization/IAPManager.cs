using NatureBears.Core;
using NatureBears.Save;
using UnityEngine;

namespace NatureBears.Monetization
{
    /// <summary>
    /// DUMMY IAP manager — no store SDK. Purchases succeed instantly and fire
    /// the corresponding signals; swap the bodies for Unity IAP / store calls
    /// later without touching any listener. Ownership persists via SaveData.
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        public const string ProductRemoveAds = "com.naturebears.removeads";

        public static IAPManager Instance { get; private set; }

        public bool IsRemoveAdsOwned =>
            SaveManager.Instance != null && SaveManager.Instance.Data != null && SaveManager.Instance.Data.adsRemoved;

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
        }

        private void Start()
        {
            // Broadcast persisted ownership so AdManager & UI sync up on boot.
            SignalBus.Fire(new RemoveAdsStateChangedSignal(IsRemoveAdsOwned));
        }

        public void Purchase(string productId)
        {
            Debug.Log($"IAPManager: [DUMMY] purchasing '{productId}'...");

            // Real SDK: async store flow -> receipt validation -> grant.
            if (productId == ProductRemoveAds)
                GrantRemoveAds();

            SignalBus.Fire(new IapPurchaseCompletedSignal(productId));
        }

        public void RestorePurchases()
        {
            Debug.Log("IAPManager: [DUMMY] restoring purchases...");

            // Real SDK: query store receipts, re-grant owned entitlements.
            SignalBus.Fire(new PurchasesRestoredSignal());
        }

        private void GrantRemoveAds()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
            {
                SaveManager.Instance.Data.adsRemoved = true;
                SaveManager.Instance.Save();
            }

            SignalBus.Fire(new RemoveAdsStateChangedSignal(owned: true));
        }
    }
}
