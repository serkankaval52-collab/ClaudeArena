using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreFactory.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> Events = new();
        private static readonly Dictionary<Type, object> StickyEvents = new();
        private static readonly HashSet<Type> StickyTypes = new();

        public static void MarkSticky<T>() => StickyTypes.Add(typeof(T));

        public static void Subscribe<T>(Action<T> listener)
        {
            if (listener == null) return;
            Type type = typeof(T);
            if (!Events.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                Events[type] = list;
            }
            if (!list.Contains(listener))
            {
                list.Add(listener);
            }
            if (StickyEvents.TryGetValue(type, out var pending))
            {
                listener((T)pending);
            }
        }

        public static void Unsubscribe<T>(Action<T> listener)
        {
            if (listener == null) return;
            Type type = typeof(T);
            if (Events.TryGetValue(type, out var list))
            {
                list.Remove(listener);
                if (list.Count == 0) Events.Remove(type);
            }
        }

        public static int Publish<T>(T eventArgs)
        {
            Type type = typeof(T);
            if (StickyTypes.Contains(type))
            {
                StickyEvents[type] = eventArgs;
            }
            if (!Events.TryGetValue(type, out var list) || list.Count == 0)
            {
                return 0;
            }

            // 100% Safe Local Copy Allocation for total re-entrancy safety (Bug 3 fixed!)
            var localCopy = list.ToArray();
            int delivered = 0;
            for (int i = 0; i < localCopy.Length; i++)
            {
                try
                {
                    ((Action<T>)localCopy[i])?.Invoke(eventArgs);
                    delivered++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] {type.Name} listener failed: {e}");
                }
            }
            return delivered;
        }

        public static int GetListenerCount<T>() =>
            Events.TryGetValue(typeof(T), out var list) ? list.Count : 0;

        public static void ClearSticky<T>() => StickyEvents.Remove(typeof(T));

        public static void Clear()
        {
            Events.Clear();
            StickyEvents.Clear();
            StickyTypes.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Events.Clear();
            StickyEvents.Clear();
            StickyTypes.Clear();
        }
    }
}