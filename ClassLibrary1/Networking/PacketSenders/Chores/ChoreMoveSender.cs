using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Chores;

namespace ONI_MP.Networking.PacketSenders.Chores
{
    public class ChoreMoveSender : MonoBehaviour
    {
        // Only add this to an entity if we're the host
        public int netId;

        private int lastSentCell = -1;

        private ChoreDriver choreDriver;
        private void Start()
        {
            choreDriver = gameObject.GetComponent<ChoreDriver>();
        }

        private void Update()
        {
            if (choreDriver == null)
            {
                return;
            }

            int currentTargetCell = GetTargetCell();

            if (currentTargetCell != -1 && currentTargetCell != lastSentCell)
            {
                lastSentCell = currentTargetCell;

                var packet = new ChoreMovePacket
                {
                    NetId = netId,
                    TargetCell = currentTargetCell
                };

                PacketSender.SendToAll(packet);
            }
        }

        private int GetTargetCell()
        {
            // Get the ChoreDriver on this GameObject
            if (choreDriver == null)
                return -1;

            // Ensure the current chore is a MoveChore
            if (choreDriver.GetCurrentChore() is MoveChore moveChore)
            {
                var smi = moveChore.smi;
                if (smi != null && smi.getCellCallback != null)
                {
                    return smi.getCellCallback(smi); // This gives the destination cell
                }
            }

            return -1;
        }


    }

}
