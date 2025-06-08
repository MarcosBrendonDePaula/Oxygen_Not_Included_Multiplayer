using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.UI;
using UnityEngine;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(SaveLoader))]
    [HarmonyPatch("OnSpawn")]
    [HarmonyPatch(MethodType.Normal)]
    public static class SaveLoaderPatch
    {
        public static void Postfix()
        {
            if (MultiplayerSession.ShouldHostAfterLoad)
            {
                MultiplayerSession.ShouldHostAfterLoad = false;

                SteamLobby.CreateLobby(onSuccess: () =>
                {
                    DebugConsole.Log("[Multiplayer] Lobby created after world load.");
                });
            }
        }
    }
}
