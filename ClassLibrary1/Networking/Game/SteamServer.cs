using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Game
{
    public static class SteamServer
    {
        public static HSteamListenSocket ListenSocket { get; private set; }
        public static HSteamNetPollGroup PollGroup { get; private set; }
        private static Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;
        private static bool Started = false;

        public static void Start()
        {
            if (!SteamManager.Initialized)
            {
                DebugConsole.LogError("[GameServer] SteamManager not initialized! Cannot start listen server.");
                return;
            }

            // Create listen socket for P2P
            ListenSocket = SteamNetworkingSockets.CreateListenSocketP2P(
                0, // Virtual port
                0, // nOptions
                null // pOptions
            );

            if (ListenSocket.m_HSteamListenSocket == 0)
            {
                DebugConsole.LogError("[GameServer] Failed to create ListenSocket!");
                return;
            }

            PollGroup = SteamNetworkingSockets.CreatePollGroup();

            if (PollGroup.m_HSteamNetPollGroup == 0)
            {
                DebugConsole.LogError("[GameServer] Failed to create PollGroup!");
                SteamNetworkingSockets.CloseListenSocket(ListenSocket);
                return;
            }

            _connectionStatusChangedCallback =
                Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);

            DebugConsole.Log("[GameServer] Listen socket and poll group created (CLIENT API).");
            Started = true;
            MultiplayerSession.InSession = true;
            MultiplayerOverlay.Close();
        }

        public static void Shutdown()
        {
            // Close all client connections and clean up
            foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
            {
                if (player.Connection != null)
                {
                    SteamNetworkingSockets.CloseConnection(player.Connection.Value, 0, "Shutdown", false);
                    player.Connection = null;
                }
            }

            if (PollGroup.m_HSteamNetPollGroup != 0)
                SteamNetworkingSockets.DestroyPollGroup(PollGroup);

            if (ListenSocket.m_HSteamListenSocket != 0)
                SteamNetworkingSockets.CloseListenSocket(ListenSocket);

            Started = false;
            MultiplayerSession.InSession = false;
            DebugConsole.Log("[GameServer] Shutdown complete.");
        }

        public static void Update()
        {
            if (!Started)
                return;

            SteamAPI.RunCallbacks(); // Not 100% sure if this is needed
            SteamNetworkingSockets.RunCallbacks();
            ReceiveMessages();
        }

        private static void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
        {
            var conn = data.m_hConn;
            var clientId = data.m_info.m_identityRemote.GetSteamID();
            var state = data.m_info.m_eState;

            DebugConsole.Log($"[GameServer] OnConnectionStatusChanged: state={state} from {clientId}");

            switch (state)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                    var result = SteamNetworkingSockets.AcceptConnection(conn);
                    if (result == EResult.k_EResultOK)
                    {
                        SteamNetworkingSockets.SetConnectionPollGroup(conn, PollGroup);
                        DebugConsole.Log($"[GameServer] Connection accepted from {clientId}");
                    }
                    else
                    {
                        DebugConsole.LogError($"[GameServer] Failed to accept connection from {clientId} ({result})");
                        SteamNetworkingSockets.CloseConnection(conn, 0, "Accept failed", false);
                    }
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    MultiplayerPlayer player;
                    if (!MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out player))
                    {
                        player = new MultiplayerPlayer(clientId);
                        MultiplayerSession.ConnectedPlayers[clientId] = player;
                    }
                    player.Connection = conn;
                    DebugConsole.Log($"[GameServer] Connection to {clientId} fully established!");
                    DebugConsole.Log($"[GameServer] Sending new client the world data!");
                    SaveFileRequestPacket.SendSaveFile(clientId);
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    SteamNetworkingSockets.CloseConnection(conn, 0, null, false);

                    if (MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out var playerToRemove))
                    {
                        playerToRemove.Connection = null;
                    }

                    DebugConsole.Log($"[GameServer] Connection closed for {clientId}");
                    break;
            }
        }

        private static void ReceiveMessages()
        {
            var messages = new IntPtr[128];
            int msgCount = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(PollGroup, messages, 128);

            for (int i = 0; i < msgCount; i++)
            {
                var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(messages[i]);
                byte[] bytes = new byte[msg.m_cbSize];
                Marshal.Copy(msg.m_pData, bytes, 0, msg.m_cbSize);

                PacketHandler.HandleIncoming(bytes);

                SteamNetworkingMessage_t.Release(messages[i]);
            }
        }
    }
}
