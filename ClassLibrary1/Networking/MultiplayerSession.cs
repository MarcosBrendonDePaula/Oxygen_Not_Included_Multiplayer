
using ONI_MP.DebugTools;
using Steamworks;
using System.Collections.Generic;

namespace ONI_MP.Networking
    {
        public static class MultiplayerSession
        {
            public static readonly List<CSteamID> ConnectedPeers = new List<CSteamID>();

            public static CSteamID LocalSteamID => SteamUser.GetSteamID();

            public static CSteamID HostSteamID { get; private set; } = CSteamID.Nil;

            public static bool IsHost => HostSteamID == LocalSteamID;

            public static void AddPeer(CSteamID peer)
            {
                if (!ConnectedPeers.Contains(peer))
                {
                    ConnectedPeers.Add(peer);
                    DebugConsole.Log($"[MultiplayerSession] Peer added: {peer}");
                }
            }

            public static void RemovePeer(CSteamID peer)
            {
                if (ConnectedPeers.Remove(peer))
                {
                    DebugConsole.Log($"[MultiplayerSession] Peer removed: {peer}");
                }
            }

            public static void Clear()
            {
                ConnectedPeers.Clear();
                HostSteamID = CSteamID.Nil;
                DebugConsole.Log("[MultiplayerSession] Session cleared.");
            }

            public static void SetHost(CSteamID host)
            {
                HostSteamID = host;
                DebugConsole.Log($"[MultiplayerSession] Host set to: {host}");
            }
        }
    }