using NatureBears.Core;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Visible Chef Panda. Plays the cooking loop (IsCooking) for the duration
    /// of each cook cycle, driven purely by the crafting signals.
    /// </summary>
    public class ChefPandaView : WorkerBearView
    {
        private void OnEnable()
        {
            SignalBus.Subscribe<OnCraftingStartedSignal>(HandleCraftingStarted);
            SignalBus.Subscribe<OnCraftingCompletedSignal>(HandleCraftingCompleted);
        }

        private void OnDisable()
        {
            SignalBus.Unsubscribe<OnCraftingStartedSignal>(HandleCraftingStarted);
            SignalBus.Unsubscribe<OnCraftingCompletedSignal>(HandleCraftingCompleted);
        }

        private void HandleCraftingStarted(OnCraftingStartedSignal signal)
        {
            if (!Matches(signal.RecipeId)) return;
            SetAnimatorBool(IsCookingHash, true);
        }

        private void HandleCraftingCompleted(OnCraftingCompletedSignal signal)
        {
            if (!Matches(signal.RecipeId)) return;
            SetAnimatorBool(IsCookingHash, false);
        }
    }
}
