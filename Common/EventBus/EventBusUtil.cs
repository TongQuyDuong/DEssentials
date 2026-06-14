using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dessentials.Common.EventBus
{
    public static class EventBusUtil
    {
        static List<Type> eventBusTypes;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ClearAllBuses();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            var eventTypes = CollectEventTypes();
            eventBusTypes = InitializeAllBuses(eventTypes);
        }

        static List<Type> CollectEventTypes()
        {
            var results = new List<Type>();
            var interfaceType = typeof(IEvent);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // Skip dynamic assemblies and known non-game assemblies
                if (assembly.IsDynamic) continue;

                var name = assembly.GetName().Name;
                if (name.StartsWith("Unity", StringComparison.Ordinal)
                    || name.StartsWith("System", StringComparison.Ordinal)
                    || name.StartsWith("Mono", StringComparison.Ordinal)
                    || name.StartsWith("mscorlib", StringComparison.Ordinal)
                    || name.StartsWith("netstandard", StringComparison.Ordinal))
                    continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type != null && type != interfaceType && interfaceType.IsAssignableFrom(type))
                        results.Add(type);
                }
            }

            return results;
        }

        static List<Type> InitializeAllBuses(List<Type> eventTypes)
        {
            var busTypes = new List<Type>(eventTypes.Count);
            var genericDef = typeof(EventBus<>);

            foreach (var eventType in eventTypes)
                busTypes.Add(genericDef.MakeGenericType(eventType));

            return busTypes;
        }

        static void ClearAllBuses()
        {
            if (eventBusTypes == null) return;

            foreach (var busType in eventBusTypes)
            {
                var clearMethod = busType.GetMethod("Clear", BindingFlags.Static | BindingFlags.NonPublic);
                clearMethod?.Invoke(null, null);
            }
        }
    }
}
