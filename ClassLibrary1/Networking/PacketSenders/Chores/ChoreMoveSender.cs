using System;
using UnityEngine;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Chores;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;

namespace ONI_MP.Networking.PacketSenders.Chores
{
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
            DebugConsole.Log("ChoreMoveSender ready!");
        }

        private void Update()
        {
            if (choreDriver == null || networkedEntity == null)
                return;

            if (MultiplayerSession.IsClient)
            {
                return;
            }

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
                DebugConsole.Log($"Sent chore move packet from {name}");
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
