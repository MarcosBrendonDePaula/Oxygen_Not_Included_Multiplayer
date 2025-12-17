using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;
using UnityEngine;

namespace ONI_MP.Patches.World
{
	public static class BuildingConfigPatch
	{
		// Use flag from packet to prevent loops
		private static bool IgnoreEvents => BuildingConfigPacket.IsApplyingPacket;

		// Sync Logic Switches (User Toggles)
		[HarmonyPatch(typeof(Switch), "SetState")]
		public static class LogicSwitchPatch
		{
			public static void Postfix(Switch __instance, bool on)
			{
				if (IgnoreEvents) return;
				SyncBuildingConfig(__instance, "LogicState", on ? 1f : 0f);
			}
		}

		// Sync Valve Flow
		[HarmonyPatch(typeof(Valve), "ChangeFlow")]
		public static class ValvePatch
		{
			public static void Postfix(Valve __instance, float amount)
			{
				if (IgnoreEvents) return;
				SyncBuildingConfig(__instance, "Rate", amount);
			}
		}

		/*
		// Sync Slider Side Screen (Batteries, Sensors, etc.)
		// [HarmonyPatch(typeof(SingleSliderSideScreen), "OnRelease")] // Method not found
		public static class SingleSliderSideScreenPatch
		{
				public static void Postfix(SingleSliderSideScreen __instance)
				{
						// ... implementation ...
				}
		}
		*/

		// Helper
		private static void SyncBuildingConfig(Component component, string configId, float value)
		{
			if (component == null) return;
			if (!MultiplayerSession.InSession) return;

			// Get NetId
			int netId = -1;
			var identity = component.GetComponent<NetworkIdentity>();
			if (identity != null)
			{
				netId = identity.NetId;
			}
			else
			{
				// Check registry? Identity should be on the object.
				// Maybe look for identity on parent?
				identity = component.GetComponentInParent<NetworkIdentity>();
				if (identity != null) netId = identity.NetId;
			}

			if (netId != -1)
			{
				var packet = new BuildingConfigPacket
				{
					NetId = netId,
					ConfigHash = configId.GetHashCode(),
					Value = value
				};

				// If Host, broadcast to all.
				// If Client, send to Host.
				if (MultiplayerSession.IsHost)
				{
					PacketSender.SendToAllClients(packet);
				}
				else
				{
					PacketSender.SendToHost(packet);
				}
			}
		}
	}
}
