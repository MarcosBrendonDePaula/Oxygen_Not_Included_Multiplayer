using UnityEngine;
using ONI_MP.DebugTools;
using Steamworks;
using ONI_MP.Misc;

namespace ONI_MP.Networking.Components
{
    public class SteamNetworkingComponent : MonoBehaviour
    {
        public static UnityTaskScheduler scheduler = new UnityTaskScheduler();

        private void Start()
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            GameClient.Init();
        }

        private void Update()
        {
            scheduler.Tick();

            if (!SteamManager.Initialized)
                return;

            if (!MultiplayerSession.InSession)
                return;

            SteamLobby.Stats.UpdatePacketRates(Time.deltaTime);

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
