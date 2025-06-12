using Steamworks;
using ONI_MP.DebugTools;
using System;
using System.Runtime.InteropServices;
using ONI_MP.Networking.Packets;
using ONI_MP.Misc;
using ONI_MP.Menus;

namespace ONI_MP.Networking
{
    public static class GameClient
    {
        private static Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;
        public static HSteamNetConnection? Connection { get; private set; }
        public static bool Connected { get; private set; } = false;

        private static bool _pollingPaused = false;

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

        public static void ReconnectToSession()
        {
            // Disconnect if currently connected or have a connection
            if (Connection.HasValue || Connected)
            {
                DebugConsole.Log("[GameClient] Reconnecting: First disconnecting existing connection.");
                Disconnect();

                // Delay to ensure the connection is properly closed
                System.Threading.Thread.Sleep(100); // 100ms
            }

            // Re-attempt to connect to the last known host
            if (MultiplayerSession.HostSteamID != CSteamID.Nil)
            {
                DebugConsole.Log("[GameClient] Attempting to reconnect to host...");
                ConnectToHost(MultiplayerSession.HostSteamID);
            }
            else
            {
                DebugConsole.LogWarning("[GameClient] Cannot reconnect: HostSteamID is not set.");
            }
        }


        public static void Poll()
        {
            if(_pollingPaused)
            {
                return;
            }

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

            // Ignore if this is not for our active connection
            if (Connection.HasValue && data.m_hConn.m_HSteamNetConnection != Connection.Value.m_HSteamNetConnection)
                return;

            switch (state)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    Connected = true;
                    MultiplayerSession.InSession = true;
                    DebugConsole.Log("[GameClient] Connection to host established!");
                    if(Utils.IsInMenu())
                    {
                        //RequestHostWorldFile(); // Host can also auto-send this on connect
                        MultiplayerOverlay.Show("Downloading world: Waiting...");
                    }
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    DebugConsole.LogWarning($"[GameClient] Connection closed or failed ({state}) for {remote}");
                    MultiplayerSession.InSession = false;
                    Connected = false;
                    Connection = null;
                    break;

                default:
                    break;
            }
        }

        public static int? GetPingToHost()
        {
            if (Connection.HasValue)
            {
                SteamNetConnectionRealTimeStatus_t status = default;
                SteamNetConnectionRealTimeLaneStatus_t laneStatus = default; // Or more if you want more lanes

                EResult res = SteamNetworkingSockets.GetConnectionRealTimeStatus(
                    Connection.Value,
                    ref status,
                    0,
                    ref laneStatus
                );

                if (res == EResult.k_EResultOK)
                {
                    // You can now access both status.m_nPing and laneStatus[0].m_nQueueTime or other lane stats
                    return status.m_nPing >= 0 ? (int?)status.m_nPing : null;
                }
            }
            return null;
        }






        public static void RequestHostWorldFile()
        {
            var req = new SaveFileRequestPacket { Requester = SteamUser.GetSteamID() };
            PacketSender.SendToPlayer(MultiplayerSession.HostSteamID, req);
        }

        public static void PauseNetworkingCallbacks()
        {
            _pollingPaused = true;
            DebugConsole.Log("[GameClient] Networking callbacks paused.");
        }

        public static void ResumeNetworkingCallbacks()
        {
            _pollingPaused = false;
            DebugConsole.Log("[GameClient] Networking callbacks resumed.");
        }

        public static void DisableMessageHandlers()
        {
            if (_connectionStatusChangedCallback != null)
            {
                _connectionStatusChangedCallback.Unregister();
                _connectionStatusChangedCallback = null;
                DebugConsole.Log("[GameClient] Networking message handlers disabled.");
            }
        }

        public static void EnableMessageHandlers()
        {
            if (_connectionStatusChangedCallback == null)
            {
                _connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
                DebugConsole.Log("[GameClient] Networking message handlers enabled.");
            }
        }

    }
}
