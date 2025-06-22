using KMod;
using UnityEngine;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Components;

namespace ONI_MP
{
    //Template: https://github.com/O-n-y/OxygenNotIncludedModTemplate

    public class MultiplayerMod : UserMod2
    {

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
            DebugConsole.Log("[ONI_MP] Loaded Oxygen Not Included Multiplayer Mod.");
        }

        private void SetupListeners()
        {
            App.OnPostLoadScene += () =>
            {
                OnPostSceneLoaded.Invoke();
            };
        }
    }
}
