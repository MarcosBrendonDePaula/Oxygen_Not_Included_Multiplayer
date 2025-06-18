using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Clear
{
    public class ClearPacket : IPacket
    {
        public PacketType Type => PacketType.Clear;

        public List<int> TargetCells = new List<int>();
        public CSteamID SenderId;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TargetCells.Count);
            foreach (var cell in TargetCells)
                writer.Write(cell);

            writer.Write(SenderId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            TargetCells = new List<int>(count);
            for (int i = 0; i < count; i++)
                TargetCells.Add(reader.ReadInt32());

            SenderId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            foreach (int cell in TargetCells)
            {
                if (!Grid.IsValidCell(cell)) continue;

                for (int i = 0; i < 45; i++)
                {
                    GameObject go = Grid.Objects[cell, i];
                    if (go == null) continue;

                    TryMarkClearable(go);
                }
            }

            void TryMarkClearable(GameObject target)
            {
                if (target.TryGetComponent(out Clearable clearable))
                {
                    clearable.MarkForClear();
                    DebugConsole.Log($"[ClearPacket] Marked {target.name} at cell for sweeping");
                }

                if (target.TryGetComponent(out Pickupable pickup))
                {
                    ObjectLayerListItem item = pickup.objectLayerListItem;
                    while (item != null)
                    {
                        GameObject g2 = item.gameObject;
                        item = item.nextItem;

                        if (g2 == null) continue;

                        if (g2.TryGetComponent(out Clearable subClearable))
                        {
                            subClearable.MarkForClear();
                            DebugConsole.Log($"[ClearPacket] Marked stacked item {g2.name} for sweeping");
                        }
                    }
                }
            }

            if (MultiplayerSession.IsHost)
            {
                var exclude = new HashSet<CSteamID>
                {
                    SenderId,
                    MultiplayerSession.LocalSteamID
                };

                PacketSender.SendToAllExcluding(this, exclude);
                DebugConsole.Log($"[ClearPacket] Rebroadcasted to clients (excluding sender {SenderId}) for {TargetCells.Count} cell(s)");
            }
        }
    }
}
