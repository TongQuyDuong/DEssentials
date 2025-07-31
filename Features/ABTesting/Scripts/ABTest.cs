using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Dessentials.Features.ABTesting
{
    public interface IABTest
    {
	    void Init();
	    void Fetch();
	    void ResetDefaults();

	    bool Enable { get; }
    }
    
    [Serializable]
    public class ABTest<T> : IABTest
    {
	    [SerializeField]
	    private string firebaseKey;

	    [SerializeField]
	    private bool enable;
#if ODIN_INSPECTOR
	    [ShowIf("@UnityEngine.Application.isPlaying && enable")]
#endif
	    [SerializeField]
	    private T FetchedValue;
#if ODIN_INSPECTOR
	    [ShowIf("@!UnityEngine.Application.isPlaying || !enable")]
#endif
	    [SerializeField]
	    private T DefaultValue;

	    public T Value
	    {
		    get
		    {
			    if (!enable)
			    {
				    return DefaultValue;
			    }

			    return FetchedValue;
		    }
	    }

	    public bool Enable => enable;

	    public bool fetched;

	    public void Init()
	    {
		    if (enable)
		    {
			    var stringPref = PlayerPrefs.GetString(firebaseKey, string.Empty);

			    if (!string.IsNullOrEmpty(stringPref))
			    {
				    FetchedValue = GetValueFromString(stringPref);
			    }
			    else
			    {
				    FetchedValue =  DefaultValue;
			    }
		    }
	    }

	    public void Fetch()
	    {
		    if (enable)
		    {
			    var exists = IRemoteConfigValueProvider.Global.TryGetStringValue(firebaseKey, out var stringValue);
			    if (exists)
			    {
				    PlayerPrefs.SetString(firebaseKey, stringValue);
			    
				    FetchedValue = GetValueFromString(stringValue);
			    }
			    else
			    {
				    var stringPref = PlayerPrefs.GetString(firebaseKey, string.Empty);

				    if (!string.IsNullOrEmpty(stringPref))
				    {
					    FetchedValue = GetValueFromString(stringPref);
				    }
				    else
				    {
					    FetchedValue =  DefaultValue;
				    }
			    }
			    
			    fetched = true;
		    }
	    }

	    public void ResetDefaults()
	    {
		    FetchedValue = DefaultValue;
	    }

	    public T GetGuaranteedValue()
	    {
		    if (!fetched)
		    {
			    return DefaultValue;
		    }

		    return FetchedValue;
	    }

	    [Button("Import Default From String")]
	    public void ImportDefaultFromString(string value)
	    {
		    DefaultValue = GetValueFromString(value);
	    }
	    
	    [Button]
	    public void PrintJson()
	    {
		    Debug.Log(JsonUtility.ToJson(DefaultValue, true));
	    }
			
	    private T GetValueFromString(string serializedValue)
	    {
		    try
		    {
			    switch (Type.GetTypeCode(typeof(T)))
			    {
				    case TypeCode.String:
					    return (T)Convert.ChangeType(serializedValue, typeof(T));
				    case TypeCode.Int32:
					    return (T)Convert.ChangeType(Convert.ToInt32(serializedValue), typeof(T));
				    case TypeCode.Single:
					    return (T)Convert.ChangeType(Convert.ToSingle(serializedValue), typeof(T));
				    case TypeCode.Double:
					    return (T)Convert.ChangeType(Convert.ToDouble(serializedValue), typeof(T));
				    case TypeCode.Boolean:
					    return (T)Convert.ChangeType(Convert.ToBoolean(serializedValue), typeof(T));
				    default:
					    return JsonUtility.FromJson<T>(serializedValue);
			    }
		    }
		    catch (Exception ex)
		    {
			    Debug.LogException(ex);
			    return default;
		    }
	    }
    }
}
