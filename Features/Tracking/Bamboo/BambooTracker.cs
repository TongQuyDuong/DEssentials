using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dessentials.Common.GlobalServices;
using UnityEngine;

namespace Dessentials.Features.Tracking
{
    public partial class BambooTracker : MonoBehaviour
    {
        public static Action OnCheatFirstAdRevenueActivated;
        
        private List<BaseBambooTrackEvent> m_bambooEvents = new();

        private void Awake()
        {
            var bambooEventFields
                = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(field => field.IsDefined(typeof(RegisteredBambooEvent), false));

            foreach (var field in bambooEventFields)
            {
                m_bambooEvents.Add((BaseBambooTrackEvent)field.GetValue(this));
            }

            var gameInitializer = IGameInitializer.Global;

            gameInitializer?.RegisterOnPlayerDataInitialized(InitBambooEvents);

            OnCheatFirstAdRevenueActivated += InitBambooEvents;
        }

        private void OnDestroy()
        {
            OnCheatFirstAdRevenueActivated -= InitBambooEvents;
        }

        private void InitBambooEvents()
        {
            foreach (var bambooEvent in m_bambooEvents)
            {
                if (bambooEvent.IsTrackable)
                    bambooEvent.InitTrackTask();
            }
        }
        
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        private sealed class RegisteredBambooEvent : Attribute { }
    }
}
