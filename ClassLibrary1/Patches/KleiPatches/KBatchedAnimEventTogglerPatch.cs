using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;

[HarmonyPatch]
public static class KBatchedAnimEventTogglerPatch
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

		if (!MultiplayerSession.IsHost)
			return;

		ONI_MP.DebugTools.DebugConsole.Log($"[KBatchedAnimEventToggler] TrySendEffectPacket enable={enable}");

		var identity = toggler.GetComponentInParent<NetworkIdentity>();
		if (identity == null)
			return;

		var handler = toggler.GetComponentInParent<AnimEventHandler>();
		if (handler == null)
			return;

		try
		{
			ONI_MP.DebugTools.DebugConsole.Log("[KBatchedAnimEventToggler] Getting context");
			var context = handler.GetContext();
			ONI_MP.DebugTools.DebugConsole.Log("[KBatchedAnimEventToggler] Got context, checking validity");
			if (!context.IsValid)
				return;

			ONI_MP.DebugTools.DebugConsole.Log("[KBatchedAnimEventToggler] Context valid, calling ToString");
			string contextStr = context.ToString();
			ONI_MP.DebugTools.DebugConsole.Log($"[KBatchedAnimEventToggler] ToString done: {contextStr}");
			if (string.IsNullOrEmpty(contextStr))
				return;

			var eventName = enable ? toggler.enableEvent : toggler.disableEvent;
			ONI_MP.DebugTools.DebugConsole.Log($"[KBatchedAnimEventToggler] Calling ToggleEffect: {eventName}");
			DuplicantPatch.ToggleEffect(identity.gameObject, eventName, contextStr, enable);
			ONI_MP.DebugTools.DebugConsole.Log("[KBatchedAnimEventToggler] TrySendEffectPacket END");
		}
		catch (System.Exception ex)
		{
			ONI_MP.DebugTools.DebugConsole.LogError($"[KBatchedAnimEventToggler] Exception: {ex}");
		}
	}
}
