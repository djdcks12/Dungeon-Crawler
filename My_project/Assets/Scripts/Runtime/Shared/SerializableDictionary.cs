using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Unity Inspector에서 직렬화 가능한 딕셔너리
    /// </summary>
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();
        
        /// <summary>
        /// 직렬화 전 호출
        /// </summary>
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }
        
        /// <summary>
        /// 직렬화 후 호출
        /// </summary>
        public void OnAfterDeserialize()
        {
            Clear();
            
            if (keys.Count != values.Count)
            {
                throw new Exception($"SerializableDictionary keys and values count mismatch: {keys.Count} keys, {values.Count} values");
            }
            
            for (int i = 0; i < keys.Count; i++)
            {
                this[keys[i]] = values[i];
            }
        }
        
        /// <summary>
        /// 기본 생성자
        /// </summary>
        public SerializableDictionary() : base() { }
        
        /// <summary>
        /// 용량 지정 생성자
        /// </summary>
        public SerializableDictionary(int capacity) : base(capacity) { }
        
        /// <summary>
        /// 딕셔너리 복사 생성자
        /// </summary>
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
    }
}