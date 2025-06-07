using ONI_MP.DebugTools;
using Steamworks;

namespace ONI_MP.Networking
{
    public static class SteamLobby
    {
        private static Callback<LobbyCreated_t> _lobbyCreated;
        private static Callback<GameLobbyJoinRequested_t> _lobbyJoinRequested;
        private static Callback<LobbyEnter_t> _lobbyEntered;
        private static Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
        private static Callback<P2PSessionRequest_t> _p2pSessionRequest;
        private static Callback<P2PSessionConnectFail_t> _p2pConnectFail;

        public static CSteamID CurrentLobby { get; private set; } = CSteamID.Nil;
        public static bool InLobby => CurrentLobby.IsValid();

        public static void Initialize()
        {
            if (!SteamManager.Initialized) return;

            _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
            _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            _p2pSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            _p2pConnectFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PConnectFail);

            DebugConsole.Log("[SteamLobby] Callbacks registered.");
        }

        public static void CreateLobby(int maxPlayers = 4, ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic)
        {
            if (!SteamManager.Initialized) return;

            if (InLobby)
            {
                DebugConsole.LogWarning("[SteamLobby] Cannot create a new lobby while already in one.");
                return;
            }

            DebugConsole.Log("[SteamLobby] Creating new lobby...");
            SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
        }

        public static void LeaveLobby()
        {
            if (InLobby)
            {
                DebugConsole.Log("[SteamLobby] Leaving lobby...");
                SteamMatchmaking.LeaveLobby(CurrentLobby);
                MultiplayerSession.Clear();
                CurrentLobby = CSteamID.Nil;
                SteamRichPresence.Clear();
            }
        }

        private static void OnLobbyCreated(LobbyCreated_t callback)
        {
            if (callback.m_eResult == EResult.k_EResultOK)
            {
                CurrentLobby = new CSteamID(callback.m_ulSteamIDLobby);
                DebugConsole.Log($"[SteamLobby] Lobby created: {CurrentLobby}");

                SteamMatchmaking.SetLobbyData(CurrentLobby, "name", SteamFriends.GetPersonaName() + "'s Lobby");
                SteamMatchmaking.SetLobbyData(CurrentLobby, "host", SteamUser.GetSteamID().ToString());

                MultiplayerSession.Clear();
                MultiplayerSession.SetHost(SteamUser.GetSteamID());
                MultiplayerSession.AddPeer(SteamUser.GetSteamID());

                SteamRichPresence.SetLobbyInfo(CurrentLobby, "Multiplayer – Hosting Lobby");
            }
            else
            {
                DebugConsole.LogError($"[SteamLobby] Failed to create lobby: {callback.m_eResult}");
            }
        }

        private static void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            DebugConsole.Log($"[SteamLobby] Joining lobby invited by {callback.m_steamIDFriend}");
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }

        private static void OnLobbyEntered(LobbyEnter_t callback)
        {
            CurrentLobby = new CSteamID(callback.m_ulSteamIDLobby);
            DebugConsole.Log($"[SteamLobby] Entered lobby: {CurrentLobby}");

            MultiplayerSession.Clear();

            string hostStr = SteamMatchmaking.GetLobbyData(CurrentLobby, "host");
            if (ulong.TryParse(hostStr, out ulong hostId))
            {
                MultiplayerSession.SetHost(new CSteamID(hostId));
            }

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
            for (int i = 0; i < memberCount; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i);
                MultiplayerSession.AddPeer(member);
            }

            SteamRichPresence.SetLobbyInfo(CurrentLobby, "Multiplayer – In Lobby");
        }

        private static void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
        {
            CSteamID user = new CSteamID(callback.m_ulSteamIDUserChanged);
            EChatMemberStateChange stateChange = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;

            if ((stateChange & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
            {
                MultiplayerSession.AddPeer(user);
                DebugConsole.Log($"[SteamLobby] {SteamFriends.GetFriendPersonaName(user)} joined the lobby.");
            }

            if ((stateChange & EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0 ||
                (stateChange & EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0 ||
                (stateChange & EChatMemberStateChange.k_EChatMemberStateChangeKicked) != 0)
            {
                MultiplayerSession.RemovePeer(user);
                DebugConsole.Log($"[SteamLobby] {SteamFriends.GetFriendPersonaName(user)} left the lobby.");
            }
        }

        private static void OnP2PSessionRequest(P2PSessionRequest_t request)
        {
            if (MultiplayerSession.ConnectedPlayers.ContainsKey(request.m_steamIDRemote))
            {
                SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote);
                DebugConsole.Log($"[SteamLobby] Accepted P2P session from {request.m_steamIDRemote}");
            }
            else
            {
                DebugConsole.LogWarning($"[SteamLobby] Rejected P2P session request from unknown peer {request.m_steamIDRemote}");
            }
        }

        private static void OnP2PConnectFail(P2PSessionConnectFail_t fail)
        {
            DebugConsole.LogError($"[SteamLobby] P2P connection failed with {fail.m_steamIDRemote}, reason: {fail.m_eP2PSessionError}");
        }
    }
}
