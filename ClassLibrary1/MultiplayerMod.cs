using KMod;
using UnityEngine;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Components;
using System.Reflection;
using System.Collections.Generic;
using ONI_MP.Misc;

namespace ONI_MP
{
    //Template: https://github.com/O-n-y/OxygenNotIncludedModTemplate

    public class MultiplayerMod : UserMod2
    {

        public static readonly Dictionary<string, AssetBundle> LoadedBundles = new Dictionary<string, AssetBundle>();

        public static System.Action OnPostSceneLoaded;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            DebugMenu.Init();
            SteamLobby.Initialize();

            var go = new GameObject("Multiplayer_Modules");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<SteamNetworkingComponent>();
            go.AddComponent<UIVisibilityController>();
            go.AddComponent<MainThreadExecutor>();
            go.AddComponent<CursorManager>();
            SetupListeners();

            // Load custom asset bundles
            LoadAssetBundle("playercursorbundle", "ONI_MP.Assets.bundles.playercursor.bundle");
            
            DebugConsole.Log("[ONI_MP] Loaded Oxygen Not Included Together Multiplayer Mod.");

            foreach (var res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                DebugConsole.Log("Embedded Resource: " + res);
            }
        }

        private void SetupListeners()
        {
            App.OnPostLoadScene += () =>
            {
                OnPostSceneLoaded.Invoke();
            };
        }

        public static AssetBundle LoadAssetBundle(string bundleKey, string resourceName)
        {
            if (MultiplayerMod.LoadedBundles.TryGetValue(bundleKey, out var bundle))
            {
                DebugConsole.Log($"LoadAssetBundle: Reusing cached AssetBundle '{bundleKey}'.");
                return bundle;
            }

            // load with your existing loader
            bundle = ResourceLoader.LoadEmbeddedAssetBundle(resourceName);

            if (bundle != null)
            {
                MultiplayerMod.LoadedBundles[bundleKey] = bundle;
                DebugConsole.Log($"LoadAssetBundle: Successfully loaded AssetBundle '{bundleKey}' from resource '{resourceName}'.");
                return bundle;
            }
            else
            {
                DebugConsole.LogError($"LoadAssetBundle: Could not load AssetBundle from resource '{resourceName}'");
                return null;
            }
        }


    }
}
