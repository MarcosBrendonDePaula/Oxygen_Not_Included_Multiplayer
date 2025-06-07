using System;
using UnityEngine;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Chores;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.PacketSenders.Chores
{
    // This component should only be attached if the local player is the host.
    public class ChoreMoveSender : KMonoBehaviour
    {
        private int lastSentCell = -1;

        private ChoreDriver choreDriver;
        private NetworkedEntityComponent networkedEntity;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            choreDriver = GetComponent<ChoreDriver>();
            networkedEntity = GetComponent<NetworkedEntityComponent>();

            if (networkedEntity == null)
            {
                DebugConsole.LogWarning("[ChoreMoveSender] Missing NetworkedEntityComponent. This component requires it to function.");
            }
        }

        private void Update()
        {
            if (choreDriver == null || networkedEntity == null)
                return;

            int targetCell = GetTargetCell();

            if (targetCell != -1 && targetCell != lastSentCell)
            {
                lastSentCell = targetCell;

                var packet = new ChoreMovePacket
                {
                    NetId = networkedEntity.NetId,
                    TargetCell = targetCell
                };

                PacketSender.SendToAll(packet);
            }
        }

        private int GetTargetCell()
        {
            if (choreDriver.GetCurrentChore() is MoveChore moveChore)
            {
                var smi = moveChore.smi;
                if (smi?.getCellCallback != null)
                {
                    return smi.getCellCallback(smi);
                }
            }

            return -1;
        }
    }
}
