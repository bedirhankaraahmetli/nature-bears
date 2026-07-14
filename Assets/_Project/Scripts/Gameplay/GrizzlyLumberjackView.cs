using NatureBears.Core;

namespace NatureBears.Gameplay
{
    /// <summary>
    /// Visible Grizzly Lumberjack. Plays the chopping loop (IsWorking) whenever
    /// its building is actively producing, driven purely by
    /// OnBuildingProductionStateChangedSignal.
    /// </summary>
    public class GrizzlyLumberjackView : WorkerBearView
    {
        private void OnEnable()
        {
            SignalBus.Subscribe<OnBuildingProductionStateChangedSignal>(HandleProductionStateChanged);
        }

        private void OnDisable()
        {
            SignalBus.Unsubscribe<OnBuildingProductionStateChangedSignal>(HandleProductionStateChanged);
        }

        private void HandleProductionStateChanged(OnBuildingProductionStateChangedSignal signal)
        {
            if (!Matches(signal.BuildingId)) return;
            SetAnimatorBool(IsWorkingHash, signal.IsProducing);
        }
    }
}
