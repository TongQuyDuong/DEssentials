using System;
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

	    string FirebaseKey { get; }
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
	    [ShowInInspector]
	    [ShowIf("@UnityEngine.Application.isPlaying && enable")]
#endif
	    private T FetchedValue;
	    
#if ODIN_INSPECTOR
	    [ShowIf("@!UnityEngine.Application.isPlaying || !enable")]
#endif
	    [SerializeField]
	    private T DefaultValue;
	    
	    [NonSerialized]
	    public bool fetched;

	    public T Value
	    {
		    get
		    {
			    if (!enable || !Application.isPlaying)
			    {
				    return DefaultValue;
			    }

			    return FetchedValue;
		    }
	    }

	    public string FirebaseKey => firebaseKey;
	    
	    public bool Enable => enable;

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

	    public void ImportDefaultValue(T value)
	    {
		    DefaultValue = value;
	    }

#if UNITY_EDITOR
	    [Button]
	    public void CopyJsonToClipboard(bool useNewtonsoft)
	    {
			var textToCopy = useNewtonsoft
				? Newtonsoft.Json.JsonConvert.SerializeObject(DefaultValue)
				: JsonUtility.ToJson(DefaultValue);

			GUIUtility.systemCopyBuffer = textToCopy;
			Debug.Log("Text copied to clipboard: " + textToCopy);
		}
#endif

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
					    return DeserializeComplex(serializedValue);
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				return default;
			}
	    }

	    // JsonUtility can't deserialize a top-level array/list. When T is a collection,
	    // wrap the json under a field whose type is T itself, then unwrap. No reflection:
	    // the nested Wrapper closes over T, so the field is already the right type.
	    private T DeserializeComplex(string json)
	    {
		    var type = typeof(T);
		    bool isCollection = type.IsArray ||
			    (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));

		    if (isCollection)
		    {
			    var wrapper = JsonUtility.FromJson<Wrapper>("{\"value\":" + json + "}");
			    return wrapper.value;
		    }

		    return JsonUtility.FromJson<T>(json);
	    }

	    [Serializable]
	    private class Wrapper
	    {
		    public T value;
	    }
    }
}
