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
    /// Inspector-wired purchase button for one Slumber Tree node. Wire the
    /// Button's OnClick to <see cref="OnPurchaseClicked"/>. Refreshes from
    /// OnSkillUnlockedSignal (ANY node — this node's prerequisites may have
    /// changed) and toggles interactability from the Slumber Point balance —
    /// no polling, all scene refs null-guarded.
    /// </summary>
    public class SkillPurchaseButton : MonoBehaviour
    {
        [SerializeField] private SlumberSkillNode skill;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text costLabel;

        // Cached last-shown text state to skip redundant string allocations.
        private bool _shownUnlocked;
        private bool _hasShownState;

        private void Awake()
        {
            SignalBus.Subscribe<OnSkillUnlockedSignal>(HandleSkillUnlocked);
            SignalBus.Subscribe<OnResourceBalanceChangedSignal>(HandleBalanceChanged);
        }

        private void OnDestroy()
        {
            SignalBus.Unsubscribe<OnSkillUnlockedSignal>(HandleSkillUnlocked);
            SignalBus.Unsubscribe<OnResourceBalanceChangedSignal>(HandleBalanceChanged);
        }

        private void Start()
        {
            // Covers scene-load ordering: SkillManager may have broadcast its
            // hydration pass before this button subscribed.
            Refresh();
        }

        /// <summary>Wired to the Button's OnClick in the inspector.</summary>
        public void OnPurchaseClicked()
        {
            if (skill == null || SkillManager.Instance == null) return;
            SkillManager.Instance.TryPurchaseSkill(skill);
        }

        // ------------------------------------------------------------------
        // Signal handlers
        // ------------------------------------------------------------------

        private void HandleSkillUnlocked(OnSkillUnlockedSignal signal)
        {
            // Any unlocked node can be a prerequisite of this one — always refresh.
            Refresh();
        }

        private void HandleBalanceChanged(OnResourceBalanceChangedSignal signal)
        {
            if (signal.Type != ResourceType.SlumberPoints) return;
            RefreshInteractable(signal.NewBalance);
        }

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        private void Refresh()
        {
            if (skill == null) return;

            bool unlocked = SkillManager.Instance != null && SkillManager.Instance.IsUnlocked(skill);

            if (!_hasShownState || unlocked != _shownUnlocked)
            {
                _hasShownState = true;
                _shownUnlocked = unlocked;

                if (nameLabel != null)
                    nameLabel.text = skill.skillName;

                if (costLabel != null)
                    costLabel.text = unlocked ? "OWNED" : NumberFormatter.Format(skill.unlockCost);
            }

            double balance = ResourceManager.Instance != null
                ? ResourceManager.Instance.GetBalance(ResourceType.SlumberPoints)
                : 0;
            RefreshInteractable(balance);
        }

        private void RefreshInteractable(double slumberPointBalance)
        {
            if (button == null || skill == null || SkillManager.Instance == null) return;

            button.interactable = !SkillManager.Instance.IsUnlocked(skill) &&
                                  SkillManager.Instance.ArePrerequisitesMet(skill) &&
                                  slumberPointBalance >= skill.unlockCost;
        }
    }
}
