using KMod;
using UnityEngine;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;

namespace ONI_MP
{
    //https://github.com/O-n-y/OxygenNotIncludedModTemplate

    public class MultiplayerMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            DebugMenu.Init();
            SteamLobby.Initialize();

            var go = new GameObject("Multiplayer_Modules");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<PingManager>();
            Debug.Log("[ONI_MP] Loaded Oxygen Not Included Multiplayer Mod.");
        }
    }
}
