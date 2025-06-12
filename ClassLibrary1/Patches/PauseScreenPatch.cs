using HarmonyLib;
using JetBrains.Annotations;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using UnityEngine;

namespace ONI_MP.Patches
{
    [HarmonyPatch]
    public static class PauseScreenPatch
    {
        // This method is called when "Quit" is confirmed in the pause menu
        [HarmonyPatch(typeof(PauseScreen), "OnQuitConfirm")]
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void OnQuitConfirm_Prefix(bool saveFirst)
        {
            if (MultiplayerSession.InSession)
            {
                SteamLobby.LeaveLobby();
                MultiplayerSession.Clear();
            }
        }

        // This prevents the game from pausing when the PauseScreen opens in multiplayer
        [HarmonyPatch(typeof(SpeedControlScreen), nameof(SpeedControlScreen.Pause))]
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool PreventPauseInMultiplayer(bool playSound = true, bool isCrashed = false)
        {
            if (MultiplayerSession.InSession && !isCrashed)
            {
                return false;
            }

            return true;
        }

    }
}
