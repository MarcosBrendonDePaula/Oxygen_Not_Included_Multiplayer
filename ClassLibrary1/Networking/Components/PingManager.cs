using System;
using UnityEngine;
using ONI_MP.Networking.Packets;
using Steamworks;

namespace ONI_MP.Networking.Components
{
    public class PingManager : MonoBehaviour
    {
        private const float PingInterval = 2f; // seconds
        private float pingTimer;

        private void Update()
        {
            if (!MultiplayerSession.IsClient)
                return;

            pingTimer += Time.unscaledDeltaTime;

            if (pingTimer >= PingInterval)
            {
                pingTimer = 0f;

                var packet = new PingPacket
                {
                    Timestamp = System.DateTime.UtcNow.Ticks
                };

                PacketSender.SendToPlayer(MultiplayerSession.HostSteamID, packet);
            }
        }

        public static void Attach()
        {
            if (MultiplayerSession.IsClient && FindObjectOfType<PingManager>() == null)
            {
                var go = new GameObject("PingManager");
                DontDestroyOnLoad(go);
                go.AddComponent<PingManager>();
            }
        }
    }
}
