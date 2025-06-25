using System.Collections;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
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

            // Cache current server info before disconnecting
            GameClient.CacheCurrentServer();
            
            // Disconnect gracefully
            GameClient.Disconnect();
            
            // Clear network registry to reset state
            NetworkIdentityRegistry.Clear();
            
            // Start reconnection process after a brief delay
            CoroutineRunner.RunOne(ReconnectAfterDelay());
        }
        
        private static IEnumerator ReconnectAfterDelay()
        {
            // Wait a moment for disconnect to complete
            yield return new WaitForSeconds(0.5f);
            
            MultiplayerOverlay.Show("Reconnecting after hard sync...");
            DebugConsole.Log("[HardSyncPacket] Starting reconnection after hard sync");
            GameClient.ReconnectFromCache();
            
            // Wait a bit more and then check if we need to force packet processing
            yield return new WaitForSeconds(1f);
            
            if (GameClient.State == ClientState.InGame && !PacketHandler.readyToProcess)
            {
                DebugConsole.LogWarning("[HardSyncPacket] Forcing packet processing after hard sync");
                PacketHandler.readyToProcess = true;
            }
        }
    }
}
