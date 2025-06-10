using UnityEngine;
using ONI_MP.Networking;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Components
{
    public class SteamNetworkingComponent : MonoBehaviour
    {
        private void Update()
        {
            if (!SteamManager.Initialized)
                return;
            

            if (!MultiplayerSession.InSession)
               return;

            if (MultiplayerSession.BlockPacketProcessing)
                return;

            SteamLobby.UpdatePacketRates(Time.deltaTime);
            SteamLobby.ProcessIncomingPackets();
        }

        private void OnApplicationQuit()
        {
            if (!MultiplayerSession.InSession)
                return;

            SteamLobby.LeaveLobby();
        }
    }
}
