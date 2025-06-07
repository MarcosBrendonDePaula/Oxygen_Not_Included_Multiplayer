using System;
using HarmonyLib;
using JetBrains.Annotations;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ONI_MP.Patches.MainMenuScreen
{
    [UsedImplicitly]
    [HarmonyPatch(typeof(MainMenu))]
    internal static class MainMenuPatch
    {
        public static MainMenu Instance { get; private set; }

        [HarmonyPrefix]
        [HarmonyPatch("OnPrefabInit")]
        [UsedImplicitly]
        private static void OnPrefabInit(MainMenu __instance)
        {
            if (Instance == null)
            {
                Instance = __instance;
            }

            __instance.AddClonedButton(
                "MULTIPLAYER",
                "Play together!",
                highlight: true,
                () => OnMultiplayerClicked()
            );
        }

        private static void OnMultiplayerClicked()
        {
            MultiplayerPopup.Show(FrontEndManager.Instance.transform);
        }

    }
}
