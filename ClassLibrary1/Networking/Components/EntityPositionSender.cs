using System;
using UnityEngine;
using ONI_MP.Networking.Packets;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Components
{
    public class EntityPositionSender : KMonoBehaviour
    {
        private Vector3 lastSentPosition;
        private float timer;
        private const float SendInterval = 0.1f; // 100ms

        private NetworkIdentity networkedEntity;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            networkedEntity = GetComponent<NetworkIdentity>();
            if (networkedEntity == null)
            {
                DebugConsole.LogWarning("[EntityPositionSender] Missing NetworkedEntityComponent. This component requires it to function.");
            }

            lastSentPosition = transform.position;
        }

        private void Update()
        {
            // Block clients from sending position data
            if (MultiplayerSession.IsClient || networkedEntity == null)
                return;

            // Only send when in a session and host
            if (!MultiplayerSession.InSession)
            {
                return;
            }

            timer += Time.unscaledDeltaTime;
            if (timer < SendInterval)
                return;

            timer = 0f;

            Vector3 currentPosition = transform.position;
            if (Vector3.Distance(currentPosition, lastSentPosition) > 0.01f)
            {
                lastSentPosition = currentPosition;

                var packet = new EntityPositionPacket
                {
                    NetId = networkedEntity.NetId,
                    Position = currentPosition
                };

                PacketSender.SendToAll(packet, sendType: SteamNetworkingSend.Unreliable);
                //DebugConsole.Log($"[EntityPositionSender] Sent position packet for entity {networkedEntity.NetId}");
            }
        }
    }
}
