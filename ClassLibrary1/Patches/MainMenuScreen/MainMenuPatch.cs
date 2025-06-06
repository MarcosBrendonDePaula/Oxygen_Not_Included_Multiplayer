using System;
using HarmonyLib;
using JetBrains.Annotations;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
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
                "MULTIPLAYER",
                "Play together!",
                highlight: true,
                () => OnMultiplayerClicked()
            );
        }

        private static void OnMultiplayerClicked()
        {
            MultiplayerMenu.Show();
            DebugConsole.Log("Multiplayer menu opened");
        }

    }
}
