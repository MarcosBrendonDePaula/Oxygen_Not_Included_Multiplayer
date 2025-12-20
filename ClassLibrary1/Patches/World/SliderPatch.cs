using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Patches.World
{
	// Valve flow synchronization is now handled in BuildingConfigPatch.cs
	// This file serves as a placeholder for other specific slider implementations if needed.
	
	/*
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
	*/
}
