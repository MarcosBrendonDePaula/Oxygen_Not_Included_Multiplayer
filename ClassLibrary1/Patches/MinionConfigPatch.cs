using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using UnityEngine;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.CreatePrefab))]
    public static class MinionConfig_CreatePrefab_Patch
    {
        public static void Postfix(GameObject __result)
        {
            if (__result.GetComponent<NetworkedEntityComponent>() == null)
            {
                DebugConsole.Log($"Added networked entity component to {__result.name}");
                __result.AddOrGet<NetworkedEntityComponent>();
            }
        }
    }

}
