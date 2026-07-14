using System;
using System.Globalization;
using System.IO;
using NatureBears.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace NatureBears.Save
{
    /// <summary>
    /// Owns the encrypted local save file and the anti-time-skip check.
    /// Hybrid cloud sync and NTP validation are stubbed behind
    /// ICloudSaveProvider / INetworkTimeService until backends are chosen.
    ///
    /// Anti-time-skip: if the device clock is BEHIND the last save time on
    /// load, offline earnings are capped to 0 for this session. On the very
    /// first offense a TimeCheatDetectedSignal(IsFirstOffense: true) is fired
    /// so the UI can show the one-time "Güzel denemeydi!" achievement.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private const string FileName = "naturebears.sav";

        public static SaveManager Instance { get; private set; }

        public SaveData Data { get; private set; }

        /// <summary>True for the rest of this session after a clock rollback was detected on load.</summary>
        public bool OfflineEarningsCapped { get; private set; }

        private ICloudSaveProvider _cloud = new NullCloudSaveProvider();
        private INetworkTimeService _timeService = new NullNetworkTimeService();

        private string _filePath;
        private double _offlineSecondsAtLoad;

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
            _filePath = Path.Combine(Application.persistentDataPath, FileName);
        }

        private void Start()
        {
            // Load in Start (not Awake): all managers subscribe to GameLoadedSignal
            // in their Awake, and Awake order across objects is nondeterministic.
            Load();
        }

        // ------------------------------------------------------------------
        // Load / Save
        // ------------------------------------------------------------------

        public void Load()
        {
            bool isNewGame = false;

            try
            {
                if (File.Exists(_filePath))
                {
                    byte[] blob = File.ReadAllBytes(_filePath);
                    string json = SaveCrypto.Decrypt(blob);
                    Data = JsonConvert.DeserializeObject<SaveData>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: could not read save file, starting fresh. ({e.Message})");
                Data = null;
            }

            if (Data == null)
            {
                Data = new SaveData();
                isNewGame = true;
            }

            _offlineSecondsAtLoad = isNewGame ? 0 : ValidateTimeAndGetOfflineSeconds();

            SignalBus.Fire(new GameLoadedSignal(isNewGame, GetOfflineSeconds(), Data));
        }

        public void Save()
        {
            if (Data == null) return;

            Data.lastSaveTimeUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            // Let gameplay systems write their runtime state into Data before it
            // is serialized — keeps SaveManager decoupled from them (SignalBus only).
            SignalBus.Fire(new GameSavingSignal(Data));

            try
            {
                string json = JsonConvert.SerializeObject(Data);
                byte[] blob = SaveCrypto.Encrypt(json);

                // Atomic write: never leave a half-written save on crash/kill.
                string tmpPath = _filePath + ".tmp";
                File.WriteAllBytes(tmpPath, blob);
                if (File.Exists(_filePath))
                    File.Replace(tmpPath, _filePath, null);
                else
                    File.Move(tmpPath, _filePath);

                SignalBus.Fire(new GameSavedSignal());
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: save failed. ({e})");
            }
        }

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(_filePath))
                    File.Delete(_filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: delete failed. ({e})");
            }

            Data = new SaveData();
            OfflineEarningsCapped = false;
            _offlineSecondsAtLoad = 0;
        }

        /// <summary>Seconds away since last save — 0 when capped by the anti-cheat or on a new game.</summary>
        public double GetOfflineSeconds()
        {
            return OfflineEarningsCapped ? 0 : _offlineSecondsAtLoad;
        }

        // ------------------------------------------------------------------
        // Anti-time-skip
        // ------------------------------------------------------------------

        private double ValidateTimeAndGetOfflineSeconds()
        {
            if (string.IsNullOrEmpty(Data.lastSaveTimeUtc))
                return 0;

            if (!DateTime.TryParse(Data.lastSaveTimeUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime lastSave))
                return 0;

            // TODO: prefer _timeService.GetUtcNowAsync() once an NTP/server source exists.
            DateTime now = DateTime.UtcNow;

            if (now < lastSave)
            {
                OfflineEarningsCapped = true;

                bool isFirstOffense = !Data.timeCheatDetected;
                Data.timeCheatDetected = true;

                SignalBus.Fire(new TimeCheatDetectedSignal(isFirstOffense));
                Debug.LogWarning("SaveManager: clock rollback detected — offline earnings capped to 0 this session.");
                return 0;
            }

            return (now - lastSave).TotalSeconds;
        }

        // ------------------------------------------------------------------
        // Mobile lifecycle autosave
        // ------------------------------------------------------------------

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Save();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
