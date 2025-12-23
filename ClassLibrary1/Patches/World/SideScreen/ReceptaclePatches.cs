using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Patches.World.SideScreen
{
	/// <summary>
	/// Patches for SingleEntityReceptacle synchronization (planters, incubators selecting items)
	/// </summary>

	[HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.CreateOrder))]
	public static class SingleEntityReceptacle_CreateOrder_Patch
	{
		public static void Postfix(SingleEntityReceptacle __instance, Tag entityTag, Tag additionalFilterTag)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packetEntity = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "ReceptacleEntityTag".GetHashCode(),
				Value = 0,
				ConfigType = BuildingConfigType.String,
				StringValue = entityTag.IsValid ? entityTag.Name : ""
			};

			var packetFilter = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "ReceptacleFilterTag".GetHashCode(),
				Value = 0,
				ConfigType = BuildingConfigType.String,
				StringValue = additionalFilterTag.IsValid ? additionalFilterTag.Name : ""
			};

			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(packetEntity);
				PacketSender.SendToAllClients(packetFilter);
			}
			else
			{
				PacketSender.SendToHost(packetEntity);
				PacketSender.SendToHost(packetFilter);
			}
		}
	}

	[HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.CancelActiveRequest))]
	public static class SingleEntityReceptacle_CancelActiveRequest_Patch
	{
		public static void Postfix(SingleEntityReceptacle __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "ReceptacleCancelRequest".GetHashCode(),
				Value = 1f,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}
}
