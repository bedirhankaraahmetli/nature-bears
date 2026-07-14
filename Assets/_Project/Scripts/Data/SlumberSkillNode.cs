using System.Collections.Generic;
using UnityEngine;

namespace NatureBears.Data
{
    /// <summary>
    /// One node of the infinitely scalable Slumber Tree (prestige skill tree),
    /// bought with Slumber Points earned through Hibernation.
    /// New nodes = new assets; no code changes needed to extend the tree.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillNode_", menuName = "NatureBears/Slumber Skill Node")]
    public class SlumberSkillNode : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used as save-file key. Never change after release.")]
        public string nodeId;
        public string skillName;
        [TextArea]
        public string description;
        public Sprite icon;

        [Header("Unlock")]
        [Tooltip("Cost in Slumber Points.")]
        public double unlockCost;
        [Tooltip("All listed nodes must be unlocked first. Empty = root node.")]
        public List<SlumberSkillNode> prerequisiteNodes = new List<SlumberSkillNode>();

        [Header("Effect")]
        public SkillBranch branch;
        public SkillEffectType effectType;
        [Tooltip("Meaning depends on effectType: multipliers are factors (0.25 = +25%), caps are seconds, reductions are fractions.")]
        public double effectValue;
    }
}
