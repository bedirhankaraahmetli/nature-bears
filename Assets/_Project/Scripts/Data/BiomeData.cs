using System.Collections.Generic;
using UnityEngine;

namespace NatureBears.Data
{
    /// <summary>
    /// A thematic expedition biome (Whispering Pines, Frostpeak Lake,
    /// Cherry Blossom Valley, Amberwood Grove, Starlit Tundra, ...).
    /// The crew moves to a new biome on (or every few) Hibernations.
    /// </summary>
    [CreateAssetMenu(fileName = "Biome_", menuName = "NatureBears/Biome Data")]
    public class BiomeData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used as save-file key. Never change after release.")]
        public string id;
        public string displayName;

        [Header("Presentation")]
        [Tooltip("Parallax background layers, back to front. Placeholders OK — swap final art later.")]
        public Sprite[] backgroundLayers;
        [Tooltip("ASMR ambient loop (crackling fire, stream, wind...).")]
        public AudioClip ambientLoop;
        [Tooltip("Biome-specific lo-fi BGM track (falls back to default when null).")]
        public AudioClip bgmTrack;

        [Header("Unlock")]
        [Tooltip("Number of Hibernations required before the crew can move here.")]
        public int unlockHibernationCount;

        [Header("Content")]
        public List<BuildingData> availableBuildings = new List<BuildingData>();
        public List<ResourceData> availableResources = new List<ResourceData>();
    }
}
