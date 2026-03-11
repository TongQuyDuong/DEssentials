using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dessentials.Common.GlobalServices;
using UnityEngine;

#if DESSENTIALS_ZEGO_SDK
using Zego;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Features.Tracking
{
    public interface IBambooTracker : IGlobalService<IBambooTracker>
    {
        public void HandleOnNewAdRevenuePaid();
    }
    
    public partial class BambooTracker : MonoBehaviour, IBambooTracker
    {
        public static Action ReInitializeBambooEvents;
        
        public int ActiveBambooTracksCount 
            => m_bambooEvents.Count(e => e.IsActive);
        
        private List<BaseBambooTrackEvent> m_bambooEvents = new();

        private void Awake()
        {
            ServiceLocator.Global.Register<IBambooTracker>(this);
            
            var bambooEventFields
                = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(field => field.IsDefined(typeof(RegisteredBambooEvent), false));

            foreach (var field in bambooEventFields)
            {
                m_bambooEvents.Add((BaseBambooTrackEvent)field.GetValue(this));
            }

            var gameInitializer = IGameInitializer.Global;

            gameInitializer?.RegisterOnPlayerDataInitialized(InitBambooEvents);

            ReInitializeBambooEvents += InitBambooEvents;

            PartialExtendedOnAwake();
        }

        partial void PartialExtendedOnAwake();

        private void OnDestroy()
        {
            ReInitializeBambooEvents -= InitBambooEvents;
        }

        private void InitBambooEvents()
        {
            foreach (var bambooEvent in m_bambooEvents)
            {
                bambooEvent.InitTrackTask();
            }
        }

        public void HandleOnNewAdRevenuePaid()
        {
            foreach (var bambooEvent in m_bambooEvents)
            {
                bambooEvent.TrackBambooEventAdditionalTimes();
            }
        }
        
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        private sealed class RegisteredBambooEvent : Attribute { }
    }
}
