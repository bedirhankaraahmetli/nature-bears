using NatureBears.Core;
using NatureBears.Gameplay;
using NatureBears.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NatureBears.UI
{
    /// <summary>
    /// Inspector-wired Hibernate (prestige) button with confirmation panel.
    /// Wire the main Button's OnClick to <see cref="OnHibernateClicked"/> and the
    /// panel's buttons to <see cref="OnConfirmClicked"/> / <see cref="OnCancelClicked"/>.
    /// Refreshes its label from OnPotentialSlumberPointsChangedSignal — no
    /// polling, all scene refs null-guarded.
    /// </summary>
    public class HibernateButton : MonoBehaviour
    {
        [SerializeField] private Button hibernateButton;
        [SerializeField] private TMP_Text pointsLabel;

        [Header("Confirmation Panel")]
        [Tooltip("Inactive by default; shown when the Hibernate button is tapped.")]
        [SerializeField] private GameObject confirmPanelRoot;
        [SerializeField] private TMP_Text confirmText;

        // Cached last-shown value to skip redundant string allocations.
        private double _shownPoints = double.NaN;

        private void Awake()
        {
            SignalBus.Subscribe<OnPotentialSlumberPointsChangedSignal>(HandlePotentialChanged);
        }

        private void OnDestroy()
        {
            SignalBus.Unsubscribe<OnPotentialSlumberPointsChangedSignal>(HandlePotentialChanged);
        }

        private void Start()
        {
            // Covers scene-load ordering: PrestigeManager may have broadcast
            // before this button subscribed.
            Refresh(PrestigeManager.Instance != null ? PrestigeManager.Instance.PotentialSlumberPoints : 0);
        }

        // ------------------------------------------------------------------
        // Public API (inspector-wired buttons)
        // ------------------------------------------------------------------

        /// <summary>Wired to the Hibernate Button's OnClick — opens the confirmation panel.</summary>
        public void OnHibernateClicked()
        {
            double points = PrestigeManager.Instance != null ? PrestigeManager.Instance.PotentialSlumberPoints : 0;
            if (points < 1) return;

            if (confirmText != null)
            {
                confirmText.text =
                    $"Hibernate for {NumberFormatter.Format(points)} Slumber Points?\n" +
                    "Everything except Slumber Points will reset.";
            }

            if (confirmPanelRoot != null)
                confirmPanelRoot.SetActive(true);
        }

        /// <summary>Wired to the confirmation panel's Confirm button.</summary>
        public void OnConfirmClicked()
        {
            if (confirmPanelRoot != null)
                confirmPanelRoot.SetActive(false);

            if (PrestigeManager.Instance != null)
                PrestigeManager.Instance.Hibernate();
        }

        /// <summary>Wired to the confirmation panel's Cancel button.</summary>
        public void OnCancelClicked()
        {
            if (confirmPanelRoot != null)
                confirmPanelRoot.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Signal handlers
        // ------------------------------------------------------------------

        private void HandlePotentialChanged(OnPotentialSlumberPointsChangedSignal signal)
        {
            Refresh(signal.PotentialPoints);
        }

        private void Refresh(double points)
        {
            if (points == _shownPoints) return;
            _shownPoints = points;

            if (pointsLabel != null)
                pointsLabel.text = $"Hibernate for {NumberFormatter.Format(points)} Points";

            if (hibernateButton != null)
                hibernateButton.interactable = points >= 1;
        }
    }
}
