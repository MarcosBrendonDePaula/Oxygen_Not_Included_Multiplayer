using HarmonyLib;
using ONI_MP.Networking;
using UnityEngine;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch(typeof(SpeedControlScreen), nameof(SpeedControlScreen.TogglePause))]
    public static class SpeedControlPausePatch
    {
        public static bool Prefix(bool playsound)
        {
            DebugConsole.Log("[ONI_MP] Intercepted TogglePause");

            // Block pausing when in a multiplayer session
            if (MultiplayerSession.InSession)
            {
                DebugConsole.Log("[ONI_MP] Pause prevented during multiplayer session.");
                return true;
            }

            return true;
        }
    }
}
