using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using UnityEngine;

namespace ONI_MP.Networking
{
    public static class NetEntityRegistry
    {
        private static readonly Dictionary<int, NetworkIdentity> entities = new Dictionary<int, NetworkIdentity>();
        private static readonly System.Random rng = new System.Random();

        public static int Register(NetworkIdentity entity)
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

        public static void RegisterExisting(NetworkIdentity entity, int netId)
        {
            if (!entities.ContainsKey(netId))
            {
                entities[netId] = entity;
                DebugConsole.Log($"[NetEntityRegistry] Registered existing entity with net id: {netId}");
            }
            else
            {
                DebugConsole.LogWarning($"[NetEntityRegistry] NetId {netId} already registered. Skipping duplicate registration.");
            }
        }

        public static void RegisterOverride(NetworkIdentity entity, int netId)
        {
            if (entities.ContainsKey(netId))
            {
                DebugConsole.LogWarning($"[NetEntityRegistry] Overwriting existing entity for NetId {netId}");
                entities[netId] = entity;
            }
            else
            {
                entities.Add(netId, entity);
                DebugConsole.Log($"[NetEntityRegistry] Registered overridden NetId {netId} for {entity.name}");
            }
        }



        public static bool TryGet(int netId, out NetworkIdentity entity)
        {
            return entities.TryGetValue(netId, out entity);
        }

        public static void Clear()
        {
            entities.Clear();
        }
    }
}
