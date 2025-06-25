using UnityEngine;
using ONI_MP.DebugTools;
using Steamworks;
using ONI_MP.Misc;
using ONI_MP.Networking.States;
using ONI_MP.Menus;

namespace ONI_MP.Networking.Components
{
    public class SteamNetworkingComponent : MonoBehaviour
    {
        public static UnityTaskScheduler scheduler = new UnityTaskScheduler();

        private void Start()
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            GameClient.Init();

            MultiplayerMod.OnPostSceneLoaded += () =>
            {
                if (GameClient.State.Equals(ClientState.LoadingWorld))
                {
                    GameClient.ReconnectFromCache();
                    MultiplayerOverlay.Close();
                }
                
                // Detect when returning to main menu while in multiplayer session
                if (Utils.IsInMenu() && MultiplayerSession.InSession)
                {
                    DebugConsole.Log("[SteamNetworking] Detected return to main menu while in multiplayer session. Cleaning up...");
                    if (MultiplayerSession.IsHost)
                    {
                        DebugConsole.Log("[SteamNetworking] Host returning to menu - shutting down server...");
                    }
                    else if (MultiplayerSession.IsClient)
                    {
                        DebugConsole.Log("[SteamNetworking] Client returning to menu - disconnecting...");
                    }
                    SteamLobby.LeaveLobby();
                }
            };
        }

        private void Update()
        {
            scheduler.Tick();

            if (!SteamManager.Initialized)
                return;

            if (!MultiplayerSession.InSession)
                return;

            if (MultiplayerSession.IsHost)
            {
                GameServer.Update();
            }
            else if (MultiplayerSession.IsClient && MultiplayerSession.HostSteamID.IsValid())
            {
                GameClient.Poll();
            }
        }

        private void OnApplicationQuit()
        {
            if (!MultiplayerSession.InSession)
                return;

            SteamLobby.LeaveLobby();
        }
    }
}
