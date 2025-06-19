using System.Collections;
using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.States;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking
{
    public static class GameServerHardSync
    {
        private static readonly Queue<CSteamID> hardSyncQueue = new Queue<CSteamID>();
        private static bool hardSyncInProgress = false;

        public static bool IsInProgress => hardSyncInProgress;

        public static void PerformHardSync()
        {
            if (hardSyncInProgress)
            {
                DebugConsole.Log("[HardSync] A hard sync is already in progress.");
                return;
            }

            SpeedControlScreen.Instance?.Pause(false); // Pause the game
            MultiplayerOverlay.Show("Hard sync in progress!");
            hardSyncQueue.Clear();

            foreach (var kvp in MultiplayerSession.ConnectedPlayers)
            {
                var client = kvp.Value;

                if (client.IsLocal) // This will return the host because this always runs on the host
                {
                    client.readyState = ClientReadyState.Ready; // Host is always ready
                    continue;
                }
                else
                {
                    client.readyState = ClientReadyState.Unready;
                }

                if (client.Connection != null)
                {
                    hardSyncQueue.Enqueue(client.SteamID);
                }
            }

            if (hardSyncQueue.Count == 0)
            {
                DebugConsole.Log("[HardSync] No connected clients to sync.");
                MultiplayerOverlay.Close();
                return;
            }

            var packet = new HardSyncPacket();
            PacketSender.SendToAllClients(packet);

            DebugConsole.Log($"[HardSync] Starting hard sync for {hardSyncQueue.Count} client(s)...");
            CoroutineRunner.RunOne(HardSyncCoroutine());
        }

        private static IEnumerator HardSyncCoroutine()
        {
            hardSyncInProgress = true;
            yield return new WaitForSeconds(1f);

            while (hardSyncQueue.Count > 0)
            {
                CSteamID clientID = hardSyncQueue.Dequeue();

                if (!MultiplayerSession.ConnectedPlayers.TryGetValue(clientID, out var player) || player.Connection == null)
                {
                    DebugConsole.LogWarning($"[HardSync] Skipping {clientID} (disconnected)");
                    continue;
                }

                DebugConsole.Log($"[HardSync] Sending save file to {clientID}");
                SaveFileRequestPacket.SendSaveFile(clientID);

                int fileSize = SaveHelper.GetWorldSave().Length;
                int chunkSize = SaveHelper.SAVEFILE_CHUNKSIZE_KB * 1024;
                int chunkCount = Mathf.CeilToInt(fileSize / (float)chunkSize);
                float estimatedTransferDuration = chunkCount * SaveFileRequestPacket.SAVE_DATA_SEND_DELAY;

                yield return new WaitForSeconds(estimatedTransferDuration);
            }

            DebugConsole.Log("[HardSync] Hard sync completed for all clients.");
            var packet = new HardSyncCompletePacket();
            PacketSender.SendToAllClients(packet);

            hardSyncInProgress = false;
            SpeedControlScreen.Instance?.Unpause(false);
            MultiplayerOverlay.Close();
        }

        public static void SyncSingleClient(CSteamID clientID)
        {
            CoroutineRunner.RunOne(SyncClientCoroutine(clientID));
        }

        private static IEnumerator SyncClientCoroutine(CSteamID clientID)
        {
            DebugConsole.Log($"[HardSync] Queued individual sync for {clientID}");

            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(clientID, out var player) || player.Connection == null)
            {
                DebugConsole.LogWarning($"[HardSync] Skipping {clientID} (not connected)");
                yield break;
            }

            PacketSender.SendToPlayer(clientID, new HardSyncPacket());
            yield return new WaitForSeconds(1f);

            SaveFileRequestPacket.SendSaveFile(clientID);

            int fileSize = SaveHelper.GetWorldSave().Length;
            int chunkSize = SaveHelper.SAVEFILE_CHUNKSIZE_KB * 1024;
            int chunkCount = Mathf.CeilToInt(fileSize / (float)chunkSize);
            float estimatedTransferDuration = chunkCount * SaveFileRequestPacket.SAVE_DATA_SEND_DELAY;
            yield return new WaitForSeconds(estimatedTransferDuration);

            DebugConsole.Log($"[HardSync] Individual sync completed for {clientID}");
        }
    }
}
