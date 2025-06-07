using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Patches.Chores;
using UnityEngine;

namespace ONI_MP.Networking.Packets
{
    public class ChoreAssignmentPacket : IPacket
    {
        public int NetId;
        public string ChoreId;

        public PacketType Type => PacketType.ChoreAssignment;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetId);
            writer.Write(ChoreId ?? string.Empty);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetId = reader.ReadInt32();
            ChoreId = reader.ReadString(); // 🔧 FIXED: reading string, not int
        }

        public void OnDispatched()
        {
            // Host doesn't need to do this
            if (MultiplayerSession.IsHost)
                return;
            
            if (!NetEntityRegistry.TryGet(NetId, out var netEntity))
            {
                DebugConsole.LogWarning($"[ChoreAssignment] Could not find entity with NetId {NetId}");
                return;
            }

            GameObject dupeGO = netEntity.gameObject;
            if (dupeGO == null)
            {
                DebugConsole.LogWarning($"[ChoreAssignment] GameObject is null for NetId {NetId}");
                return;
            }

            ChoreConsumer consumer = dupeGO.GetComponent<ChoreConsumer>();
            if (consumer == null)
            {
                DebugConsole.LogWarning($"[ChoreAssignment] No ChoreConsumer found on {dupeGO.name}");
                return;
            }

            int worldId = dupeGO.GetMyParentWorldId();
            List<ChoreProvider> providers = Traverse.Create(consumer).Field("providers").GetValue<List<ChoreProvider>>();

            foreach (var provider in providers)
            {
                if (provider == null) continue;

                if (!provider.choreWorldMap.TryGetValue(worldId, out var choreList))
                    continue;

                foreach (var chore in choreList)
                {
                    if (chore != null && chore.choreType.Id == ChoreId)
                    {
                        chore.AssignChoreToDuplicant(dupeGO);
                        DebugConsole.Log($"[ChoreAssignment] Assigned chore '{ChoreId}' to {dupeGO.name}");
                        return;
                    }
                }
            }

            DebugConsole.LogWarning($"[ChoreAssignment] Chore with ID '{ChoreId}' not found for duplicant {dupeGO.name}");
        }
    }
}
