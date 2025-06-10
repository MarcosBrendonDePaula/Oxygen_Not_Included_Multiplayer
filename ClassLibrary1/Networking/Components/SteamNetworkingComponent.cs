using UnityEngine;
using ONI_MP.DebugTools;
using Steamworks;

namespace ONI_MP.Networking.Components
{
    public class SteamNetworkingComponent : MonoBehaviour
    {
        private void Start()
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            GameClient.Init();
        }

        private void Update()
        {
            if (!SteamManager.Initialized)
                return;

            if (!MultiplayerSession.InSession)
                return;

            if (MultiplayerSession.BlockPacketProcessing)
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
