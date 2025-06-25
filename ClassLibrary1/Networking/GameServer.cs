using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using Steamworks;
using UnityEngine;
using ONI_MP.Networking.States;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets;

namespace ONI_MP.Networking
{
    public static class GameServer
    {
        public static HSteamListenSocket ListenSocket { get; private set; }
        public static HSteamNetPollGroup PollGroup { get; private set; }
        private static Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;

        private static ServerState _state = ServerState.Stopped;
        public static ServerState State => _state;

        private static void SetState(ServerState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                DebugConsole.Log($"[GameServer] State changed to: {_state}");
            }
        }

        public static void Start()
        {
            SetState(ServerState.Preparing);

            if (!SteamManager.Initialized)
            {
                SetState(ServerState.Error);
                DebugConsole.LogError("[GameServer] SteamManager not initialized! Cannot start listen server.");
                return;
            }

            SetState(ServerState.Starting);

            // Create listen socket for P2P
            ListenSocket = SteamNetworkingSockets.CreateListenSocketP2P(
                0, // Virtual port
                0, // nOptions
                null // pOptions
            );

            if (ListenSocket.m_HSteamListenSocket == 0)
            {
                SetState(ServerState.Error);
                DebugConsole.LogError("[GameServer] Failed to create ListenSocket!");
                return;
            }

            PollGroup = SteamNetworkingSockets.CreatePollGroup();

            if (PollGroup.m_HSteamNetPollGroup == 0)
            {
                SetState(ServerState.Error);
                DebugConsole.LogError("[GameServer] Failed to create PollGroup!");
                SteamNetworkingSockets.CloseListenSocket(ListenSocket);
                return;
            }

            _connectionStatusChangedCallback =
                Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);

            DebugConsole.Log("[GameServer] Listen socket and poll group created (CLIENT API).");
            MultiplayerSession.InSession = true;
            MultiplayerOverlay.Close();

            SetState(ServerState.Started);
        }

        public static void Shutdown()
        {
            SetState(ServerState.Stopped);

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

            MultiplayerSession.InSession = false;
            DebugConsole.Log("[GameServer] Shutdown complete.");
        }

        public static void Update()
        {
            switch (State)
            {
                case ServerState.Started:
                case ServerState.WaitingForModSync:
                case ServerState.ModSyncComplete:
                    SteamAPI.RunCallbacks();
                    SteamNetworkingSockets.RunCallbacks();
                    ReceiveMessages();
                    break;

                case ServerState.Preparing:
                case ServerState.Starting:
                case ServerState.Stopped:
                case ServerState.Error:
                default:
                    // No server activity in these states.
                    break;
            }
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
                    TryAcceptConnection(conn, clientId);
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    OnClientConnected(conn, clientId);
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    OnClientClosed(conn, clientId);
                    break;
            }
        }

        private static void TryAcceptConnection(HSteamNetConnection conn, CSteamID clientId)
        {
            var result = SteamNetworkingSockets.AcceptConnection(conn);
            if (result == EResult.k_EResultOK)
            {
                SteamNetworkingSockets.SetConnectionPollGroup(conn, PollGroup);
                DebugConsole.Log($"[GameServer] Connection accepted from {clientId}");
            }
            else
            {
                RejectConnection(conn, clientId, $"Accept failed ({result})");
            }
        }

        private static void RejectConnection(HSteamNetConnection conn, CSteamID clientId, string reason)
        {
            DebugConsole.LogError($"[GameServer] Rejecting connection from {clientId}: {reason}");
            SteamNetworkingSockets.CloseConnection(conn, 0, reason, false);
        }

        private static void OnClientConnected(HSteamNetConnection conn, CSteamID clientId)
        {
            MultiplayerPlayer player;
            bool isNewPlayer = !MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out player);
            
            if (isNewPlayer)
            {
                player = new MultiplayerPlayer(clientId);
                MultiplayerSession.ConnectedPlayers[clientId] = player;
                DebugConsole.Log($"[GameServer] New client {clientId} connected!");
            }
            else
            {
                DebugConsole.Log($"[GameServer] Client {clientId} reconnected!");
            }
            
            player.Connection = conn;

            DebugConsole.Log($"[GameServer] Connection to {clientId} fully established!");
            
            // Check if this is a hard sync reconnection
            bool isHardSyncReconnection = GameClient.IsHardSyncInProgress;
            
            // During hard sync, never do mod validation - game is already running
            if (isHardSyncReconnection)
            {
                DebugConsole.Log($"[GameServer] Hard sync reconnection for {clientId} - skipping all mod validation");
                
                // Mark client as ready for game packets immediately
                player.ModSyncCompleted = true;
                player.ModSyncCompatible = true;
                player.readyState = ClientReadyState.Ready;
                
                // Ensure server is in correct state to handle game packets
                if (State != ServerState.ModSyncComplete)
                {
                    SetState(ServerState.ModSyncComplete);
                    DebugConsole.Log("[GameServer] Set server state to ModSyncComplete for hard sync");
                }
                
                DebugConsole.Log($"[GameServer] Client {clientId} marked as ready after hard sync reconnection");
                return;
            }
            
            // Only do mod sync for new players or players who haven't completed mod sync
            if (isNewPlayer || !player.ModSyncCompleted)
            {
                DebugConsole.Log($"[GameServer] Starting mod sync for {clientId}");
                ModListSyncPacket.SendModList(clientId);
                SetState(ServerState.WaitingForModSync);
            }
            else if (player.ModSyncCompatible)
            {
                DebugConsole.Log($"[GameServer] Client {clientId} already validated. Skipping mod sync and save file transfer.");
                // Client already validated and has the save file, keep current server state
                // No need to send save file again - they're just reconnecting after loading
            }
            else
            {
                DebugConsole.LogWarning($"[GameServer] Client {clientId} reconnected but was previously incompatible.");
                // Send mod list again in case they fixed compatibility issues
                ModListSyncPacket.SendModList(clientId);
                SetState(ServerState.WaitingForModSync);
            }
        }

        public static void OnModCompatibilityReceived(CSteamID clientId, ModCompatibilityStatusPacket.CompatibilityStatus status)
        {
            if (MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out var player))
            {
                player.ModSyncCompleted = true;
                player.ModSyncCompatible = (status == ModCompatibilityStatusPacket.CompatibilityStatus.Compatible);
                
                DebugConsole.Log($"[GameServer] Mod compatibility received from {clientId}: {status}");
                
                // If this specific client is compatible, send save file immediately
                if (player.ModSyncCompatible)
                {
                    DebugConsole.Log($"[GameServer] Client {clientId} is mod compatible. Sending save file...");
                    SaveFileRequestPacket.SendSaveFile(clientId);
                }
                else
                {
                    DebugConsole.LogWarning($"[GameServer] Client {clientId} has mod compatibility issues. Save sync blocked for this client.");
                }
                
                // Check if all connected clients have completed mod sync
                bool allClientsReady = true;
                bool allClientsCompatible = true;
                int connectedClientCount = 0;
                
                foreach (var connectedPlayer in MultiplayerSession.ConnectedPlayers.Values)
                {
                    if (connectedPlayer.IsConnected && !connectedPlayer.IsLocal)
                    {
                        connectedClientCount++;
                        if (!connectedPlayer.ModSyncCompleted)
                        {
                            allClientsReady = false;
                        }
                        if (!connectedPlayer.ModSyncCompatible)
                        {
                            allClientsCompatible = false;
                        }
                    }
                }
                
                DebugConsole.Log($"[GameServer] Mod sync status: {connectedClientCount} clients connected, allReady: {allClientsReady}, allCompatible: {allClientsCompatible}");
                
                if (allClientsReady && connectedClientCount > 0)
                {
                    SetState(ServerState.ModSyncComplete);
                    if (allClientsCompatible)
                    {
                        DebugConsole.Log("[GameServer] All clients have completed mod sync and are compatible.");
                        SaveFileRequestPacket.SendSaveFile(clientId);
                    }
                    else
                    {
                        DebugConsole.LogWarning("[GameServer] All clients have completed mod sync, but some have compatibility issues.");
                    }
                }
            }
        }


        private static void OnClientClosed(HSteamNetConnection conn, CSteamID clientId)
        {
            SteamNetworkingSockets.CloseConnection(conn, 0, null, false);

            if (MultiplayerSession.ConnectedPlayers.TryGetValue(clientId, out var playerToRemove))
            {
                playerToRemove.Connection = null;
            }

            DebugConsole.Log($"[GameServer] Connection closed for {clientId}");

            // Do I wanna auto shutdown here? I don't think so
            // if (MultiplayerSession.ConnectedPlayers.Count == 0)
            // {
            //     SetState(ServerState.Stopped);
            //     Shutdown
            // }
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
