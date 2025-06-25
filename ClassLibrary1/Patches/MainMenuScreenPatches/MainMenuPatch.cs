using HarmonyLib;
using UnityEngine;
using JetBrains.Annotations;
using ONI_MP.Networking;
using Steamworks;
using System;
using System.Reflection;
using System.Linq;
using ONI_MP.Misc;
using UnityEngine.UI;

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

        InsertStaticBackground(__instance);
        UpdateLogo();
        UpdatePlacements(__instance);
        UpdatePromos();
        UpdateDLC();
        UpdateBuildNumber();
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

    private static void UpdateLogo()
    {
        // Attempt to find and replace the logo
        GameObject logoObj = GameObject.Find("Logo");
        if (logoObj != null)
        {
            var image = logoObj.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                Texture2D tex = ResourceLoader.LoadEmbeddedTexture("ONI_MP.Assets.oni_together_logo.png");
                if (tex != null)
                {
                    Sprite newSprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    image.sprite = newSprite;
                    Debug.Log("[ONI_MP] Replaced main menu logo with custom logo.");
                }
                else
                {
                    Debug.LogWarning("[ONI_MP] Failed to load embedded logo texture.");
                }
            }
            else
            {
                Debug.LogWarning("[ONI_MP] Logo GameObject found, but no Image component attached.");
            }
        }
        else
        {
            Debug.LogWarning("[ONI_MP] Could not find logo GameObject.");
        }

    }

    private static void InsertStaticBackground(MainMenu menu)
    {
        // Step 1: Find FrontEndBackground/mainmenu_border
        var border = menu.transform.Find("FrontEndBackground/mainmenu_border");
        if (border == null)
        {
            Debug.LogError("[ONI_MP] Could not find mainmenu_border.");
            return;
        }

        // Step 2: Load the embedded static background
        Texture2D bgTex = ResourceLoader.LoadEmbeddedTexture("ONI_MP.Assets.background-static.png");
        if (bgTex == null)
        {
            Debug.LogError("[ONI_MP] Failed to load static background texture.");
            return;
        }

        Sprite bgSprite = Sprite.Create(
            bgTex,
            new Rect(0, 0, bgTex.width, bgTex.height),
            new Vector2(0.5f, 0.5f),
            bgTex.width
        );

        // Step 3: Create the UI Image GameObject
        GameObject bgGO = new GameObject("ONI_MP_StaticBackground", typeof(UnityEngine.UI.Image));
        bgGO.transform.SetParent(border, false);

        var image = bgGO.GetComponent<UnityEngine.UI.Image>();
        image.sprite = bgSprite;
        image.preserveAspect = true;

        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Step 4: Place it as the last sibling (on top of other children under mainmenu_border)
        bgGO.transform.SetAsLastSibling();

        Debug.Log("[ONI_MP] Static background inserted as last sibling of mainmenu_border.");
    }

    private static void UpdatePromos()
    {
        GameObject topLeftColumns = GameObject.Find("TopLeftColumns");
        if (topLeftColumns == null)
        {
            Debug.LogWarning("[ONI_MP] TopLeftColumns not found.");
            return;
        }

        // Create container object under canvas root
        GameObject rootCanvas = GameObject.Find("Canvas");
        if (rootCanvas == null)
        {
            Debug.LogError("[ONI_MP] Root Canvas not found.");
            return;
        }

        GameObject promoContainer = new GameObject("ONI_MP_PromoContainer", typeof(RectTransform));
        promoContainer.transform.SetParent(rootCanvas.transform, false);

        // Manually place bottom-left
        RectTransform promoRect = promoContainer.GetComponent<RectTransform>();
        promoRect.anchorMin = new Vector2(0f, 0f);
        promoRect.anchorMax = new Vector2(0f, 0f);
        promoRect.pivot = new Vector2(0f, 0f);
        promoRect.anchoredPosition = new Vector2(30f, 30f); // bottom-left corner + margin
        promoRect.sizeDelta = new Vector2(1000f, 215f);     // wide enough for 3x banners

        // Remove layout system — manual positioning
        string[] motdNames = { "MOTDBox_A", "MOTDBox_B", "MOTDBox_C" };
        float bannerWidth = 300f;
        float bannerHeight = 215f;
        float spacing = 10f;

        for (int i = 0; i < motdNames.Length; i++)
        {
            Transform banner = topLeftColumns.transform.Find("MOTD/" + motdNames[i]);
            if (banner != null)
            {
                banner.SetParent(promoContainer.transform, false);

                RectTransform bannerRect = banner.GetComponent<RectTransform>();
                bannerRect.anchorMin = new Vector2(0f, 0f);
                bannerRect.anchorMax = new Vector2(0f, 0f);
                bannerRect.pivot = new Vector2(0f, 0f);
                bannerRect.sizeDelta = new Vector2(bannerWidth, bannerHeight);
                bannerRect.anchoredPosition = new Vector2((bannerWidth + spacing) * i, 0f);
            }
            else
            {
                Debug.LogWarning($"[ONI_MP] Could not find {motdNames[i]} under MOTD.");
            }
        }

        Debug.Log("[ONI_MP] Promo banners manually positioned at bottom-left.");
    }

    private static void UpdateDLC()
    {
        Transform dlcLogos = GameObject.Find("DLCLogos (1)")?.transform;
        Transform topLeft = GameObject.Find("TopLeftColumns")?.transform;

        if (dlcLogos == null || topLeft == null)
        {
            Debug.LogWarning("[ONI_MP] Could not find DLC logos or TopLeftColumns.");
            return;
        }

        dlcLogos.SetParent(topLeft, true);

        var rect = dlcLogos.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, 0f);
        rect.localScale = Vector3.one;

        dlcLogos.SetAsFirstSibling();
        Debug.Log("[ONI_MP] Raised DLC logos to better top-left position.");
    }

    private static void UpdateBuildNumber()
    {
        GameObject promoContainer = GameObject.Find("ONI_MP_PromoContainer");
        GameObject watermark = GameObject.Find("BuildWatermark");

        if (promoContainer == null)
        {
            Debug.LogWarning("[ONI_MP] Promo container not found. Cannot reposition build watermark.");
            return;
        }

        if (watermark == null)
        {
            Debug.LogWarning("[ONI_MP] BuildWatermark object not found.");
            return;
        }

        RectTransform promoRect = promoContainer.GetComponent<RectTransform>();
        RectTransform watermarkRect = watermark.GetComponent<RectTransform>();

        // Re-parent the watermark to the same parent as the promo container
        watermark.transform.SetParent(promoContainer.transform.parent, worldPositionStays: false);

        // Anchor it to bottom-left
        watermarkRect.anchorMin = new Vector2(0f, 0f);
        watermarkRect.anchorMax = new Vector2(0f, 0f);
        watermarkRect.pivot = new Vector2(0f, 0f);

        // Place it just above the DLC panels (which are 215 high)
        watermarkRect.anchoredPosition = new Vector2(30f, 260f);

        Debug.Log("[ONI_MP] BuildWatermark repositioned above promo panels.");
    }


}
