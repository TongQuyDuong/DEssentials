using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dessentials.Common;
using UnityEngine;

namespace Dessentials.Features.ABTesting
{
    public partial class ABTestManager : PersistentMonoSingleton<ABTestManager>
    {
        private List<IABTest> m_ABTests = new();
        
		protected override void Awake()
		{
			base.Awake(); 

			var abTestFields
				= GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
					.Where(field => field.IsDefined(typeof(RegisteredABTestAttribute), false));

			foreach (var field in abTestFields)
			{
				m_ABTests.Add((IABTest)field.GetValue(this));
			}
						
			IRemoteConfigValueProvider.Global.OnFetched += OnRemoteConfigFetched;
			
			foreach (var test in m_ABTests)
			{
				test.Init();
			}
        }

		private void OnRemoteConfigFetched()
		{
			foreach (var test in m_ABTests)
			{
				test.Init();
				if (test.Enable)
				{
					test.Fetch();
				}
				else
				{
					test.ResetDefaults();
				}
			}
		}
		
		[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
		private sealed class RegisteredABTestAttribute : Attribute { }
    }

}
