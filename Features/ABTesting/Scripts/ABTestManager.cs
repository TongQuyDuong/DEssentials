using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dessentials.Common;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
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

#if UNITY_EDITOR
		#if ODIN_INSPECTOR
	    [Button]
	    [PropertyOrder(-9999)]
	    [GUIColor("cyan")]
		#endif
	    public string SearchFirebaseKeyUsage(string firebaseKey)
	    {
		    var searchResult = "Key usage not found!";
		    
		    var abTestFields
			    = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				    .Where(field => field.IsDefined(typeof(RegisteredABTestAttribute), false));

		    foreach (var field in abTestFields)
		    {
			    var abTest = (IABTest)field.GetValue(this);

			    if (abTest.FirebaseKey == firebaseKey)
			    {
				    searchResult = $"Key use found in field: {field.Name}";
				    break;
			    }
		    }
		    
		    return searchResult;
	    }
#endif
		
		[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
		private sealed class RegisteredABTestAttribute : Attribute { }
    }

}
