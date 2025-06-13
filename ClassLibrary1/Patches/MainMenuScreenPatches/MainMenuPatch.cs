using HarmonyLib;
using UnityEngine;
using JetBrains.Annotations;
using ONI_MP.Networking;
using Steamworks;
using System;
using System.Reflection;
using System.Linq;

[HarmonyPatch(typeof(MainMenu), "OnPrefabInit")]
internal static class MainMenuPatch
{
    private static void Postfix(MainMenu __instance)
    {
        int normalFontSize = 20;
        var normalStyle = Traverse.Create(__instance).Field("normalButtonStyle").GetValue<ColorStyleSetting>();

        var buttonInfoType = __instance.GetType().GetNestedType("ButtonInfo", BindingFlags.NonPublic);

        var makeButton = __instance.GetType().GetMethod("MakeButton", BindingFlags.NonPublic | BindingFlags.Instance);

        // Host Game
        var hostInfo = CreateButtonInfo(
            "Host Game",
            new System.Action(() => {
                MultiplayerSession.ShouldHostAfterLoad = true;
                __instance.Button_ResumeGame.SignalClick(KKeyCode.Mouse0);
            }),
            normalFontSize,
            normalStyle,
            buttonInfoType
        );
        makeButton.Invoke(__instance, new object[] { hostInfo });

        // Join Game
        var joinInfo = CreateButtonInfo(
            "Join Game",
            new System.Action(() => {
                SteamFriends.ActivateGameOverlay("friends");
            }),
            normalFontSize,
            normalStyle,
            buttonInfoType
        );
        makeButton.Invoke(__instance, new object[] { joinInfo });

        UpdatePlacements(__instance);
    }

    // Reflection utility to build ButtonInfo struct
    private static object CreateButtonInfo(string text, System.Action action, int fontSize, ColorStyleSetting style, Type buttonInfoType)
    {
        var buttonInfo = Activator.CreateInstance(buttonInfoType);
        buttonInfoType.GetField("text").SetValue(buttonInfo, new LocString(text));
        buttonInfoType.GetField("action").SetValue(buttonInfo, action);
        buttonInfoType.GetField("fontSize").SetValue(buttonInfo, fontSize);
        buttonInfoType.GetField("style").SetValue(buttonInfo, style);
        return buttonInfo;
    }

    private static void UpdatePlacements(MainMenu __instance)
    {
        var buttonParent = Traverse.Create(__instance).Field("buttonParent").GetValue<GameObject>();
        if (buttonParent != null)
        {
            var children = buttonParent.GetComponentsInChildren<KButton>(true);

            // Find "Load Game" button
            var loadGameBtn = children.FirstOrDefault(b =>
                b.GetComponentInChildren<LocText>().text.ToUpper().Contains("LOAD GAME"));

            // Find your buttons
            var hostBtn = children.FirstOrDefault(b =>
                b.GetComponentInChildren<LocText>().text.ToUpper().Contains("HOST GAME"));
            var joinBtn = children.FirstOrDefault(b =>
                b.GetComponentInChildren<LocText>().text.ToUpper().Contains("JOIN GAME"));

            if (loadGameBtn != null && hostBtn != null && joinBtn != null)
            {
                int loadGameIdx = loadGameBtn.transform.GetSiblingIndex();
                // Move host and join immediately after "Load Game"
                hostBtn.transform.SetSiblingIndex(loadGameIdx + 1);
                joinBtn.transform.SetSiblingIndex(loadGameIdx + 2);
            }
        }
    }
}
