using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using Steamworks;

namespace ONI_MP.Networking.Packets.Core
{
    class ClientReadyStatusPacket : IPacket
    {
        public PacketType Type => PacketType.ClientReadyStatus;

        private CSteamID SenderId;
        private ClientReadyState Status = ClientReadyState.Unready;

        public ClientReadyStatusPacket() { }

        public ClientReadyStatusPacket(CSteamID senderId, ClientReadyState status)
        {
            SenderId = senderId;
            Status = status;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((int)Status);
            writer.Write(SenderId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            Status = (ClientReadyState)reader.ReadInt32();
            SenderId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (!MultiplayerSession.IsHost)
            {
                DebugConsole.LogWarning("[ClientReadyStatusPacket] Received on client — ignoring.");
                return;
            }

            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(SenderId, out var player))
            {
                DebugConsole.LogWarning($"[ClientReadyStatusPacket] Unknown sender: {SenderId}");
                return;
            }

            player.readyState = Status;
            DebugConsole.Log($"[ClientReadyStatusPacket] {player} marked as {Status}");

            // Build message string for overlay
            string message = "Waiting for players to be ready!\n";
            MultiplayerOverlay.Show(message);
            bool allReady = true;
            foreach (var p in MultiplayerSession.AllPlayers)
            {
                MultiplayerOverlay.Show(message);
                if (p.SteamID == MultiplayerSession.HostSteamID)
                    continue;

                message += $"{p.SteamName} : {p.readyState}\n";

                if (p.readyState != ClientReadyState.Ready)
                    allReady = false;
            }


            if (allReady)
            {
                CoroutineRunner.RunOne(DelayAllReadyBroadcast());
            } else
            {
                // Broadcast updated overlay message to all clients
                PacketSender.SendToAllClients(new ClientReadyStatusUpdatePacket(message));
            }
        }

        private System.Collections.IEnumerator DelayAllReadyBroadcast()
        {
            yield return new UnityEngine.WaitForSeconds(1f);
            PacketSender.SendToAllClients(new AllClientsReadyPacket());
            AllClientsReadyPacket.ProcessAllReady(); // Host transitions after delay
        }

    }
}
