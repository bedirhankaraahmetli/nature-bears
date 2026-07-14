using UnityEngine;

namespace NatureBears.Data
{
    /// <summary>
    /// Definition of a single resource type (Timberwood, Embers, Golden Honey, ...).
    /// Instances live under Assets/_Project/ScriptableObjects/.
    /// </summary>
    [CreateAssetMenu(fileName = "Resource_", menuName = "NatureBears/Resource Data")]
    public class ResourceData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used as save-file key. Never change after release.")]
        public string id;
        [Tooltip("Enum bridge used by gameplay code and signals; keep in sync with id.")]
        public ResourceType resourceType;
        public string displayName;
        public Sprite icon;

        [Header("Economy")]
        public ResourceCategory category;
        [Tooltip("Base sell/exchange value in Golden Honey (where applicable).")]
        public double baseValue;

        [TextArea]
        public string description;
    }
}
