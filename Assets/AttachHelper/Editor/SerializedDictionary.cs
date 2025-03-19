using System.Collections.Generic;
using UnityEngine;

namespace AttachHelper.Editor
{
    [System.Serializable]
    public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [System.Serializable]
        internal class KeyValue
        {
            public TKey key;
            public TValue value;

            public KeyValue(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }
        [SerializeField] List<KeyValue> m_list;

        public TKey DefaultKey => default;
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();
            foreach (var item in m_list)
            {
                this[ContainsKey(item.key) ? DefaultKey : item.key] = item.value;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_list = new List<KeyValue>(Count);
            foreach (var keyValuePair in this)
            {
                m_list.Add(new KeyValue(keyValuePair.Key, keyValuePair.Value));
            }
        }
    }
}