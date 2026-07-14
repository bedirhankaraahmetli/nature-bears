using System;
using System.Collections.Generic;
using UnityEngine;

namespace NatureBears.Data
{
    /// <summary>A resource amount produced or consumed per production cycle.</summary>
    [Serializable]
    public class ResourceRate
    {
        public ResourceData resource;
        public double amountPerCycle;
    }

    /// <summary>
    /// Definition of a production building / worker station (Lumberjack Grove,
    /// Kitchen Tent, Fishing Spot, ...). The assigned bear may be a visible
    /// animated worker or a hidden math-only passive.
    /// </summary>
    [CreateAssetMenu(fileName = "Building_", menuName = "NatureBears/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used as save-file key. Never change after release.")]
        public string id;
        public string displayName;
        public Sprite icon;

        [Header("Worker")]
        [Tooltip("Prefab with Animator for visible workers (Grizzly Lumberjack, Chef Panda). Null for hidden passives.")]
        public GameObject workerPrefab;
        [Tooltip("Visible & animated in the camp, or hidden math-only passive.")]
        public bool isVisibleWorker;
        public string assignedBearName;

        [Header("Production")]
        public List<ResourceRate> produces = new List<ResourceRate>();
        public List<ResourceRate> consumes = new List<ResourceRate>();
        [Tooltip("Seconds per production cycle at level 1.")]
        public float cycleDurationSeconds = 1f;

        [Header("Cost Curve  (cost = baseCost * growth^level)")]
        public ResourceData costResource;
        public double baseCost = 10;
        public float costGrowthFactor = 1.15f;
        [Tooltip("0 = uncapped.")]
        public int maxLevel;

        /// <summary>Cost of buying the upgrade that takes the building TO level+1 (level is current, 0-based for unowned).</summary>
        public double GetCostForLevel(int level)
        {
            return baseCost * Math.Pow(costGrowthFactor, level);
        }

        /// <summary>Total cost of buying <paramref name="count"/> levels starting from <paramref name="fromLevel"/> (geometric series).</summary>
        public double GetBulkCost(int fromLevel, int count)
        {
            if (count <= 0) return 0;
            double g = costGrowthFactor;
            if (Math.Abs(g - 1.0) < 1e-9)
                return baseCost * count;
            double first = GetCostForLevel(fromLevel);
            return first * (Math.Pow(g, count) - 1.0) / (g - 1.0);
        }
    }
}
