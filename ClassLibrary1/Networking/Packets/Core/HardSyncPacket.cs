using System.Collections;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Core
{
    public class HardSyncPacket : IPacket
    {
        public PacketType Type => PacketType.HardSync;

        public void Serialize(BinaryWriter writer)
        {
            // No payload needed
        }

        public void Deserialize(BinaryReader reader)
        {
            // No payload needed
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost)
                return;

            Sync();
            //PauseScreen.TriggerQuitGame();
        }

        public static void Sync()
        {
            GameClient.IsHardSyncInProgress = true;
            MultiplayerOverlay.Show("Hard sync in progress!");

            // This is incredibly stupid...
            GameClient.CacheCurrentServer();
            GameClient.Disconnect();

            PauseScreen.TriggerQuitGame(); // Force exit to frontend

            MultiplayerOverlay.Show("Hard sync in process!");
            NetworkIdentityRegistry.Clear();
            GameClient.ReconnectFromCache();
        }
    }
}
