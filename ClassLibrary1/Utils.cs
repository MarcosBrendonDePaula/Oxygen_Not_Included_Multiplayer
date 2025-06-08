using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONI_MP
{
    public static class Utils
    {
        /// <summary>
        /// Recursively logs the hierarchy of a GameObject and its children.
        /// </summary>
        /// <param name="root">The root transform to start logging from.</param>
        /// <param name="prefix">Used internally for indentation.</param>
        public static void LogHierarchy(Transform root, string prefix = "")
        {
            if (root == null)
            {
                DebugConsole.LogWarning("LogHierarchy called with null root.");
                return;
            }

            DebugConsole.Log($"{prefix}{root.name}");

            foreach (Transform child in root)
            {
                LogHierarchy(child, prefix + "  ");
            }
        }

        /// <summary>
        /// Inject and add a component to the prefab gameobject
        /// </summary>
        /// <typeparam name="T">The type of Component you wish to inject</typeparam>
        /// <param name="prefab">The GameObject the component is to be added too</param>
        public static void Inject<T>(GameObject prefab) where T : KMonoBehaviour
        {
            if (prefab.GetComponent<T>() == null)
            {
                DebugConsole.Log($"Added {typeof(T).Name} to {prefab.name}");
                prefab.AddOrGet<T>();
            }
        }

        public static void ListAllTMPFonts()
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            DebugConsole.Log($"Found {fonts.Length} TMP_FontAsset(s):");

            foreach (var font in fonts)
            {
                DebugConsole.Log($" - {font.name} (path: {font.name}, instance ID: {font.GetInstanceID()})");
            }

            if (fonts.Length == 0)
            {
                DebugConsole.Log("No TMP_FontAsset found in memory.");
            }
        }

        public static TMP_FontAsset GetDefaultTMPFont()
        {
            var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(f => f.name == "NotoSans-Regular");

            if (font == null)
            {
                DebugConsole.Log("[ONI_MP] Fallback: NotoSans-Regular not found. Attempting to use any available font.");

                var fallback = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
                if (fallback != null)
                {
                    DebugConsole.Log($"[ONI_MP] Using fallback font: {fallback.name}");
                    return fallback;
                }

                DebugConsole.Log("[ONI_MP] ERROR: No TMP_FontAsset found at all.");
                return null;
            }

            return font;
        }



    }
}
