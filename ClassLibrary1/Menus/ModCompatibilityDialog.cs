using System.Collections.Generic;
using Klei;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace ONI_MP.Menus
{
    public static class ModCompatibilityDialog
    {
        private static GameObject currentDialog;

        public static void ShowMissingMods(List<ModCompatibilityStatusPacket.MissingModInfo> missingMods)
        {
            if (currentDialog != null)
            {
                DebugConsole.Log("[ModCompatibilityDialog] Dialog already open.");
                return;
            }

            // Find a suitable parent (main canvas)
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                DebugConsole.LogError("[ModCompatibilityDialog] Cannot find canvas to show dialog.");
                return;
            }

            // Create base dialog container
            GameObject dialog = new GameObject("ModCompatibility_Dialog", typeof(RectTransform), typeof(CanvasGroup));
            currentDialog = dialog;

            dialog.transform.SetParent(canvas.transform, worldPositionStays: false);

            var rt = dialog.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 400);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var canvasGroup = dialog.GetComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // Add background
            AddBackground(dialog.transform);

            // Add title
            AddTitle(dialog.transform, "Mod Compatibility Issue");

            // Add description
            AddDescription(dialog.transform, $"The server requires {missingMods.Count} mod(s) that you don't have or need to update:");

            // Add scrollable mod list
            AddModList(dialog.transform, missingMods);

            // Add buttons
            AddDialogButton(dialog.transform, "Open Steam Workshop", new Vector2(-100, -150), () =>
            {
                foreach (var mod in missingMods)
                {
                    if (!string.IsNullOrEmpty(mod.SteamWorkshopUrl))
                    {
                        Application.OpenURL(mod.SteamWorkshopUrl);
                    }
                }
            });

            AddDialogButton(dialog.transform, "Try Auto-Subscribe", new Vector2(0, -150), () =>
            {
                foreach (var mod in missingMods)
                {
                    ONI_MP.Mods.ModLoader.SubscribeToWorkshopMod(mod.Id);
                }
                DebugConsole.Log("[ModCompatibilityDialog] Attempted to subscribe to missing mods. Please restart the game.");
            });

            AddDialogButton(dialog.transform, "Cancel", new Vector2(100, -150), () =>
            {
                Close();
            });
        }

        private static void AddBackground(Transform parent)
        {
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(parent, worldPositionStays: false);

            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = bg.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        }

        private static void AddTitle(Transform parent, string titleText)
        {
            GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(parent, worldPositionStays: false);

            var rt = title.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(450, 40);
            rt.anchoredPosition = new Vector2(0, 150);

            var text = title.GetComponent<Text>();
            text.text = titleText;
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void AddDescription(Transform parent, string descText)
        {
            GameObject desc = new GameObject("Description", typeof(RectTransform), typeof(Text));
            desc.transform.SetParent(parent, worldPositionStays: false);

            var rt = desc.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(450, 60);
            rt.anchoredPosition = new Vector2(0, 100);

            var text = desc.GetComponent<Text>();
            text.text = descText;
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void AddModList(Transform parent, List<ModCompatibilityStatusPacket.MissingModInfo> missingMods)
        {
            GameObject scrollArea = new GameObject("ModList", typeof(RectTransform));
            scrollArea.transform.SetParent(parent, worldPositionStays: false);

            var rt = scrollArea.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(450, 120);
            rt.anchoredPosition = new Vector2(0, 20);

            float yPos = 50;
            foreach (var mod in missingMods)
            {
                GameObject modItem = new GameObject($"Mod_{mod.Id}", typeof(RectTransform), typeof(Text));
                modItem.transform.SetParent(scrollArea.transform, worldPositionStays: false);

                var itemRt = modItem.GetComponent<RectTransform>();
                itemRt.sizeDelta = new Vector2(440, 25);
                itemRt.anchoredPosition = new Vector2(0, yPos);

                var itemText = modItem.GetComponent<Text>();
                itemText.text = $"• {mod.Id} (v{mod.Version})";
                itemText.fontSize = 12;
                itemText.color = Color.yellow;
                itemText.alignment = TextAnchor.MiddleLeft;
                itemText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                yPos -= 30;
            }
        }

        private static void AddDialogButton(Transform parent, string text, Vector2 position, System.Action onClick)
        {
            GameObject btnGO = new GameObject($"Btn_{text.Replace(" ", "")}", typeof(RectTransform), typeof(Button), typeof(Image));
            btnGO.transform.SetParent(parent, worldPositionStays: false);

            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 30);
            rt.anchoredPosition = position;

            var img = btnGO.GetComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            // Add text to button
            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(btnGO.transform, worldPositionStays: false);

            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var btnText = textGO.GetComponent<Text>();
            btnText.text = text;
            btnText.fontSize = 12;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void Close()
        {
            if (currentDialog != null)
            {
                Object.Destroy(currentDialog);
                currentDialog = null;
            }
        }
    }
}
