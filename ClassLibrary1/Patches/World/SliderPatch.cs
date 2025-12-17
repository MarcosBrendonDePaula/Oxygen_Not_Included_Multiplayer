using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Patches.World
{
	[HarmonyPatch(typeof(Valve), "ChangeFlow")]
	public static class ValveFlowPatch
	{
		public static void Postfix(Valve __instance, float amount)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.GetComponent<NetworkIdentity>();
			if (identity == null) return;

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				ConfigHash = "Rate".GetHashCode(),
				Value = amount
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	// Abstract classes or Interfaces cannot be patched directly easily.
	// We typically patch the implementations or base classes.
	// SingleSliderSideScreen invokes ISliderControl.SetSliderValue(val, index).
	// The implementations are diverse (Door, etc).
	// Let's try to patch known implementations if necessary, or check if we can patch via interface (Harmony doesn't support interface patch directly).
	// We should patch the methods that are CALLED by the UI.

	// Door has 'OpenValue' etc?
	// Door implements ISliderControl? No, usually not.
	// Let's check typical slider users: 
	// - Valve (Handled)
	// - IntSliderSideScreen targets?

	// For now, let's stick to Valve and Thresholds which cover 90% of user config needs.
}
