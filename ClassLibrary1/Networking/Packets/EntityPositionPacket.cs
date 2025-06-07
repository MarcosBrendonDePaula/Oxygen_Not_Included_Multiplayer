using System.IO;
using ONI_MP.DebugTools;
using UnityEngine;

namespace ONI_MP.Networking.Packets
{
    public class EntityPositionPacket : IPacket
    {
        // TODO Later update this to use prediction, deadreckoning etc to smoothly interpolate position instead of hard setting it
        public int NetId;
        public Vector3 Position;

        public PacketType Type => PacketType.EntityPosition;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetId);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetId = reader.ReadInt32();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            Position = new Vector3(x, y, z);
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost)
            {
                return;
            }

            if (NetEntityRegistry.TryGet(NetId, out var entity))
            {
                entity.transform.SetPosition(Position);
                DebugConsole.Log($"[Packets/EntityPosition] Entity {NetId} moved to {Position}");
            }
            else
            {
                DebugConsole.LogWarning($"[Packets] Could not find entity with NetId {NetId}");
            }
            
        }

    }
}
