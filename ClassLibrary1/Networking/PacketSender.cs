using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;
using ONI_MP.DebugTools;
using ONI_MP.Misc;

namespace ONI_MP.Networking
{

    public enum SteamNetworkingSend
    {
        Unreliable = 0,
        NoNagle = 2,
        UnreliableNoNagle = Unreliable | NoNagle, // 2
        Reliable = 1,
        ReliableNoNagle = Reliable | NoNagle      // 3
    }

    public static class PacketSender
    {
        public static int MAX_PACKET_SIZE_RELIABLE = 512;
        public static int MAX_PACKET_SIZE_UNRELIABLE = 1024;

        public static byte[] SerializePacket(IPacket packet)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write((byte)packet.Type);
                packet.Serialize(writer);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Send to one connection by HSteamNetConnection handle.
        /// </summary>
        public static void SendToConnection(HSteamNetConnection conn, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
        {
            var bytes = SerializePacket(packet);
            var _sendType = (int)sendType;

            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);

                var result = SteamNetworkingSockets.SendMessageToConnection(
                    conn, unmanagedPointer, (uint)bytes.Length, _sendType, out long msgNum);

                bool sent = (result == EResult.k_EResultOK);

                if (!sent)
                {
                    DebugConsole.LogError($"[Sockets] Failed to send {packet.Type} to conn {conn} ({Utils.FormatBytes(bytes.Length)} | result: {result})", false);
                }
                else
                {
                    DebugConsole.Log($"[Sockets] Sent {packet.Type} to conn {conn} ({Utils.FormatBytes(bytes.Length)})");
                    SteamLobby.Stats.AddBytesSent(bytes.Length);
                    SteamLobby.Stats.IncrementSentPackets();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedPointer);
            }
        }

        /// <summary>
        /// Send a packet to a player by their SteamID.
        /// </summary>
        public static void SendToPlayer(CSteamID steamID, IPacket packet, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
        {
            if (MultiplayerSession.BlockPacketProcessing)
                return;

            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(steamID, out var player) || player.Connection == null)
            {
                DebugConsole.LogWarning($"[PacketSender] No connection found for SteamID {steamID}");
                return;
            }

            SendToConnection(player.Connection.Value, packet, sendType);
        }

        /// <summary>
        /// Send to all players, optionally excluding one SteamID.
        /// </summary>
        public static void SendToAll(IPacket packet, CSteamID? exclude = null, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
        {
            if (MultiplayerSession.BlockPacketProcessing)
                return;

            foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
            {
                if (exclude.HasValue && player.SteamID == exclude.Value)
                    continue;

                if (player.Connection != null)
                    SendToConnection(player.Connection.Value, packet, sendType);
            }
        }

    }
}
