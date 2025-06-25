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

            // Create a new overlay canvas with high sort order
            GameObject canvasObject = new GameObject("ModCompatibilityDialog_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Very high to ensure it's on top
            
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            var raycaster = canvasObject.GetComponent<GraphicRaycaster>();

            // Create base dialog container
            GameObject dialog = new GameObject("ModCompatibility_Dialog", typeof(RectTransform), typeof(CanvasGroup));
            currentDialog = canvasObject; // Store the canvas as the current dialog for cleanup

            dialog.transform.SetParent(canvas.transform, worldPositionStays: false);

            var rt = dialog.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(650, 550);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var canvasGroup = dialog.GetComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // Add fullscreen backdrop
            AddBackdrop(canvas.transform);
            
            // Add background
            AddBackground(dialog.transform);

            // Add title
            AddTitle(dialog.transform, "Mod Compatibility Issue");

            // Add description with more detailed information
            string statusMessage = GetCompatibilityMessage(missingMods);
            AddDescription(dialog.transform, statusMessage);

            // Add scrollable mod list
            AddModList(dialog.transform, missingMods);

            // Add action buttons
            AddDialogButton(dialog.transform, "Subscribe All", new Vector2(-80, -230), () =>
            {
                foreach (var mod in missingMods)
                {
                    if (ONI_MP.Mods.ModLoader.SubscribeToWorkshopMod(mod.Id))
                    {
                        ONI_MP.Mods.ModLoader.SetModEnabled(mod.Id, true);
                    }
                }
                DebugConsole.Log("[ModCompatibilityDialog] Attempted to subscribe and enable all missing mods. Please restart the game.");
            });

            AddDialogButton(dialog.transform, "Cancel", new Vector2(80, -230), () =>
            {
                Close();
            });
        }

        private static void AddBackdrop(Transform parent)
        {
            // Full screen backdrop - lighter and allows interaction with dialog
            GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(parent, worldPositionStays: false);

            var rt = backdrop.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = backdrop.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.3f); // Much lighter backdrop
            img.raycastTarget = false; // Allow clicks to pass through
        }

        private static void AddBackground(Transform parent)
        {
            // Main background with rounded appearance using multiple layers
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(parent, worldPositionStays: false);

            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = bg.GetComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.15f, 0.98f); // Dark blue-grey background
            
            // Outer border (shadow effect)
            GameObject outerBorder = new GameObject("OuterBorder", typeof(RectTransform), typeof(Image));
            outerBorder.transform.SetParent(parent, worldPositionStays: false);
            outerBorder.transform.SetSiblingIndex(0); // Behind main background
            
            var outerRt = outerBorder.GetComponent<RectTransform>();
            outerRt.anchorMin = Vector2.zero;
            outerRt.anchorMax = Vector2.one;
            outerRt.offsetMin = new Vector2(-4, -4);
            outerRt.offsetMax = new Vector2(4, 4);
            
            var outerImg = outerBorder.GetComponent<Image>();
            outerImg.color = new Color(0f, 0f, 0f, 0.6f); // Shadow
            
            // Main border
            GameObject mainBorder = new GameObject("MainBorder", typeof(RectTransform), typeof(Image));
            mainBorder.transform.SetParent(bg.transform, worldPositionStays: false);
            
            var borderRt = mainBorder.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-3, -3);
            borderRt.offsetMax = new Vector2(3, 3);
            
            var borderImg = mainBorder.GetComponent<Image>();
            borderImg.color = new Color(0.2f, 0.4f, 0.6f, 1f); // Blue border
            
            // Inner highlight border
            GameObject innerBorder = new GameObject("InnerBorder", typeof(RectTransform), typeof(Image));
            innerBorder.transform.SetParent(bg.transform, worldPositionStays: false);
            
            var innerRt = innerBorder.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(-1, -1);
            innerRt.offsetMax = new Vector2(1, 1);
            
            var innerImg = innerBorder.GetComponent<Image>();
            innerImg.color = new Color(0.4f, 0.6f, 0.8f, 0.7f); // Lighter blue highlight
            
            // Top gradient for modern look
            GameObject topGradient = new GameObject("TopGradient", typeof(RectTransform), typeof(Image));
            topGradient.transform.SetParent(bg.transform, worldPositionStays: false);
            
            var gradientRt = topGradient.GetComponent<RectTransform>();
            gradientRt.anchorMin = new Vector2(0, 0.7f);
            gradientRt.anchorMax = new Vector2(1, 1);
            gradientRt.offsetMin = Vector2.zero;
            gradientRt.offsetMax = Vector2.zero;
            
            var gradientImg = topGradient.GetComponent<Image>();
            gradientImg.color = new Color(0.2f, 0.3f, 0.4f, 0.3f); // Subtle top gradient
        }

        private static void AddTitle(Transform parent, string titleText)
        {
            GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(parent, worldPositionStays: false);

            var rt = title.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(550, 40);
            rt.anchoredPosition = new Vector2(0, 200);

            var text = title.GetComponent<Text>();
            text.text = titleText;
            text.fontSize = 20;
            text.color = new Color(1f, 0.9f, 0.7f, 1f); // Warm white
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
        }

        private static void AddDescription(Transform parent, string descText)
        {
            GameObject desc = new GameObject("Description", typeof(RectTransform), typeof(Text));
            desc.transform.SetParent(parent, worldPositionStays: false);

            var rt = desc.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(550, 80);
            rt.anchoredPosition = new Vector2(0, 130);

            var text = desc.GetComponent<Text>();
            text.text = descText;
            text.fontSize = 14;
            text.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            text.alignment = TextAnchor.UpperCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.raycastTarget = false;
        }

        private static void AddModList(Transform parent, List<ModCompatibilityStatusPacket.MissingModInfo> missingMods)
        {
            // Create scroll view container
            GameObject scrollViewContainer = new GameObject("ModListContainer", typeof(RectTransform), typeof(ScrollRect));
            scrollViewContainer.transform.SetParent(parent, worldPositionStays: false);

            var containerRt = scrollViewContainer.GetComponent<RectTransform>();
            containerRt.sizeDelta = new Vector2(550, 180);
            containerRt.anchoredPosition = new Vector2(0, 20);

            // Add background for the list
            GameObject listBg = new GameObject("ListBackground", typeof(RectTransform), typeof(Image));
            listBg.transform.SetParent(scrollViewContainer.transform, worldPositionStays: false);

            var listBgRt = listBg.GetComponent<RectTransform>();
            listBgRt.anchorMin = Vector2.zero;
            listBgRt.anchorMax = Vector2.one;
            listBgRt.offsetMin = Vector2.zero;
            listBgRt.offsetMax = Vector2.zero;

            var listBgImg = listBg.GetComponent<Image>();
            listBgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.8f);

            // Create viewport for scrolling
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollViewContainer.transform, worldPositionStays: false);

            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(5, 5);
            viewportRt.offsetMax = new Vector2(-5, -5);

            var viewportImg = viewport.GetComponent<Image>();
            viewportImg.color = Color.clear;

            var mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            // Create content container for mods
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, worldPositionStays: false);

            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchoredPosition = Vector2.zero;
            
            // Calculate content height based on number of mods (65px per mod)
            float contentHeight = Mathf.Max(170, missingMods.Count * 65 + 10);
            contentRt.sizeDelta = new Vector2(0, contentHeight);

            // Setup ScrollRect
            var scrollRect = scrollViewContainer.GetComponent<ScrollRect>();
            scrollRect.content = contentRt;
            scrollRect.viewport = viewportRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20;

            float yPos = -10;
            for (int i = 0; i < missingMods.Count; i++)
            {
                var mod = missingMods[i];
                
                // Create mod item container
                GameObject modItem = new GameObject($"Mod_{mod.Id}", typeof(RectTransform), typeof(Image));
                modItem.transform.SetParent(content.transform, worldPositionStays: false);

                var itemRt = modItem.GetComponent<RectTransform>();
                itemRt.sizeDelta = new Vector2(-10, 60);
                itemRt.anchorMin = new Vector2(0, 1);
                itemRt.anchorMax = new Vector2(1, 1);
                itemRt.pivot = new Vector2(0.5f, 1);
                itemRt.anchoredPosition = new Vector2(0, yPos);

                var itemImg = modItem.GetComponent<Image>();
                itemImg.color = i % 2 == 0 ? new Color(0.15f, 0.15f, 0.2f, 0.6f) : new Color(0.2f, 0.2f, 0.25f, 0.6f);

                // Mod icon/status indicator
                GameObject statusIcon = new GameObject("StatusIcon", typeof(RectTransform), typeof(Image));
                statusIcon.transform.SetParent(modItem.transform, worldPositionStays: false);

                var iconRt = statusIcon.GetComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(12, 12);
                iconRt.anchoredPosition = new Vector2(-250, 15);

                var iconImg = statusIcon.GetComponent<Image>();
                iconImg.color = new Color(1f, 0.6f, 0.2f, 1f); // Orange warning

                // Add mod name (main text)
                GameObject nameText = new GameObject("ModName", typeof(RectTransform), typeof(Text));
                nameText.transform.SetParent(modItem.transform, worldPositionStays: false);

                var nameRt = nameText.GetComponent<RectTransform>();
                nameRt.sizeDelta = new Vector2(300, 18);
                nameRt.anchoredPosition = new Vector2(-120, 15);

                var nameTextComp = nameText.GetComponent<Text>();
                nameTextComp.text = mod.Name ?? mod.Id;
                nameTextComp.fontSize = 13;
                nameTextComp.color = new Color(1f, 0.95f, 0.8f, 1f);
                nameTextComp.alignment = TextAnchor.MiddleLeft;
                nameTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                nameTextComp.fontStyle = FontStyle.Bold;
                nameTextComp.raycastTarget = false;

                // Add mod details (version)
                GameObject versionText = new GameObject("Version", typeof(RectTransform), typeof(Text));
                versionText.transform.SetParent(modItem.transform, worldPositionStays: false);

                var versionRt = versionText.GetComponent<RectTransform>();
                versionRt.sizeDelta = new Vector2(300, 14);
                versionRt.anchoredPosition = new Vector2(-120, -2);

                var versionTextComp = versionText.GetComponent<Text>();
                versionTextComp.text = $"Required: v{mod.Version}";
                versionTextComp.fontSize = 10;
                versionTextComp.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                versionTextComp.alignment = TextAnchor.MiddleLeft;
                versionTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                versionTextComp.raycastTarget = false;

                // Add mod ID
                GameObject idText = new GameObject("ModID", typeof(RectTransform), typeof(Text));
                idText.transform.SetParent(modItem.transform, worldPositionStays: false);

                var idRt = idText.GetComponent<RectTransform>();
                idRt.sizeDelta = new Vector2(300, 12);
                idRt.anchoredPosition = new Vector2(-120, -15);

                var idTextComp = idText.GetComponent<Text>();
                idTextComp.text = $"ID: {mod.Id}";
                idTextComp.fontSize = 9;
                idTextComp.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                idTextComp.alignment = TextAnchor.MiddleLeft;
                idTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                idTextComp.raycastTarget = false;

                // Add individual action buttons
                AddModActionButton(modItem.transform, "View", new Vector2(180, 10), 65, 25, () =>
                {
                    if (!string.IsNullOrEmpty(mod.SteamWorkshopUrl))
                    {
                        Application.OpenURL(mod.SteamWorkshopUrl);
                        DebugConsole.Log($"[ModCompatibilityDialog] Opening workshop page for mod: {mod.Name ?? mod.Id}");
                    }
                });

                AddModActionButton(modItem.transform, "Install", new Vector2(180, -15), 65, 25, () =>
                {
                    if (ONI_MP.Mods.ModLoader.SubscribeToWorkshopMod(mod.Id))
                    {
                        ONI_MP.Mods.ModLoader.SetModEnabled(mod.Id, true);
                        DebugConsole.Log($"[ModCompatibilityDialog] Subscribed and enabled mod: {mod.Name ?? mod.Id}");
                    }
                });

                yPos -= 65;
            }
        }

        private static void AddModActionButton(Transform parent, string text, Vector2 position, float width, float height, System.Action onClick)
        {
            // Create compact button for mod actions
            GameObject btnGO = new GameObject($"ModBtn_{text}", typeof(RectTransform), typeof(Button), typeof(Image));
            btnGO.transform.SetParent(parent, worldPositionStays: false);

            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = position;

            var img = btnGO.GetComponent<Image>();
            img.color = text == "View" ? new Color(0.2f, 0.5f, 0.3f, 1f) : new Color(0.4f, 0.3f, 0.6f, 1f);

            var btn = btnGO.GetComponent<Button>();
            
            // Set up color transitions
            var colors = btn.colors;
            if (text == "View")
            {
                colors.normalColor = new Color(0.2f, 0.5f, 0.3f, 1f);
                colors.highlightedColor = new Color(0.3f, 0.6f, 0.4f, 1f);
                colors.pressedColor = new Color(0.1f, 0.4f, 0.2f, 1f);
            }
            else
            {
                colors.normalColor = new Color(0.4f, 0.3f, 0.6f, 1f);
                colors.highlightedColor = new Color(0.5f, 0.4f, 0.7f, 1f);
                colors.pressedColor = new Color(0.3f, 0.2f, 0.5f, 1f);
            }
            btn.colors = colors;
            
            btn.onClick.AddListener(() => {
                DebugConsole.Log($"[ModCompatibilityDialog] Mod action '{text}' clicked.");
                onClick();
            });

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
            btnText.fontSize = 11;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontStyle = FontStyle.Bold;
            btnText.raycastTarget = false;
        }

        private static void AddDialogButton(Transform parent, string text, Vector2 position, System.Action onClick)
        {
            // Create button container with shadow
            GameObject btnContainer = new GameObject($"BtnContainer_{text.Replace(" ", "")}", typeof(RectTransform));
            btnContainer.transform.SetParent(parent, worldPositionStays: false);

            var containerRt = btnContainer.GetComponent<RectTransform>();
            containerRt.sizeDelta = new Vector2(160, 45);
            containerRt.anchoredPosition = position;

            // Button shadow
            GameObject btnShadow = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            btnShadow.transform.SetParent(btnContainer.transform, worldPositionStays: false);

            var shadowRt = btnShadow.GetComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(2, -2);
            shadowRt.offsetMax = new Vector2(2, -2);

            var shadowImg = btnShadow.GetComponent<Image>();
            shadowImg.color = new Color(0f, 0f, 0f, 0.4f);

            // Main button
            GameObject btnGO = new GameObject("Button", typeof(RectTransform), typeof(Button), typeof(Image));
            btnGO.transform.SetParent(btnContainer.transform, worldPositionStays: false);

            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = btnGO.GetComponent<Image>();
            img.color = new Color(0.25f, 0.45f, 0.65f, 1f);

            // Button border
            GameObject btnBorder = new GameObject("Border", typeof(RectTransform), typeof(Image));
            btnBorder.transform.SetParent(btnGO.transform, worldPositionStays: false);

            var borderRt = btnBorder.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2, -2);
            borderRt.offsetMax = new Vector2(2, 2);

            var borderImg = btnBorder.GetComponent<Image>();
            borderImg.color = new Color(0.4f, 0.6f, 0.8f, 1f);

            var btn = btnGO.GetComponent<Button>();
            
            // Set up color transitions for better visual feedback
            var colors = btn.colors;
            colors.normalColor = new Color(0.25f, 0.45f, 0.65f, 1f);
            colors.highlightedColor = new Color(0.35f, 0.55f, 0.75f, 1f);
            colors.pressedColor = new Color(0.15f, 0.35f, 0.55f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;
            
            btn.onClick.AddListener(() => {
                DebugConsole.Log($"[ModCompatibilityDialog] Button '{text}' clicked.");
                onClick();
            });

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
            btnText.fontSize = 14;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontStyle = FontStyle.Bold;
            
            // Ensure text doesn't block raycasts
            btnText.raycastTarget = false;

            // Add text shadow for better readability
            GameObject textShadow = new GameObject("TextShadow", typeof(RectTransform), typeof(Text));
            textShadow.transform.SetParent(btnGO.transform, worldPositionStays: false);

            var textShadowRt = textShadow.GetComponent<RectTransform>();
            textShadowRt.anchorMin = Vector2.zero;
            textShadowRt.anchorMax = Vector2.one;
            textShadowRt.offsetMin = new Vector2(1, -1);
            textShadowRt.offsetMax = new Vector2(1, -1);

            var shadowText = textShadow.GetComponent<Text>();
            shadowText.text = text;
            shadowText.fontSize = 14;
            shadowText.color = new Color(0f, 0f, 0f, 0.5f);
            shadowText.alignment = TextAnchor.MiddleCenter;
            shadowText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            shadowText.fontStyle = FontStyle.Bold;
            shadowText.raycastTarget = false;

            // Make sure text shadow is behind main text
            textShadow.transform.SetSiblingIndex(0);
        }

        private static string GetCompatibilityMessage(List<ModCompatibilityStatusPacket.MissingModInfo> missingMods)
        {
            if (missingMods == null || missingMods.Count == 0)
                return "No mod compatibility issues detected.";

            int totalMods = missingMods.Count;
            
            if (totalMods == 1)
            {
                return $"The server requires 1 mod that you don't have or need to update:";
            }
            else
            {
                return $"The server requires {totalMods} mods that you don't have or need to update:\n" +
                       $"Please install or update the missing mods to join this server.";
            }
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
