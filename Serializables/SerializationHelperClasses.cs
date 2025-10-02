using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dessentials.Serializables
{
    [Serializable]
    public struct EnumTypedGameObject<TEnum> where TEnum : Enum
    {
        public TEnum type;
        public GameObject gameObject;
    }

    [Serializable]
    public struct EnumTypedObject<TEnum, TObject> where TEnum : Enum
    {
        public TEnum type;
        public TObject pairedObject;
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
}
