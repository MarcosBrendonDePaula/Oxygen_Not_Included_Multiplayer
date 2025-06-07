using ONI_MP.DebugTools;
using Steamworks;
using System.Collections.Generic;

namespace ONI_MP.Networking
{
    public static class MultiplayerSession
    {
        public static readonly Dictionary<CSteamID, MultiplayerPlayer> ConnectedPlayers = new Dictionary<CSteamID, MultiplayerPlayer>();

        public static CSteamID LocalSteamID => SteamUser.GetSteamID();

        public static CSteamID HostSteamID { get; private set; } = CSteamID.Nil;

        public static bool InSession => SteamLobby.InLobby && HostSteamID.IsValid();

        public static bool IsHost => HostSteamID == LocalSteamID;

        public static bool IsClient => InSession && !IsHost;

        public static void AddPeer(CSteamID peer)
        {
            if (!ConnectedPlayers.ContainsKey(peer))
            {
                var player = new MultiplayerPlayer(peer);
                ConnectedPlayers.Add(peer, player);
                DebugConsole.Log($"[MultiplayerSession] Player added: {player}");
            }
        }

        public static void RemovePeer(CSteamID peer)
        {
            if (ConnectedPlayers.Remove(peer))
            {
                DebugConsole.Log($"[MultiplayerSession] Player removed: {peer}");
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
