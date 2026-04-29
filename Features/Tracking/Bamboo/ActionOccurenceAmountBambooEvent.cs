using System;
using System.Collections;
using System.Collections.Generic;
using Dessentials.Common.GlobalServices;
using UnityEngine;

namespace Dessentials.Features.Tracking
{
    public abstract class ActionOccurenceAmountBambooEvent : BaseBambooTrackEvent
    {
        protected virtual int ActionOccurenceAmountCurrentValue => 0;
        
        protected readonly int _withinAmountOfDaysThreshold;
        protected readonly int _actionOccurenceAmountReportThreshold;

        protected ActionOccurenceAmountBambooEvent
        (string eventName,
            int withinAmountOfDays,
            int actionOccurenceAmountReportThreshold,
            bool trackOnIAP = true) : base(eventName, trackOnIAP)
        {
            _withinAmountOfDaysThreshold = withinAmountOfDays;
            _actionOccurenceAmountReportThreshold = actionOccurenceAmountReportThreshold;
        }

        protected override bool IsTrackable
        {
            get
            {
                var withinDaysThresholdCondition = true;

                if (_withinAmountOfDaysThreshold > 0)
                {
                    var sessionDataProvider = ISessionDataProvider.Global;
                    
                    withinDaysThresholdCondition = 
                        sessionDataProvider != null
                        && sessionDataProvider.DaysSinceFirstActive <= _withinAmountOfDaysThreshold;
                }

                return base.IsTrackable
                       && ActionOccurenceAmountCurrentValue < _actionOccurenceAmountReportThreshold
                       && withinDaysThresholdCondition;
            }
        }

        public override bool InitTrackTask()
        {
            if (!base.InitTrackTask())
                return false;

            if (ActionOccurenceAmountCurrentValue >= _actionOccurenceAmountReportThreshold)
                TrackBambooEventFirstTime();
            else
                RegisterOnValueChanged();
            
            return true;
        }

        protected abstract void RegisterOnValueChanged();
        protected abstract void DeregisterOnValueChanged();

        protected void OnActionOccured()
        {
            var currentValue = ActionOccurenceAmountCurrentValue;

            if (currentValue >= _actionOccurenceAmountReportThreshold)
            {
                TrackBambooEventFirstTime();
                DeregisterOnValueChanged();
            }
        }
    }
}
