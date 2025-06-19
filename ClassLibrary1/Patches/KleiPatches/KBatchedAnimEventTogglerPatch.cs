using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using UnityEngine;

[HarmonyPatch]
public static class KBatchedAnimEventToggler_NetworkPatch
{
    [HarmonyPatch(typeof(KBatchedAnimEventToggler), "Enable")]
    [HarmonyPrefix]
    private static void Prefix_Enable(KBatchedAnimEventToggler __instance, object data)
    {
        TrySendEffectPacket(__instance, true);
    }

    [HarmonyPatch(typeof(KBatchedAnimEventToggler), "Disable")]
    [HarmonyPrefix]
    private static void Prefix_Disable(KBatchedAnimEventToggler __instance, object data)
    {
        TrySendEffectPacket(__instance, false);
    }

    private static void TrySendEffectPacket(KBatchedAnimEventToggler toggler, bool enable)
    {
        if (!toggler.isActiveAndEnabled || toggler.eventSource == null)
            return;

        var identity = toggler.GetComponentInParent<NetworkIdentity>();
        if (identity == null)
            return;

        var handler = toggler.GetComponentInParent<AnimEventHandler>();
        if (handler == null)
            return;

        var context = handler.GetContext();
        if (!context.IsValid)
            return;

        var eventName = enable ? toggler.enableEvent : toggler.disableEvent;
        DuplicantPatch.ToggleEffect(identity.gameObject, eventName, context.ToString(), enable);
    }
}
