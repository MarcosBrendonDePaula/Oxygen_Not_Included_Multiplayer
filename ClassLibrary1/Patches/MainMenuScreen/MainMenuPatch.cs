using System;
using HarmonyLib;
using JetBrains.Annotations;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using Steamworks;
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

            /*
            __instance.AddClonedButton(
                "MULTIPLAYER",
                "Play together!",
                highlight: true,
                () => OnMultiplayerClicked()
            );
            */
            __instance.AddClonedButton(
                "Host Game",
                "Resume your last save!",
                highlight: true,
                () => OnHostClicked()
            );

            __instance.AddClonedButton(
                "Join Game",
                "Join your friends!",
                highlight: true,
                () => OnJoinClicked()
            );
        }

        private static void OnMultiplayerClicked()
        {
            MultiplayerPopup.Show(FrontEndManager.Instance.transform);
        }

        private static void OnHostClicked()
        {
            MultiplayerSession.ShouldHostAfterLoad = true;
            MainMenuPatch.Instance.Button_ResumeGame.SignalClick(KKeyCode.Mouse0);
        }

        private static void OnJoinClicked()
        {
            SteamFriends.ActivateGameOverlay("friends");
        }

    }
}
