using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using UnityEngine;
using Utils = ONI_MP.Misc.Utils;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.CreatePrefab))]
    public static class MinionConfig_CreatePrefab_Patch
    {
        public static void Postfix(GameObject __result)
        {
            Utils.Inject<NetworkIdentity>(__result);
            //Utils.Inject<EntityPositionSender>(__result);
        }
    }

}
