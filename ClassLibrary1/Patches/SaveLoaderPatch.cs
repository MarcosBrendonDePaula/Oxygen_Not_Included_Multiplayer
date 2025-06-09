using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using System.Reflection;
using UnityEngine;

namespace ONI_MP.Patches
{
    [HarmonyPatch]
    public static class SaveLoaderPatch
    {
        // Patch private method: SaveLoader.OnSpawn
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoader), "OnSpawn")]
        public static void Postfix_OnSpawn()
        {
            TryCreateLobbyAfterLoad("[Multiplayer] Lobby created after world load.");
        }

        // Patch public method: SaveLoader.LoadFromWorldGen
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoader), nameof(SaveLoader.LoadFromWorldGen))]
        public static void Postfix_LoadFromWorldGen(bool __result)
        {
            if (__result)
                TryCreateLobbyAfterLoad("[Multiplayer] Lobby created after new world gen.");
        }

        private static void TryCreateLobbyAfterLoad(string logMessage)
        {
            if (MultiplayerSession.ShouldHostAfterLoad)
            {
                MultiplayerSession.ShouldHostAfterLoad = false;

                SteamLobby.CreateLobby(onSuccess: () =>
                {
                    SpeedControlScreen.Instance?.Unpause(false);
                    DebugConsole.Log(logMessage);
                });
            }
        }
    }
}
