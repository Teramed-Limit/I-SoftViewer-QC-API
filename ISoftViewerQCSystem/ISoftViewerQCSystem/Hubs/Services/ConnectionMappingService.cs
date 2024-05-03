using System.Collections.Generic;
using System.Linq;

namespace ISoftViewerQCSystem.Hubs.Services
{
    public class ConnectionMapping<T>
    {
        private readonly Dictionary<T, HashSet<string>> _connections = new ();

        public int Count => _connections.Count;

        public void Add(T key, string connectionId)
        {
            lock (_connections)
            {
                if (!_connections.TryGetValue(key, out var connections))
                {
                    connections = new HashSet<string>();
                    _connections.Add(key, connections);
                }

                lock (connections)
                {
                    connections.Add(connectionId);
                }
            }
        }
        
        public void Remove(T key, string connectionId)
        {
            lock (_connections)
            {
                if (!_connections.TryGetValue(key, out var connections))
                {
                    return;
                }

                lock (connections)
                {
                    connections.Remove(connectionId);

                    if (connections.Count == 0)
                    {
                        _connections.Remove(key);
                    }
                }
            }
        }
        
        public IEnumerable<string> GetUserConnectionIdList(T key)
        {
            if (_connections.TryGetValue(key, out var connections))
            {
                return connections;
            }

            return Enumerable.Empty<string>();
        }
        
        public List<T> GetAllUser()
        {
            return _connections.Keys.ToList();
        }
    }
}