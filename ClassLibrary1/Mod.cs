using HarmonyLib;
using ONI_MP.DebugTools;
using UnityEngine;

namespace ONI_MP
{
    // Ref: https://forums.kleientertainment.com/forums/topic/107833-tutorial-how-to-create-a-basic-mod-for-oni/
    // How to use Harmony https://github.com/pardeike/Harmony/wiki
    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public class Mod
    {
        public static void Postfix()
        {
            Debug.Log("Loaded Oxygen Not Included Multiplayer Mod. We have the steamworks library");
            ONI_MP.DebugTools.DebugMenu.Init();
        }

    }
}
