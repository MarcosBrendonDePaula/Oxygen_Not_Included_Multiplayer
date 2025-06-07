using HarmonyLib;
using JetBrains.Annotations;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using UnityEngine;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(PauseScreen), "OnQuitConfirm")]
    public static class PauseScreenPatch
    {
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void OnQuitConfirm_Prefix(bool saveFirst)
        {
            if (MultiplayerSession.InSession)
            {
                SteamLobby.LeaveLobby();
                DebugConsole.Log("Left Steam lobby before quitting to main menu.");
                MultiplayerSession.Clear();
            }

        }
    }
}
