using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Networking
{
    public static class NetEntityRegistry
    {
        private static readonly Dictionary<int, NetworkedEntityComponent> entities = new Dictionary<int, NetworkedEntityComponent>();
        private static readonly System.Random rng = new System.Random();

        public static int Register(NetworkedEntityComponent entity)
        {
            int id;
            do
            {
                id = rng.Next(100000, int.MaxValue); // Avoid very low IDs
            } while (entities.ContainsKey(id));

            entities[id] = entity;
            return id;
        }

        public static void Unregister(int netId)
        {
            entities.Remove(netId);
        }

        public static bool TryGet(int netId, out NetworkedEntityComponent entity)
        {
            return entities.TryGetValue(netId, out entity);
        }

        public static void Clear()
        {
            entities.Clear();
        }
    }
}
