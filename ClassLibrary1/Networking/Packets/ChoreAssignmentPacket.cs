using ONI_MP.DebugTools;
using ONI_MP.Networking;
using System.IO;
using UnityEngine;

public class ChoreAssignmentPacket : IPacket
{
    public int NetId;
    public string ChoreTypeId;

    public Vector3 TargetPosition;
    public int TargetCell = -1;
    public string TargetPrefabId; // optional

    public PacketType Type => PacketType.ChoreAssignment;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NetId);
        writer.Write(ChoreTypeId ?? string.Empty);
        writer.Write(TargetPosition.x);
        writer.Write(TargetPosition.y);
        writer.Write(TargetPosition.z);
        writer.Write(TargetCell);
        writer.Write(TargetPrefabId ?? string.Empty);
    }

    public void Deserialize(BinaryReader reader)
    {
        NetId = reader.ReadInt32();
        ChoreTypeId = reader.ReadString();
        TargetPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        TargetCell = reader.ReadInt32();
        TargetPrefabId = reader.ReadString();
    }

    public void OnDispatched()
    {
        var chore = ChoreFactory.Create(ChoreTypeId, dupeGO, TargetPosition, TargetCell, TargetPrefabId);
        if (chore != null)
        {
            chore.AssignChoreToDuplicant(dupeGO);
        }
        else
        {
            DebugConsole.LogWarning($"[ChoreAssignment] Could not create chore: {ChoreTypeId} for {dupeGO.name}");
        }

    }
}
