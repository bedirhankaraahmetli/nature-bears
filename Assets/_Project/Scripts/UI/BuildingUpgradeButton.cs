using NatureBears.Core;
using NatureBears.Data;
using NatureBears.Gameplay;
using NatureBears.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NatureBears.UI
{
    /// <summary>
    /// Inspector-wired upgrade button for one building/tent. Wire the Button's
    /// OnClick to <see cref="OnUpgradeClicked"/>. Refreshes its cost/level labels
    /// from OnBuildingUpgradedSignal and toggles interactability from
    /// OnResourceBalanceChangedSignal — no polling, all scene refs null-guarded.
    /// </summary>
    public class BuildingUpgradeButton : MonoBehaviour
    {
        [SerializeField] private BuildingData building;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text costLabel;
        [SerializeField] private TMP_Text levelLabel;

        // Cached last-shown values to skip redundant string allocations.
        private double _shownCost = double.NaN;
        private int _shownLevel = -1;

        private void Awake()
        {
            SignalBus.Subscribe<OnBuildingUpgradedSignal>(HandleBuildingUpgraded);
            SignalBus.Subscribe<OnResourceBalanceChangedSignal>(HandleBalanceChanged);
        }

        private void OnDestroy()
        {
            SignalBus.Unsubscribe<OnBuildingUpgradedSignal>(HandleBuildingUpgraded);
            SignalBus.Unsubscribe<OnResourceBalanceChangedSignal>(HandleBalanceChanged);
        }

        private void Start()
        {
            Refresh();
        }

        /// <summary>Wired to the Button's OnClick in the inspector.</summary>
        public void OnUpgradeClicked()
        {
            if (building == null || BuildingManager.Instance == null) return;
            BuildingManager.Instance.TryUpgrade(building);
        }

        private void HandleBuildingUpgraded(OnBuildingUpgradedSignal signal)
        {
            if (building == null || building.id != signal.BuildingId) return;
            Refresh();
        }

        private void HandleBalanceChanged(OnResourceBalanceChangedSignal signal)
        {
            if (building == null || building.costResource == null) return;
            if (signal.Type != building.costResource.resourceType) return;
            RefreshInteractable(signal.NewBalance);
        }

        private void Refresh()
        {
            if (building == null || BuildingManager.Instance == null) return;

            int level = BuildingManager.Instance.GetLevel(building.id);
            bool maxed = BuildingManager.Instance.IsMaxLevel(building);
            double cost = BuildingManager.Instance.GetUpgradeCost(building);

            if (levelLabel != null && level != _shownLevel)
            {
                _shownLevel = level;
                levelLabel.SetText("Lv {0}", level);
            }

            if (costLabel != null && (maxed || cost != _shownCost))
            {
                _shownCost = cost;
                costLabel.text = maxed ? "MAX" : NumberFormatter.Format(cost);
            }

            double balance = building.costResource != null && ResourceManager.Instance != null
                ? ResourceManager.Instance.GetBalance(building.costResource.resourceType)
                : 0;
            RefreshInteractable(balance);
        }

        private void RefreshInteractable(double costResourceBalance)
        {
            if (button == null || BuildingManager.Instance == null) return;

            button.interactable = !BuildingManager.Instance.IsMaxLevel(building) &&
                                  costResourceBalance >= BuildingManager.Instance.GetUpgradeCost(building);
        }
    }
}
