using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking;
using System.IO;
using UnityEngine;

public class EntityPositionPacket : IPacket
{
    public int NetId;
    public Vector3 Position;
    public bool FacingLeft;  // ← new field!

    public PacketType Type => PacketType.EntityPosition;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NetId);
        writer.Write(Position.x);
        writer.Write(Position.y);
        writer.Write(Position.z);
        writer.Write(FacingLeft);
    }

    public void Deserialize(BinaryReader reader)
    {
        NetId = reader.ReadInt32();
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        Position = new Vector3(x, y, z);
        FacingLeft = reader.ReadBoolean();
    }

    public void OnDispatched()
    {
        if (MultiplayerSession.IsHost) return;

        if (NetEntityRegistry.TryGet(NetId, out var entity))
        {
            entity.transform.position = Position;

            // Mirror on X-axis based on facing direction
            Vector3 ls = entity.transform.localScale;
            ls.x = Mathf.Abs(ls.x) * (FacingLeft ? -1f : 1f);
            entity.transform.localScale = ls;
        }
        else
        {
            DebugConsole.LogWarning($"[Packets] Could not find entity with NetId {NetId}");
        }
    }
}
