using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;
using Steamworks;
using System.Collections.Generic;

namespace ONI_MP.Networking
{
    public static class MultiplayerSession
    {
        public static bool ShouldHostAfterLoad = false;

        public static readonly Dictionary<CSteamID, MultiplayerPlayer> ConnectedPlayers = new Dictionary<CSteamID, MultiplayerPlayer>();

        public static CSteamID LocalSteamID => SteamUser.GetSteamID();

        public static CSteamID HostSteamID { get; private set; } = CSteamID.Nil;

        public static bool InSession => SteamLobby.InLobby && HostSteamID.IsValid();

        public static bool IsHost => HostSteamID == LocalSteamID;

        public static bool IsClient => InSession && !IsHost;

        public static void AddPeer(CSteamID peer)
        {
            if (IsHost)
            {
                // Send any existing peers to new players
                foreach (var player in ConnectedPlayers.Values)
                {
                    var existingPacket = new PlayerJoinedPacket
                    {
                        SteamId = player.SteamID
                    };

                    PacketSender.SendToPlayer(peer, existingPacket);
                }
                
                // Tell everyone else about the new player
                var newJoinPacket = new PlayerJoinedPacket
                {
                    SteamId = peer
                };

                PacketSender.SendToAll(newJoinPacket);
                DebugConsole.Log($"[MultiplayerSession] Sent PlayerJoinedPacket for {peer} to all clients.");
            }
        }



        public static void RemovePeer(CSteamID peer)
        {
            if (IsHost)
            {
                var packet = new PlayerLeftPacket
                {
                    SteamId = peer
                };

                PacketSender.SendToAll(packet);
                DebugConsole.Log($"[MultiplayerSession] Sent PlayerLeftPacket for {peer} to all clients.");
            }
        }



        public static void Clear()
        {
            ConnectedPlayers.Clear();
            HostSteamID = CSteamID.Nil;
            DebugConsole.Log("[MultiplayerSession] Session cleared.");
        }

        public static void SetHost(CSteamID host)
        {
            HostSteamID = host;
            DebugConsole.Log($"[MultiplayerSession] Host set to: {host}");
        }

        public static MultiplayerPlayer GetPlayer(CSteamID id)
        {
            return ConnectedPlayers.TryGetValue(id, out var player) ? player : null;
        }

        public static MultiplayerPlayer LocalPlayer => GetPlayer(LocalSteamID);

        public static IEnumerable<MultiplayerPlayer> AllPlayers => ConnectedPlayers.Values;
    }
}
