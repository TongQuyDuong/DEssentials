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
            int actionOccurenceAmountReportThreshold) : base(eventName)
        {
            _withinAmountOfDaysThreshold = withinAmountOfDays;
            _actionOccurenceAmountReportThreshold = actionOccurenceAmountReportThreshold;
        } 

        public override bool IsTrackable
        {
            get
            {
                var sessionDataProvider = ISessionDataProvider.Global;

                return base.IsTrackable
                       && ActionOccurenceAmountCurrentValue < _actionOccurenceAmountReportThreshold
                       && sessionDataProvider != null
                       && sessionDataProvider.DaysSinceFirstActive <= _withinAmountOfDaysThreshold;
            }
        }

        public override bool InitTrackTask()
        {
            if (!base.InitTrackTask())
                return false;

            if (ActionOccurenceAmountCurrentValue >= _actionOccurenceAmountReportThreshold)
                TrackBambooEvent();
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
                TrackBambooEvent();
                DeregisterOnValueChanged();
            }
        }
    }
}
