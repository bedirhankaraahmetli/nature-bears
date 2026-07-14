using System;
using System.Globalization;
using NatureBears.Core;
using NatureBears.Data;
using UnityEngine;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Active gameplay: tapping trees for Timberwood and tapping the campfire
    /// to fill the Fever Pitch gauge (daily-limited 30s x2 boost).
    /// TapTree/TapCampfire are hooked to uGUI Button OnClick in the inspector —
    /// the UI never references this class in code.
    ///
    /// Fever countdown broadcasts OnFeverPitchStateChangedSignal on start, on
    /// each whole-second boundary and on end, so the UI never polls.
    /// An in-flight fever does NOT survive an app restart (session-only);
    /// only the daily activation count + date persist.
    /// </summary>
    public class TapManager : MonoBehaviour
    {
        private const string FeverDateFormat = "yyyy-MM-dd";

        public static TapManager Instance { get; private set; }

        [Header("Tree Tapping")]
        [Tooltip("Timberwood gained per tree tap (before multipliers).")]
        [SerializeField] private double baseTapYield = 1.0;

        [Header("Fever Pitch")]
        [Tooltip("Campfire taps required to fill the gauge and trigger Fever Pitch.")]
        [SerializeField] private int tapsToFillGauge = 20;
        [SerializeField] private float feverDurationSeconds = 30f;
        [Tooltip("Multiplier applied to tap yields while Fever Pitch is active.")]
        [SerializeField] private double feverMultiplier = 2.0;
        [Tooltip("How many times per (UTC) day the gauge can be triggered.")]
        [SerializeField] private int maxDailyFeverActivations = 3;

        private int _gaugeTaps;
        private bool _feverActive;
        private float _feverRemaining;
        private int _lastBroadcastSecond;
        private int _feverActivationsToday;
        private DateTime _lastFeverDayUtc;

        /// <summary>True while the 30s x2 boost is running — read by future producers.</summary>
        public bool IsFeverActive => _feverActive;

        /// <summary>Current multiplier for active (tap) yields.</summary>
        public double ActiveMultiplier => _feverActive ? feverMultiplier : 1.0;

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
            SignalBus.Subscribe<GameSavingSignal>(HandleGameSaving);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            SignalBus.Unsubscribe<GameLoadedSignal>(HandleGameLoaded);
            SignalBus.Unsubscribe<GameSavingSignal>(HandleGameSaving);
        }

        private void Update()
        {
            if (!_feverActive) return;

            _feverRemaining -= Time.deltaTime;

            if (_feverRemaining <= 0f)
            {
                EndFever();
                return;
            }

            int wholeSecond = Mathf.CeilToInt(_feverRemaining);
            if (wholeSecond != _lastBroadcastSecond)
            {
                _lastBroadcastSecond = wholeSecond;
                SignalBus.Fire(new OnFeverPitchStateChangedSignal(true, _feverRemaining));
            }
        }

        // ------------------------------------------------------------------
        // Button hooks (wired via uGUI Button OnClick in the inspector)
        // ------------------------------------------------------------------

        public void TapTree()
        {
            double amount = baseTapYield * ActiveMultiplier;
            SignalBus.Fire(new OnResourceGatheredSignal(ResourceType.Timberwood, amount));
        }

        public void TapCampfire()
        {
            RollDailyIfNeeded();

            // Blocked: fever already running, or daily limit spent. Still fire
            // the gauge signal so the UI can give deny feedback.
            if (_feverActive || _feverActivationsToday >= maxDailyFeverActivations)
            {
                FireGauge();
                return;
            }

            _gaugeTaps++;

            if (_gaugeTaps >= tapsToFillGauge)
                ActivateFever();
            else
                FireGauge();
        }

        // ------------------------------------------------------------------
        // Fever state machine
        // ------------------------------------------------------------------

        private void ActivateFever()
        {
            _gaugeTaps = 0;
            FireGauge();

            _feverActivationsToday++;
            _lastFeverDayUtc = DateTime.UtcNow.Date;

            _feverActive = true;
            _feverRemaining = feverDurationSeconds;
            _lastBroadcastSecond = Mathf.CeilToInt(feverDurationSeconds);

            SignalBus.Fire(new OnFeverPitchStateChangedSignal(true, _feverRemaining));
        }

        private void EndFever()
        {
            _feverActive = false;
            _feverRemaining = 0f;
            SignalBus.Fire(new OnFeverPitchStateChangedSignal(false, 0f));
        }

        private void RollDailyIfNeeded()
        {
            if (DateTime.UtcNow.Date != _lastFeverDayUtc)
                _feverActivationsToday = 0;
        }

        private void FireGauge()
        {
            SignalBus.Fire(new OnCampfireTappedSignal(_gaugeTaps, tapsToFillGauge));
        }

        // ------------------------------------------------------------------
        // Persistence (SignalBus payloads only — no SaveManager reference)
        // ------------------------------------------------------------------

        private void HandleGameLoaded(GameLoadedSignal signal)
        {
            _gaugeTaps = 0;
            _feverActive = false;
            _feverRemaining = 0f;

            bool sameDay = DateTime.TryParseExact(
                               signal.Data.feverLastActivationDateUtc,
                               FeverDateFormat, CultureInfo.InvariantCulture,
                               DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                               out DateTime lastDay)
                           && lastDay.Date == DateTime.UtcNow.Date;

            _lastFeverDayUtc = sameDay ? lastDay.Date : default;
            _feverActivationsToday = sameDay ? signal.Data.feverActivationsToday : 0;

            // Push initial state so the UI hydrates without polling.
            FireGauge();
            SignalBus.Fire(new OnFeverPitchStateChangedSignal(false, 0f));
        }

        private void HandleGameSaving(GameSavingSignal signal)
        {
            signal.Data.feverActivationsToday = _feverActivationsToday;
            signal.Data.feverLastActivationDateUtc = _lastFeverDayUtc == default
                ? null
                : _lastFeverDayUtc.ToString(FeverDateFormat, CultureInfo.InvariantCulture);
        }
    }
}
