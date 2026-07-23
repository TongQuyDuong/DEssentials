using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace Dessentials.Serializables
{
    public static class SerializationExtensions
    {
#if ODIN_INSPECTOR
        public static ValueDropdownList<string> ValueDropdownListFromEnum<TEnum>()
            where TEnum : struct, Enum
        {
            var result = new ValueDropdownList<string>();

            if (!typeof(TEnum).IsEnum)
            {
                Debug.LogError($"{typeof(TEnum).Name} is not an Enum");
                return result;
            }

            var allEnums = Enum.GetNames(typeof(TEnum));

            foreach (var val in allEnums)
            {
                result.Add(val, val);
            }
            
            return result;
        }
#endif
    }
    
    [Serializable]
    public struct EnumTypedGameObject<TEnum> : IEquatable<EnumTypedGameObject<TEnum>> where TEnum : struct, Enum
    {
        public TEnum type;
        public GameObject gameObject;

        public bool Equals(EnumTypedGameObject<TEnum> other)
        {
            return EqualityComparer<TEnum>.Default.Equals(type, other.type) && Equals(gameObject, other.gameObject);
        }

        public override bool Equals(object obj)
        {
            return obj is EnumTypedGameObject<TEnum> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(type, gameObject);
        }
    }

    [Serializable]
    public struct EnumTypedObject<TEnum, TObject> : IEquatable<EnumTypedObject<TEnum, TObject>> where TEnum : Enum
    {
        public TEnum type;
        public TObject pairedObject;

        public bool Equals(EnumTypedObject<TEnum, TObject> other)
        {
            return EqualityComparer<TEnum>.Default.Equals(type, other.type) && EqualityComparer<TObject>.Default.Equals(pairedObject, other.pairedObject);
        }

        public override bool Equals(object obj)
        {
            return obj is EnumTypedObject<TEnum, TObject> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(type, pairedObject);
        }
    }
    
    [Serializable]
    public struct ValueTypedGameObject<TValue> where TValue : struct
    {
        public TValue value;
        public GameObject gameObject;
    }
    
    [Serializable]
    public struct StringTypedObject<TObject>
    {
        public string key;
        public TObject pairedObject;
    }
    
    [Serializable]
    public class StringSerializedEnum<TEnum>
        where TEnum : struct, Enum
    {
        [HideInInspector]
        public string serializedValue = "None";

        public TEnum Value
        {
            get
            {
                if (Enum.TryParse<TEnum>(serializedValue, out var result))
                    return result;

                Debug.LogError($"Failed to get Enum {nameof(TEnum)} : {serializedValue}");

                var val = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Min();

                serializedValue = val.ToString();

                return val;
            }

            set => serializedValue = value.ToString();
        }
    }
}

#if UNITY_EDITOR
namespace Dessentials.Serializables
{
    using UnityEditor;

    [CustomPropertyDrawer(typeof(StringSerializedEnum<>))]
    public class StringSerializedEnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var serializedValueProp = property.FindPropertyRelative("serializedValue");

            var enumType = fieldInfo.FieldType;
            if (enumType.IsGenericType)
                enumType = enumType.GetGenericArguments()[0];

            Enum currentValue;
            if (!string.IsNullOrEmpty(serializedValueProp.stringValue)
                && Enum.IsDefined(enumType, serializedValueProp.stringValue))
            {
                currentValue = (Enum)Enum.Parse(enumType, serializedValueProp.stringValue);
            }
            else
            {
                currentValue = (Enum)Enum.GetValues(enumType).GetValue(0);
            }

            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUI.EnumPopup(position, label, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                serializedValueProp.stringValue = newValue.ToString();
            }
        }
    }
}
#endif
