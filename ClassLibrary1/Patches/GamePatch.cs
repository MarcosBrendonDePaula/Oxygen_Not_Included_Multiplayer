using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Sync;
using ONI_MP.World;
using UnityEngine;

namespace ONI_MP.Patches
{
    // This single class contains BOTH patches.
    public static class GamePatch
    {
        // Patch Game.Update to run the two batchers if host
        [HarmonyPatch(typeof(Game), "Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix()
        {
            if (MultiplayerSession.IsHost)
            {
                InstantiationBatcher.Update();
                WorldUpdateBatcher.Update();
            }
        }

        // Patch Game.OnSpawn to set client state to InGame
        [HarmonyPatch(typeof(Game), "OnSpawn")]
        [HarmonyPrefix]
        public static void OnSpawnPrefix()
        {
            if (MultiplayerSession.IsClient)
            {
                DebugConsole.Log("Game on spawn pre fix");
                GameClient.SetInGame();
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        [HarmonyPrefix]
        public static void OnSpawnPostfix()
        {
            DebugConsole.Log("Game on spawn post fix");
        }
    }
}
