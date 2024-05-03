using System;
using System.Collections.Generic;

namespace ISoftViewerQCSystem.utils
{
    public class FactoryTool<K, V> : Dictionary<K, V>, IDisposable
    {
        ///<summary>建構</summary>
        public FactoryTool()
        {
            this.Clear();
        }

        ///<summary>釋放</summary>
        public void Dispose()
        {
            this.Clear();
            GC.SuppressFinalize(this);
        }

        ///<summary>註冊產品</summary>
        public void registerProduct(K key, V value)
        {
            if (!this.ContainsKey(key))
                this.Add(key, value);
        }

        ///<summary>產生產品</summary>
        public V createProduct(K key)
        {
            if (ContainsKey(key))
                return this[key];
            return default(V);
        }
    }
}