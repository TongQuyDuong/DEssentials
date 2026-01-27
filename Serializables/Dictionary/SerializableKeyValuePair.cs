/***
 * Author HNB-RaBear - 2024
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Dessentials.Serializables
{
    [Serializable]
    public class SerializableKeyValue<TKey, TValue>
    {
#if ODIN_INSPECTOR
        [PreviewField]
#endif
        public TKey k;
#if ODIN_INSPECTOR
        [PreviewField]
#endif
        public TValue v;
        [JsonIgnore] public TKey Key => k;
        [JsonIgnore] public TValue Value { get => v; set => v = value; }
        public SerializableKeyValue() { }
        public SerializableKeyValue(TKey pKey, TValue pValue)
        {
            k = pKey;
            v = pValue;
        }
        public static implicit operator SerializableKeyValue<TKey, TValue>(KeyValuePair<TKey, TValue> kvp)
            => new(kvp.Key, kvp.Value);
        public static implicit operator KeyValuePair<TKey, TValue>(SerializableKeyValue<TKey, TValue> kvp)
            => new(kvp.k, kvp.v);
    }
}