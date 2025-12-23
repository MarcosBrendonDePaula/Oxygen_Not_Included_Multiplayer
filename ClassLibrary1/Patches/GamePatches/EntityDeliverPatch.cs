using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Patches.GamePatches
{
	/// <summary>
	/// Patch MinionStartingStats.Deliver to send EntitySpawnPacket when duplicants are spawned.
	/// This ensures newly printed duplicants are synced to clients with correct NetIds.
	/// </summary>
	[HarmonyPatch(typeof(MinionStartingStats), nameof(MinionStartingStats.Deliver))]
	public static class MinionDeliverPatch
	{
		public static void Postfix(MinionStartingStats __instance, Vector3 location, ref GameObject __result)
		{
			if (!MultiplayerSession.IsHost) return;
			if (__result == null) return;


			try
			{
				// Get or add NetworkIdentity
				var identity = __result.AddOrGet<NetworkIdentity>();

				// Make sure the NetId is valid (not 0)
				if (identity.NetId == 0)
				{
					// Force registration to generate a new NetId
					identity.RegisterIdentity();
					DebugConsole.Log($"[MinionDeliverPatch] Registered with NetId {identity.NetId} for {__instance.Name}");
				}

				// Build trait list
				var traitIds = new List<string>();
				if (__instance.Traits != null)
				{
					foreach (var trait in __instance.Traits)
					{
						if (trait != null) traitIds.Add(trait.Id);
					}
				}

				// Get personality ID
				var personalityId = "";
				if (__instance.personality != null)
				{
					personalityId = __instance.personality.Id;
				}

				// Send EntitySpawnPacket to clients
				var packet = new EntitySpawnPacket
				{
					NetId = identity.NetId,
					IsDuplicant = true,
					Name = __instance.Name,
					PersonalityId = personalityId,
					TraitIds = traitIds,
					PosX = location.x,
					PosY = location.y
				};

				PacketSender.SendToAllClients(packet);
				DebugConsole.Log($"[MinionDeliverPatch] Sent EntitySpawnPacket for {__instance.Name} (NetId: {identity.NetId})");
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[MinionDeliverPatch] Error: {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Patch CarePackageInfo.Deliver to send EntitySpawnPacket when care packages are spawned.
	/// </summary>
	[HarmonyPatch(typeof(CarePackageInfo), nameof(CarePackageInfo.Deliver))]
	public static class CarePackageDeliverPatch
	{
		public static void Postfix(CarePackageInfo __instance, Vector3 location, ref GameObject __result)
		{
			if (!MultiplayerSession.IsHost) return;
			if (__result == null) return;

			try
			{
				// Get or add NetworkIdentity
				var identity = __result.AddOrGet<NetworkIdentity>();

				// Make sure the NetId is valid (not 0)
				if (identity.NetId == 0)
				{
					identity.RegisterIdentity();
					DebugConsole.Log($"[CarePackageDeliverPatch] Registered with NetId {identity.NetId} for {__instance.id}");
				}

				// Send EntitySpawnPacket to clients
				var packet = new EntitySpawnPacket
				{
					NetId = identity.NetId,
					IsDuplicant = false,
					ItemId = __instance.id,
					Quantity = __instance.quantity,
					PosX = location.x,
					PosY = location.y
				};

				PacketSender.SendToAllClients(packet);
				DebugConsole.Log($"[CarePackageDeliverPatch] Sent EntitySpawnPacket for {__instance.id} x{__instance.quantity} (NetId: {identity.NetId})");
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[CarePackageDeliverPatch] Error: {ex.Message}");
			}
		}
	}
}
