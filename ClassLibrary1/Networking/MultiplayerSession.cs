using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;
using ONI_MP.UI;
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
                if (!ConnectedPlayers.ContainsKey(peer))
                {
                    ConnectedPlayers[peer] = new MultiplayerPlayer(peer);
                    ChatScreen.QueueMessage($"<color=yellow>[System]</color> <b>{ConnectedPlayers[peer].SteamName}</b> joined the game.");
                }

                // Send any existing peers to new players
                foreach (var player in ConnectedPlayers.Values)
                {
                    if (player.SteamID == peer)
                        continue; // Don’t send the new peer to themselves

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
            }
        }

        public static void RemovePeer(CSteamID peer)
        {
            if (IsHost)
            {
                // ✅ Remove from internal state before broadcasting
                if (ConnectedPlayers.ContainsKey(peer))
                {
                    ChatScreen.QueueMessage($"<color=yellow>[System]</color> <b>{ConnectedPlayers[peer].SteamName}</b> left the game.");
                    ConnectedPlayers.Remove(peer);
                }
                else
                {
                    DebugConsole.LogWarning($"[MultiplayerSession] Tried to remove unknown player {peer}.");
                }

                // Broadcast to all remaining peers
                var packet = new PlayerLeftPacket
                {
                    SteamId = peer
                };

                PacketSender.SendToAll(packet);
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
