using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.DuplicantActions;

namespace ONI_MP.Patches.DuplicantActions
{
	[HarmonyPatch(typeof(ConsumableConsumer), "SetPermitted")]
	public static class ConsumableConsumerPatch
	{
		[HarmonyPatch(typeof(ConsumableConsumer), "OnSpawn")]
		public static class OnSpawnPatch
		{
			public static void Postfix(ConsumableConsumer __instance)
			{
				if (!MultiplayerSession.IsHost) return;

				// Send full state to ensure defaults are synced
				// Using UnityTaskScheduler to delay slightly? No, might not be needed.
				var identity = __instance.GetComponent<NetworkIdentity>();
				if (identity != null)
				{
					var forbiddenObj = HarmonyLib.Traverse.Create(__instance).Field("forbiddenTags").GetValue();
					if (forbiddenObj != null && forbiddenObj is System.Collections.IEnumerable enumerable)
					{
						var packet = new ONI_MP.Networking.Packets.DuplicantActions.ConsumableStatePacket
						{
							NetId = identity.NetId,
							ForbiddenIds = new System.Collections.Generic.List<string>()
						};
						foreach (var item in enumerable)
						{
							if (item is Tag tag)
							{
								packet.ForbiddenIds.Add(tag.Name);
							}
						}
						if (packet.ForbiddenIds.Count > 0)
						{
							PacketSender.SendToAllClients(packet);
						}
					}
				}
			}
		}

		public static void Postfix(ConsumableConsumer __instance, string consumable_id, bool is_allowed)
		{
			if (!MultiplayerSession.InSession) return;
			if (ConsumablePermissionPacket.IsApplying) return;

			var identity = __instance.GetComponent<NetworkIdentity>();
			if (identity != null)
			{
				var packet = new ConsumablePermissionPacket
				{
					NetId = identity.NetId,
					ConsumableId = consumable_id,
					IsAllowed = is_allowed
				};

				if (MultiplayerSession.IsHost)
					PacketSender.SendToAllClients(packet);
				else
					PacketSender.SendToHost(packet);
			}
		}
	}
}
