using System;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UI;

namespace ONI_MP.Menus
{
    public static class MultiplayerMenu
    {
        public static void Show()
        {
            var parent = FrontEndManager.Instance.gameObject;
            var original = ScreenPrefabs.Instance.OptionsScreen;
            if (original == null)
            {
                Debug.LogError("[ONI_MP] Could not find OptionsScreen prefab");
                return;
            }

            // ✅ FIX: Instantiate the GameObject, NOT the component
            GameObject instance = UnityEngine.Object.Instantiate(original.gameObject, parent.transform);
            instance.name = "MP_MultiplayerMenu";

            // Change the menu title
            var titleText = instance.transform.Find("Title")?.GetComponent<LocText>();
            if (titleText != null)
                titleText.text = "MULTIPLAYER";

            // Find button container

            Debug.Log("Logging hierarchy");
            Utils.LogHierarchy(instance.transform);
            var buttonArea = instance.transform.Find("Contents")?.Find("ScrollContents");
            if (buttonArea == null)
            {
                Debug.LogError("[ONI_MP] Could not find ScrollContents");
                return;
            }

            // Remove all existing option buttons
            foreach (Transform child in buttonArea)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }

            // Add your custom buttons
            AddButton(buttonArea, "Find Game", () => Debug.Log("[ONI_MP] Find Game clicked"));
            AddButton(buttonArea, "Host New Game", () => Debug.Log("[ONI_MP] Host New clicked"));
            AddButton(buttonArea, "Host Load Game", () => Debug.Log("[ONI_MP] Host Load clicked"));
            AddButton(buttonArea, "Join Game", () => Debug.Log("[ONI_MP] Join Game clicked"));

            Debug.Log("[ONI_MP] Multiplayer menu shown.");
        }

        private static void AddButton(Transform parent, string text, System.Action onClick)
        {
            // ✅ FIX: Clone the GameObject of the first existing button
            var templateButton = ScreenPrefabs.Instance.OptionsScreen
                .gameObject.transform.Find("Contents/ScrollContents")?.GetChild(0)?.gameObject;

            if (templateButton == null)
            {
                Debug.LogError("[ONI_MP] Failed to find template button");
                return;
            }

            GameObject buttonGO = UnityEngine.Object.Instantiate(templateButton, parent);
            buttonGO.name = $"MP_{text.Replace(" ", "")}_Button";

            var label = buttonGO.GetComponentInChildren<LocText>();
            if (label != null)
                label.text = text;

            var btn = buttonGO.GetComponent<KButton>();
            btn.onClick += () => onClick();
        }
    }
}
