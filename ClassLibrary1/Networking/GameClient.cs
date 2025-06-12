using Steamworks;
using ONI_MP.DebugTools;
using System;
using System.Runtime.InteropServices;
using ONI_MP.Networking.Packets;

namespace ONI_MP.Networking
{
    public static class GameClient
    {
        private static Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;
        public static HSteamNetConnection? Connection { get; private set; }
        public static bool Connected { get; private set; } = false;

        public static void Init()
        {
            if (_connectionStatusChangedCallback == null)
            {
                _connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
                DebugConsole.Log("[GameClient] Registered connection status callback.");
            }
        }

        public static void ConnectToHost(CSteamID hostSteamId)
        {
            DebugConsole.Log($"[GameClient] Attempting ConnectP2P to host {hostSteamId}...");
            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID64(hostSteamId.m_SteamID);

            Connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, null);
            DebugConsole.Log($"[GameClient] ConnectP2P returned handle: {Connection.Value.m_HSteamNetConnection}");
        }

        public static void Disconnect()
        {
            if (Connection.HasValue)
            {
                DebugConsole.Log("[GameClient] Disconnecting from host...");

                // Close the connection
                bool result = SteamNetworkingSockets.CloseConnection(
                    Connection.Value,
                    0,
                    "Client disconnecting",
                    false // do not linger; close immediately
                );

                DebugConsole.Log($"[GameClient] CloseConnection result: {result}");

                // Update state
                Connection = null;
                Connected = false;
                MultiplayerSession.InSession = false;
            }
            else
            {
                DebugConsole.LogWarning("[GameClient] Disconnect called, but no connection exists.");
            }
        }


        public static void Poll()
        {
            SteamNetworkingSockets.RunCallbacks();

            // If connected, process incoming messages
            if (Connected && Connection.HasValue)
            {
                ProcessIncomingMessages(Connection.Value);
            }
        }

        private static void ProcessIncomingMessages(HSteamNetConnection conn)
        {
            IntPtr[] messages = new IntPtr[16];
            int msgCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, messages, messages.Length);

            for (int i = 0; i < msgCount; i++)
            {
                var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(messages[i]);
                byte[] data = new byte[msg.m_cbSize];
                Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);

                SteamLobby.Stats.IncrementReceivedPackets(msg.m_cbSize);

                try
                {
                    PacketHandler.HandleIncoming(data);
                }
                catch (Exception ex)
                {
                    DebugConsole.LogError($"[GameClient] Failed to handle incoming packet: {ex}");
                }

                SteamNetworkingMessage_t.Release(messages[i]);
            }
        }

        private static void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
        {
            var state = data.m_info.m_eState;
            var remote = data.m_info.m_identityRemote.GetSteamID();

            DebugConsole.Log($"[GameClient] Connection status changed: {state} (remote={remote})");

            if (Connection.HasValue && data.m_hConn.m_HSteamNetConnection != Connection.Value.m_HSteamNetConnection)
                return; // Not for our main host connection

            if (state == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
            {
                Connected = true;
                DebugConsole.Log("[GameClient] Connection to host established!");
                MultiplayerSession.InSession = true;
                //RequestHostWorldFile(); // Have host just send it on connect
            }
            else if (state == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer ||
                     state == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
            {
                DebugConsole.LogWarning($"[GameClient] Connection closed or failed ({state}) for {remote}");
                MultiplayerSession.InSession = false;
                Connected = false;
                Connection = null;
            }
        }

        public static void RequestHostWorldFile()
        {
            var req = new SaveFileRequestPacket { Requester = SteamUser.GetSteamID() };
            PacketSender.SendToPlayer(MultiplayerSession.HostSteamID, req);
        }
    }
}
