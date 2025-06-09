using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.World;
using UnityEngine;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(Game), "Update")]
    public static class GamePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (MultiplayerSession.IsHost)
            {
                WorldUpdateBatcher.Update(Time.deltaTime);
            }
        }
    }
}
