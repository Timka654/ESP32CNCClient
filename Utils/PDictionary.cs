using System;
using System.Collections;
using System.Text;

namespace NFGCodeESP32Client.Utils
{
    /// <summary>
    /// Very simple dictionary
    /// </summary>
    public class PDictionary/*<TKey, TValue>*/
    {
        private ArrayList KeysCollection = new ArrayList();

        private ArrayList ValuesCollection = new ArrayList();

        public object[] Keys => KeysCollection.ToArray();

        public object[] Values => ValuesCollection.ToArray();

        public object this[object key]
        {
            get
            {
                var idx = FindIdx(key);

                if (idx == -1)
                    throw new Exception($"Key {key} no exists in collection");

                return (object)ValuesCollection[idx];
            }
            set => AddOrUpdate(key, value);
        }

        public int Count => KeysCollection.Count;

        public bool TryGetValue(object key, out object value)
        {
            if (ContainsKey(key))
            {
                value = this[key];
                return true;
            }

            value = default;

            return false;
        }

        public bool ContainsKey(object key)
            => FindIdx(key) != -1;

        public void Add(object key, object value)
        {
            var keyIndex = FindIdx(key);

            if (keyIndex == -1)
            {
                Append(key, value);
                return;
            }

            throw new Exception($"Key {key} alreadyExists");
        }

        public void AddOrUpdate(object key, object value)
        {
            var keyIndex = FindIdx(key);

            if (keyIndex == -1)
            {
                Append(key, value);
                return;
            }

            Set(keyIndex, value);
        }

        public void Remove(object key)
        {
            var keyIndex = FindIdx(key);

            if (keyIndex == -1)
                return;

            KeysCollection.RemoveAt(keyIndex);
            ValuesCollection.RemoveAt(keyIndex);
        }

        public void Clear()
        {
            KeysCollection.Clear();
            ValuesCollection.Clear();
        }

        private int FindIdx(object key)
            => KeysCollection.IndexOf(key);
        private void Set(int keyIdx, object value)
        {
            ValuesCollection[keyIdx] = value;
        }

        private void Append(object key, object value)
        {
            KeysCollection.Add(key);
            ValuesCollection.Add(value);
        }
    }
}
