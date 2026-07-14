using System;
using System.Collections.Generic;
using UnityEngine;

namespace NatureBears.Core
{
    /// <summary>
    /// Central event bus. Systems communicate EXCLUSIVELY through this class —
    /// never hold direct references to other managers.
    ///
    /// Signals are plain structs (see Signals.cs). Usage:
    /// <code>
    ///     SignalBus.Subscribe&lt;TimeCheatDetectedSignal&gt;(OnTimeCheat);
    ///     SignalBus.Fire(new TimeCheatDetectedSignal(isFirstOffense: true));
    ///     SignalBus.Unsubscribe&lt;TimeCheatDetectedSignal&gt;(OnTimeCheat);
    /// </code>
    /// </summary>
    public static class SignalBus
    {
        // Clear-delegates registered by every closed Registry<T> so Clear() can
        // reach all generic instantiations without reflection.
        private static readonly List<Action> ClearActions = new List<Action>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            Registry<T>.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            Registry<T>.Remove(handler);
        }

        public static void Fire<T>(in T signal) where T : struct
        {
            // Snapshot array: safe to Subscribe/Unsubscribe from inside a handler,
            // and zero allocation per fire.
            Action<T>[] snapshot = Registry<T>.Snapshot;
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i].Invoke(signal);
                }
                catch (Exception e)
                {
                    // One faulty subscriber must not break dispatch for the rest.
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>Removes ALL subscriptions of every signal type.</summary>
        public static void Clear()
        {
            for (int i = 0; i < ClearActions.Count; i++)
                ClearActions[i].Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Required with "Enter Play Mode Options" (domain reload disabled):
            // stale subscriptions from the previous play session must not survive.
            Clear();
        }

        private static class Registry<T> where T : struct
        {
            private static readonly List<Action<T>> Handlers = new List<Action<T>>();
            private static Action<T>[] _snapshot = Array.Empty<Action<T>>();
            private static bool _registeredClear;

            public static Action<T>[] Snapshot => _snapshot;

            public static void Add(Action<T> handler)
            {
                if (!_registeredClear)
                {
                    _registeredClear = true;
                    ClearActions.Add(ClearAll);
                }

                if (Handlers.Contains(handler))
                {
                    Debug.LogWarning($"SignalBus: duplicate subscription of {handler.Method.Name} to {typeof(T).Name} ignored.");
                    return;
                }

                Handlers.Add(handler);
                RebuildSnapshot();
            }

            public static void Remove(Action<T> handler)
            {
                if (Handlers.Remove(handler))
                    RebuildSnapshot();
            }

            private static void ClearAll()
            {
                Handlers.Clear();
                _snapshot = Array.Empty<Action<T>>();
            }

            private static void RebuildSnapshot()
            {
                _snapshot = Handlers.Count == 0 ? Array.Empty<Action<T>>() : Handlers.ToArray();
            }
        }
    }
}
