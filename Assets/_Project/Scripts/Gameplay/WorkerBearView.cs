using NatureBears.Data;
using UnityEngine;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Base for visible animated workers (Grizzly Lumberjack, Chef Panda).
    /// Purely a signal consumer — it never talks back to gameplay systems.
    /// Placeholder-tolerant: the Animator may be missing or have no controller
    /// assigned; parameter writes silently no-op until final art is hooked up.
    ///
    /// Animator parameter contract (bool, set via precomputed hashes):
    ///   IsWorking — production running (chopping loop vs idle). GrizzlyLumberjackView.
    ///   IsCooking — cook cycle in progress (cooking loop vs idle). ChefPandaView.
    /// </summary>
    public abstract class WorkerBearView : MonoBehaviour
    {
        [Tooltip("The building this worker belongs to — signals are matched on its id.")]
        [SerializeField] protected BuildingData building;

        [Tooltip("Optional until final art exists; controller may be None.")]
        [SerializeField] protected Animator animator;

        protected static readonly int IsWorkingHash = Animator.StringToHash("IsWorking");
        protected static readonly int IsCookingHash = Animator.StringToHash("IsCooking");

        /// <summary>True when a signal's building/recipe id belongs to this worker.</summary>
        protected bool Matches(string buildingId)
        {
            return building != null && building.id == buildingId;
        }

        /// <summary>No-ops while the Animator or its controller is a placeholder.</summary>
        protected void SetAnimatorBool(int paramHash, bool value)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetBool(paramHash, value);
        }
    }
}
