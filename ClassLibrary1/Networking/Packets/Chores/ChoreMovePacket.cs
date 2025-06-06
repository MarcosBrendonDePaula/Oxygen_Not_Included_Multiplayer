using System.IO;
using ONI_MP.DebugTools;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Chores
{
    public class ChoreMovePacket : IPacket
    {
        public PacketType Type => PacketType.ChoreMove;

        public int NetId;
        public int TargetCell;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetId);
            writer.Write(TargetCell);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetId = reader.ReadInt32();
            TargetCell = reader.ReadInt32();
        }

        public void OnDispatched()
        {
            if (!NetEntityRegistry.TryGet(NetId, out var entity))
            {
                Debug.LogWarning($"ChoreMovePacket: NetEntity {NetId} not found.");
                return;
            }

            var go = entity.gameObject;
            if (go == null)
            {
                Debug.LogWarning("ChoreMovePacket: GameObject is null.");
                return;
            }

            new MoveChore(
                go.GetComponent<IStateMachineTarget>(),
                Db.Get().ChoreTypes.MoveTo,
                smi => TargetCell
            );
        }
    }
}
