using System;
using System.Collections.Generic;
using NatureBears.Core;
using NatureBears.Data;
using NatureBears.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NatureBears.UI
{
    /// <summary>
    /// Basic HUD: resource balance labels + Fever Pitch gauge/timer.
    /// Purely signal-driven — no Update(), no polling. All scene references are
    /// null-guarded so the HUD works with placeholder/partial wiring (rule 7).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Serializable]
        private struct ResourceLabelBinding
        {
            public ResourceType type;
            public TMP_Text label;
        }

        public static UIManager Instance { get; private set; }

        [Header("Resource Labels")]
        [SerializeField] private ResourceLabelBinding[] resourceLabels;

        [Header("Fever Pitch")]
        [Tooltip("Non-interactable slider showing campfire gauge fill.")]
        [SerializeField] private Slider feverGaugeSlider;
        [Tooltip("Container shown only while Fever Pitch is active (holds the timer).")]
        [SerializeField] private GameObject feverActiveRoot;
        [SerializeField] private TMP_Text feverTimerText;

        private readonly Dictionary<ResourceType, TMP_Text> _labelByType =
            new Dictionary<ResourceType, TMP_Text>(8);
        private readonly Dictionary<ResourceType, double> _lastShownValue =
            new Dictionary<ResourceType, double>(8);

        private bool _feverShownActive;
        private int _lastShownFeverSecond = -1;

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

            Instance = this; // scene-owned: no DontDestroyOnLoad

            if (resourceLabels != null)
            {
                for (int i = 0; i < resourceLabels.Length; i++)
                {
                    if (resourceLabels[i].label != null)
                        _labelByType[resourceLabels[i].type] = resourceLabels[i].label;
                }
            }

            SignalBus.Subscribe<OnResourceBalanceChangedSignal>(HandleBalanceChanged);
            SignalBus.Subscribe<OnCampfireTappedSignal>(HandleCampfireTapped);
            SignalBus.Subscribe<OnFeverPitchStateChangedSignal>(HandleFeverStateChanged);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            SignalBus.Unsubscribe<OnResourceBalanceChangedSignal>(HandleBalanceChanged);
            SignalBus.Unsubscribe<OnCampfireTappedSignal>(HandleCampfireTapped);
            SignalBus.Unsubscribe<OnFeverPitchStateChangedSignal>(HandleFeverStateChanged);
        }

        // ------------------------------------------------------------------
        // Signal handlers
        // ------------------------------------------------------------------

        private void HandleBalanceChanged(OnResourceBalanceChangedSignal signal)
        {
            if (!_labelByType.TryGetValue(signal.Type, out TMP_Text label)) return;

            // Skip redundant updates — the string alloc happens only on real change.
            if (_lastShownValue.TryGetValue(signal.Type, out double shown) && shown == signal.NewBalance)
                return;

            _lastShownValue[signal.Type] = signal.NewBalance;
            label.text = NumberFormatter.Format(signal.NewBalance);
        }

        private void HandleCampfireTapped(OnCampfireTappedSignal signal)
        {
            if (feverGaugeSlider == null) return;

            feverGaugeSlider.maxValue = signal.TapsToFill;
            feverGaugeSlider.value = signal.GaugeTaps;
        }

        private void HandleFeverStateChanged(OnFeverPitchStateChangedSignal signal)
        {
            if (signal.IsActive != _feverShownActive)
            {
                _feverShownActive = signal.IsActive;
                _lastShownFeverSecond = -1;

                if (feverActiveRoot != null)
                    feverActiveRoot.SetActive(signal.IsActive);
            }

            if (!signal.IsActive || feverTimerText == null) return;

            int wholeSecond = Mathf.CeilToInt(signal.RemainingSeconds);
            if (wholeSecond == _lastShownFeverSecond) return;

            _lastShownFeverSecond = wholeSecond;
            feverTimerText.SetText("{0}s", wholeSecond); // TMP formats without a string alloc
        }
    }
}
