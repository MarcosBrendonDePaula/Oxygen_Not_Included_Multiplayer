using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace ONI_MP.Patches.MainMenuScreen
{
    [UsedImplicitly]
    [HarmonyPatch(typeof(MainMenu))]
    internal static class MainMenuPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnPrefabInit")]
        [UsedImplicitly]
        private static void OnPrefabInit(MainMenu __instance)
        {
            __instance.AddClonedButton(
                "NEW MULTIPLAYER",
                "Start a new game with multiplayer!",
                highlight: true,
                () => NewMultiplayer()
            );

            __instance.AddClonedButton(
                "LOAD MULTIPLAYER",
                "Load a game with multiplayer!",
                highlight: false,
                () => LoadMultiplayer()
            );

            __instance.AddClonedButton(
                "JOIN MULTIPLAYER",
                "Join an existing multiplayer game!",
                highlight: false,
                () => JoinMultiplayer()
            );
        }

        private static void NewMultiplayer()
        {
            Debug.Log("[ONI_MP] NEW MULTIPLAYER button clicked");
        }

        private static void LoadMultiplayer()
        {
            Debug.Log("[ONI_MP] LOAD MULTIPLAYER button clicked");
        }

        private static void JoinMultiplayer()
        {
            Debug.Log("[ONI_MP] JOIN MULTIPLAYER button clicked");
        }
    }
}
