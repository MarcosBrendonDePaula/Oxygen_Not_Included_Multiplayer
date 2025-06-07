using HarmonyLib;
using TMPro;
using UnityEngine;
using System.Reflection;
using JetBrains.Annotations;
using ONI_MP.DebugTools;
using ONI_MP.Networking;

namespace ONI_MP.Patches.LoadingScreen
{
    [HarmonyPatch(typeof(LoadingOverlay), "Load")]
    public static class LoadingScreenPatch
    {
        [HarmonyPostfix]
        [UsedImplicitly]
        public static void Load_Postfix()
        {
            DebugConsole.LogWarning("Updating loading screen!");

            // Find the overlay instance
            var overlay = GameObject.FindObjectOfType<LoadingOverlay>();
            if (overlay == null)
            {
                DebugConsole.LogWarning("Could not find LoadingOverlay instance.");
                return;
            }

            // Find the text component (TMP or LocText)
            var locText = overlay.GetComponentInChildren<LocText>();
            if (locText != null)
            {
                locText.SetText(SteamLobby.InLobby ? "Connecting to Multiplayer..." : "Loading...");
                DebugConsole.Log("Loading screen message set.");
            }
            else
            {
                DebugConsole.LogWarning("Could not find LocText in loading overlay.");
            }
        }
    }
}
