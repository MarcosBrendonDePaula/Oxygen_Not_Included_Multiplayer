using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Components;
using UnityEngine;

[HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.CreatePrefab))]
public static class MinionPatch
{
    public static void Postfix(GameObject __result)
    {
        var saveRoot = __result.GetComponent<SaveLoadRoot>();
        if (saveRoot != null)
        {
            //saveRoot.DeclareOptionalComponent<NetworkIdentity>();
            saveRoot.TryDeclareOptionalComponent<NetworkIdentity>();
            DebugConsole.Log($"[SaveLoadRoot] Declared optional component: {typeof(NetworkIdentity)}");
        }

        if (__result.GetComponent<NetworkIdentity>() == null)
        {
            __result.AddOrGet<NetworkIdentity>();
            DebugConsole.Log("[NetworkIdentity] Injected via MinionConfig.CreatePrefab");
        }
        __result.AddOrGet<EntityPositionHandler>();
    }
}
