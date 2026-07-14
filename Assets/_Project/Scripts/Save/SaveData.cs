using System.Collections.Generic;

namespace NatureBears.Save
{
    /// <summary>
    /// Serializable save-game state (Newtonsoft JSON → AES-encrypted file).
    /// Values here are PLAIN types — in-memory obfuscation (ObscuredDouble)
    /// applies only to live runtime state, never to serialization.
    /// Bump <see cref="version"/> and migrate in SaveManager when the schema changes.
    /// Deliberately NOT [Serializable]: Newtonsoft doesn't need it, and Unity's
    /// serializer can't handle the Dictionary fields anyway (analyzer UAC1009).
    /// </summary>
    public class SaveData
    {
        public int version = 2;

        // --- Time / anti-cheat ---
        /// <summary>ISO-8601 round-trip ("o") UTC timestamp of the last save.</summary>
        public string lastSaveTimeUtc;
        /// <summary>True once a clock rollback has ever been detected on this save.</summary>
        public bool timeCheatDetected;
        /// <summary>True once the one-time "Güzel denemeydi!" achievement was shown.</summary>
        public bool timeCheatAchievementShown;

        // --- Progression ---
        public int hibernationCount;
        public string currentBiomeId;
        public double slumberPoints;
        public List<string> unlockedSkillNodeIds = new List<string>();

        // --- Economy (resources keys = ResourceType enum names; buildings keys = BuildingData.id) ---
        public Dictionary<string, double> resources = new Dictionary<string, double>();
        public Dictionary<string, int> buildingLevels = new Dictionary<string, int>();

        // --- Fever Pitch ---
        public int feverActivationsToday;
        /// <summary>UTC date "yyyy-MM-dd" of the last fever activation; drives the daily rollover.</summary>
        public string feverLastActivationDateUtc;

        // --- Monetization ---
        public bool adsRemoved;

        // --- Settings ---
        public float masterVolume = 1f;
        public float bgmVolume = 1f;
        public float ambientVolume = 1f;
        public float sfxVolume = 1f;
    }
}
