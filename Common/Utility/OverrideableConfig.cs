using System;
using System.Collections.Generic;
using Dessentials.Serializables;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Dessentials.Common.Utility
{
	/// <summary>
	/// A config holder that serves a single default <typeparamref name="TConfig"/>, with optional per-key overrides.
	/// </summary>
	/// <typeparam name="TKey">Serializable primitive key (int, string, enum, ...).</typeparam>
	/// <typeparam name="TConfig">Any serializable config class.</typeparam>
	[Serializable]
	public class OverrideableConfig<TKey, TConfig> where TKey : IConvertible where TConfig : class
	{
#if ODIN_INSPECTOR
		private const string TAB_GROUP = "Overrideable Config";
		private const string TAB_DEFAULT = "Default";
		private const string TAB_OVERRIDES = "Overrides";
		private const string TOOLS_DEFAULT = TAB_GROUP + "/" + TAB_DEFAULT + "/Overrideable Config Editor Tools";
		private const string TOOLS_OVERRIDES = TAB_GROUP + "/" + TAB_OVERRIDES + "/Overrideable Config Editor Tools";

		[TabGroup(TAB_GROUP, TAB_DEFAULT)]
		[PropertySpace(SpaceAfter = 12)]
#endif
		[SerializeField] private TConfig defaultConfig;

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_OVERRIDES)]
		[PropertySpace(SpaceAfter = 12)]
#endif
		[SerializeField] private SerializableDictionary<TKey, TConfig> overrides = new();

		/// <summary>The value used whenever a key has no override.</summary>
		public TConfig Default => defaultConfig;

		/// <summary>
		/// Replaces the default value, leaving every override in place. For config loaders that push a
		/// locally loaded fallback in at runtime.
		/// </summary>
		public void SetDefault(TConfig config) => defaultConfig = config;

		public IReadOnlyDictionary<TKey, TConfig> Overrides => overrides;

		/// <summary>Returns the override registered for <paramref name="key"/>, or the default config when there is none.</summary>
		public TConfig Get(TKey key)
		{
			return TryGet(key, out var config) ? config : defaultConfig;
		}

		/// <summary>Returns true (and outputs the override) only when <paramref name="key"/> has a non-null override.</summary>
		public bool TryGet(TKey key, out TConfig config)
		{
			config = null;

			if (key == null || overrides == null)
				return false;

			return overrides.TryGetValue(key, out config) && config != null;
		}

		public bool HasOverride(TKey key) => TryGet(key, out _);

#if UNITY_EDITOR
		//======================================================================
		// Editor authoring - Default tab
		//======================================================================

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_DEFAULT), TitleGroup(TOOLS_DEFAULT), PropertyOrder(100)]
		[MultiLineProperty(4), LabelText("Json")]
		[ShowInInspector]
#endif
		private string _defaultJson = "";

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_DEFAULT), TitleGroup(TOOLS_DEFAULT), PropertyOrder(101)]
		[Button("Import Default From Json", ButtonSizes.Medium)]
#endif
		private void ImportDefaultFromJsonField()
		{
			ImportDefaultFromJson(_defaultJson);
		}

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_DEFAULT), TitleGroup(TOOLS_DEFAULT), PropertyOrder(102)]
		[Button("Copy Default Json To Clipboard")]
#endif
		private void CopyDefaultJsonToClipboard()
		{
			var json = ToJson(defaultConfig);
			GUIUtility.systemCopyBuffer = json;
			Debug.Log($"[OverrideableConfig] Copied default json to clipboard:\n{json}");
		}

		/// <summary>
		/// Replaces the default value with the config parsed from <paramref name="json"/>. Parsed the same way
		/// <see cref="Dessentials.Features.ABTesting.ABTest{T}"/> parses remote config strings (JsonUtility, with
		/// support for top-level arrays/lists), so a value pasted from an AB test works as-is.
		/// </summary>
		public void ImportDefaultFromJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				Debug.LogError("[OverrideableConfig] Import failed: json is empty.");
				return;
			}

			defaultConfig = ConfigFromString(json);
			Debug.Log("[OverrideableConfig] Imported default value.");
		}

		//======================================================================
		// Editor authoring - Overrides tab
		//======================================================================

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_OVERRIDES), TitleGroup(TOOLS_OVERRIDES), PropertyOrder(100)]
		[LabelText("Key"), Tooltip("The dictionary key the buttons below act on.")]
		[ShowInInspector]
#endif
		private TKey _overrideKey;

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_OVERRIDES), TitleGroup(TOOLS_OVERRIDES), PropertyOrder(101)]
		[MultiLineProperty(4), LabelText("Json")]
		[ShowInInspector]
#endif
		private string _overrideJson = "";

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_OVERRIDES), TitleGroup(TOOLS_OVERRIDES), PropertyOrder(102)]
		[Button("Import Override For Key", ButtonSizes.Medium)]
#endif
		private void ImportOverrideForKeyField()
		{
			ImportOverride(_overrideKey, _overrideJson);
		}

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_OVERRIDES), TitleGroup(TOOLS_OVERRIDES), PropertyOrder(103)]
		[Button("Import Many (Json Is A Key To Config Map)")]
#endif
		private void ImportOverridesMapField(bool clearExistingOverrides = false)
		{
			ImportOverridesFromJson(_overrideJson, clearExistingOverrides);
		}

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_OVERRIDES), TitleGroup(TOOLS_OVERRIDES), PropertyOrder(104)]
		[Button("Copy Override Json For Key To Clipboard")]
#endif
		private void CopyOverrideJsonToClipboard()
		{
			if (overrides == null || !overrides.TryGetValue(_overrideKey, out var config))
			{
				Debug.LogError($"[OverrideableConfig] No override registered for key '{_overrideKey}'.");
				return;
			}

			var json = ToJson(config);
			GUIUtility.systemCopyBuffer = json;
			Debug.Log($"[OverrideableConfig] Copied override json for key '{_overrideKey}' to clipboard:\n{json}");
		}

#if ODIN_INSPECTOR
		[TabGroup(TAB_GROUP, TAB_OVERRIDES), TitleGroup(TOOLS_OVERRIDES), PropertyOrder(105)]
		[Button("Remove Override For Key")]
#endif
		private void RemoveOverrideForKey()
		{
			if (overrides == null || !overrides.Remove(_overrideKey))
			{
				Debug.LogError($"[OverrideableConfig] No override registered for key '{_overrideKey}'.");
				return;
			}

			Debug.Log($"[OverrideableConfig] Removed override for key '{_overrideKey}'.");
		}

		/// <summary>
		/// Returns the override registered for <paramref name="key"/>, creating and registering an empty
		/// one when the key has no entry yet. For editor tooling that authors an override in place;
		/// requires <typeparamref name="TConfig"/> to have a public parameterless constructor.
		/// </summary>
		public TConfig GetOrCreateOverride(TKey key)
		{
			if (key == null)
			{
				Debug.LogError("[OverrideableConfig] Cannot create an override for a null key.");
				return null;
			}

			if (TryGet(key, out var existing))
				return existing;

			try
			{
				overrides ??= new SerializableDictionary<TKey, TConfig>();
				var created = Activator.CreateInstance<TConfig>();
				overrides[key] = created;
				return created;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[OverrideableConfig] Could not create a {typeof(TConfig).Name} for key '{key}': it needs a public parameterless constructor.");
				Debug.LogException(ex);
				return null;
			}
		}

		/// <summary>Adds or replaces the override stored under <paramref name="key"/> with the config parsed from <paramref name="json"/>.</summary>
		public void ImportOverride(TKey key, string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				Debug.LogError("[OverrideableConfig] Import failed: json is empty.");
				return;
			}

			if (key == null)
			{
				Debug.LogError("[OverrideableConfig] Import failed: key is null.");
				return;
			}

			overrides ??= new SerializableDictionary<TKey, TConfig>();
			overrides[key] = ConfigFromString(json);
			Debug.Log($"[OverrideableConfig] Imported override for key '{key}'.");
		}

		/// <summary>
		/// Bulk import. <paramref name="json"/> is a json object mapping each key to its config, e.g.
		/// <code>{ "1": { ... }, "2": { ... } }</code>
		/// Existing keys are replaced; keys absent from the json are left alone unless <paramref name="clearExistingOverrides"/> is set.
		/// </summary>
		public void ImportOverridesFromJson(string json, bool clearExistingOverrides = false)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				Debug.LogError("[OverrideableConfig] Import failed: json is empty.");
				return;
			}

			Newtonsoft.Json.Linq.JObject map;
			try
			{
				map = Newtonsoft.Json.Linq.JObject.Parse(json);
			}
			catch (Exception ex)
			{
				Debug.LogError("[OverrideableConfig] Import failed: json is not an object mapping key to config.");
				Debug.LogException(ex);
				return;
			}

			// Assigning a fresh instance rather than calling Clear(): the serialized backing list of
			// SerializableDictionary is only reconciled on serialize, so clearing in place can leave stale entries.
			if (clearExistingOverrides || overrides == null)
			{
				overrides = new SerializableDictionary<TKey, TConfig>();
			}

			int importedCount = 0;

			foreach (var property in map.Properties())
			{
				if (!TryParseKey(property.Name, out var key))
					continue;

				overrides[key] = ConfigFromString(TokenToJson(property.Value));
				importedCount++;
			}

			Debug.Log($"[OverrideableConfig] Imported {importedCount} override(s).");
		}

		//======================================================================
		// Editor authoring - shared plumbing
		//======================================================================

		// A JValue string is handed over unquoted, so a config supplied as an escaped json string
		// (the shape remote config uses) imports the same as an inlined object.
		private static string TokenToJson(Newtonsoft.Json.Linq.JToken token)
		{
			return token.Type == Newtonsoft.Json.Linq.JTokenType.String
				? (string)token
				: token.ToString(Newtonsoft.Json.Formatting.None);
		}

		private static bool TryParseKey(string rawKey, out TKey key)
		{
			key = default;

			try
			{
				var keyType = typeof(TKey);
				key = keyType.IsEnum
					? (TKey)Enum.Parse(keyType, rawKey, true)
					: (TKey)Convert.ChangeType(rawKey, keyType, System.Globalization.CultureInfo.InvariantCulture);
				return true;
			}
			catch (Exception)
			{
				Debug.LogError($"[OverrideableConfig] Skipped override '{rawKey}': not convertible to {typeof(TKey).Name}.");
				return false;
			}
		}

		private static TConfig ConfigFromString(string serializedValue)
		{
			try
			{
				if (typeof(TConfig) == typeof(string))
					return (TConfig)(object)serializedValue;

				// JsonUtility can't deserialize a top-level array/list. When TConfig is a collection,
				// wrap the json under a field whose type is TConfig itself, then unwrap.
				return IsCollectionConfig
					? JsonUtility.FromJson<Wrapper>("{\"value\":" + serializedValue + "}").value
					: JsonUtility.FromJson<TConfig>(serializedValue);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				return default;
			}
		}

		// Round-trips with ConfigFromString: a collection is unwrapped back to a bare top-level array.
		private static string ToJson(TConfig config)
		{
			if (config == null)
				return string.Empty;

			if (config is string rawString)
				return rawString;

			if (!IsCollectionConfig)
				return RoundJsonFloats(JsonUtility.ToJson(config, true));

			var wrapped = Newtonsoft.Json.Linq.JObject.Parse(JsonUtility.ToJson(new Wrapper { value = config }, true));
			var inner = wrapped["value"];
			return RoundJsonFloats(inner == null
				? wrapped.ToString(Newtonsoft.Json.Formatting.Indented)
				: inner.ToString(Newtonsoft.Json.Formatting.Indented));
		}

		private static bool IsCollectionConfig
		{
			get
			{
				var type = typeof(TConfig);
				return type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
			}
		}

		private const int FLOAT_ROUND_DIGITS = 6;

		// JsonUtility.ToJson doesn't support custom converters, so round the numeric literals in the
		// produced json. Only touches tokens with a decimal point, leaving integers untouched.
		// Without this, float->double widening leaks tails like 0.30000001192092896.
		private static string RoundJsonFloats(string json)
		{
			if (string.IsNullOrEmpty(json))
				return json;

			return System.Text.RegularExpressions.Regex.Replace(
				json,
				@"-?\d+\.\d+(?:[eE][-+]?\d+)?",
				match =>
				{
					var rounded = Math.Round(
						double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture),
						FLOAT_ROUND_DIGITS);
					return rounded.ToString(System.Globalization.CultureInfo.InvariantCulture);
				});
		}

		[Serializable]
		private class Wrapper
		{
			public TConfig value;
		}
#endif
	}
}
