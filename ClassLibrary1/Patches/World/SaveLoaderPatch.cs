using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking;
using UnityEngine;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch]
    public static class SaveLoaderPatch
    {
        // Add prefix to prevent CustomGameSettings crashes
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SaveLoader), "OnSpawn")]
        public static void Prefix_OnSpawn()
        {
            try
            {
                // Ensure CustomGameSettings is properly initialized before spawn
                if (CustomGameSettings.Instance == null)
                {
                    DebugConsole.LogWarning("[SaveLoaderPatch] CustomGameSettings.Instance is null, attempting to initialize...");
                }
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[SaveLoaderPatch] Error in OnSpawn prefix: {ex}");
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoader), "OnSpawn")]
        public static void Postfix_OnSpawn()
        {
            try
            {
                TryCreateLobbyAfterLoad("[Multiplayer] Lobby created after world load.");
                if(MultiplayerSession.InSession)
                {
                    SpeedControlScreen.Instance?.Unpause(false); // Unpause the game
                }
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[SaveLoaderPatch] Error in OnSpawn postfix: {ex}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoader), nameof(SaveLoader.LoadFromWorldGen))]
        public static void Postfix_LoadFromWorldGen(bool __result)
        {
            try
            {
                if (__result)
                    TryCreateLobbyAfterLoad("[Multiplayer] Lobby created after new world gen.");
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[SaveLoaderPatch] Error in LoadFromWorldGen postfix: {ex}");
            }
        }

        private static void TryCreateLobbyAfterLoad(string logMessage)
        {
            try
            {
                if (MultiplayerSession.ShouldHostAfterLoad)
                {
                    MultiplayerSession.ShouldHostAfterLoad = false;

                    // Add delay to ensure game is fully loaded before creating lobby
                    CoroutineRunner.RunOne(CreateLobbyDelayed(logMessage));
                }
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[SaveLoaderPatch] Error in TryCreateLobbyAfterLoad: {ex}");
            }
        }

        private static System.Collections.IEnumerator CreateLobbyDelayed(string logMessage)
        {
            // Wait a frame to ensure all systems are initialized
            yield return null;
            
            try
            {
                SteamLobby.CreateLobby(onSuccess: () =>
                {
                    SpeedControlScreen.Instance?.Unpause(false);
                    DebugConsole.Log(logMessage);
                });
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[SaveLoaderPatch] Error creating lobby after delay: {ex}");
            }
        }
    }
}
