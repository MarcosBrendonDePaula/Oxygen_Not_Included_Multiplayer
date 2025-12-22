using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Patches.World
{
	// Patches for BatterySmart and other range-based buildings
	// Note: BatterySmart uses IActivationRangeTarget for sliders usually in side-screen?

	[HarmonyPatch(typeof(BatterySmart), "ActivateValue", MethodType.Setter)]
	public static class SmartBatteryActivatePatch
	{
		public static void Postfix(BatterySmart __instance, float value)
		{
			SendUpdate(__instance, "Activate", value);
		}

		public static void SendUpdate(BatterySmart battery, string param, float val)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = battery.GetComponent<NetworkIdentity>();
			if (identity == null) return;

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				ConfigHash = param.GetHashCode(),
				Value = val
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	[HarmonyPatch(typeof(BatterySmart), "DeactivateValue", MethodType.Setter)]
	public static class SmartBatteryDeactivatePatch
	{
		public static void Postfix(BatterySmart __instance, float value)
		{
			SmartBatteryActivatePatch.SendUpdate(__instance, "Deactivate", value);
		}
	}

	// SmartReservoir uses IActivationRangeTarget with ActivateValue/DeactivateValue
	[HarmonyPatch(typeof(SmartReservoir), "ActivateValue", MethodType.Setter)]
	public static class SmartReservoirActivatePatch
	{
		public static void Postfix(SmartReservoir __instance, float value)
		{
			DebugConsole.Log($"[SmartReservoirActivatePatch] ActivateValue setter called with value={value}");
			
			if (BuildingConfigPacket.IsApplyingPacket)
			{
				DebugConsole.Log("[SmartReservoirActivatePatch] IsApplyingPacket=true, skipping");
				return;
			}
			if (!MultiplayerSession.InSession)
			{
				DebugConsole.Log("[SmartReservoirActivatePatch] Not in session, skipping");
				return;
			}

			var identity = __instance.GetComponent<NetworkIdentity>();
			if (identity == null)
			{
				identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
				identity.RegisterIdentity();
			}

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "SmartReservoirActivate".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.Float
			};

			DebugConsole.Log($"[SmartReservoirActivatePatch] Sending packet: ConfigHash={packet.ConfigHash}, Value={value}");
			
			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	[HarmonyPatch(typeof(SmartReservoir), "DeactivateValue", MethodType.Setter)]
	public static class SmartReservoirDeactivatePatch
	{
		public static void Postfix(SmartReservoir __instance, float value)
		{
			DebugConsole.Log($"[SmartReservoirDeactivatePatch] DeactivateValue setter called with value={value}");
			
			if (BuildingConfigPacket.IsApplyingPacket)
			{
				DebugConsole.Log("[SmartReservoirDeactivatePatch] IsApplyingPacket=true, skipping");
				return;
			}
			if (!MultiplayerSession.InSession)
			{
				DebugConsole.Log("[SmartReservoirDeactivatePatch] Not in session, skipping");
				return;
			}

			var identity = __instance.GetComponent<NetworkIdentity>();
			if (identity == null)
			{
				identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
				identity.RegisterIdentity();
			}

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "SmartReservoirDeactivate".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.Float
			};

			DebugConsole.Log($"[SmartReservoirDeactivatePatch] Sending packet: ConfigHash={packet.ConfigHash}, Value={value}");
			
			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	// Generic IThresholdSwitch patch?
	// Often implemented by LogicTemperatureSensor, LogicPressureSensor, etc.
	// They usually have a 'Threshold' property.
	// Patching LogicThresholdSwitch (the specific component)

	// [HarmonyPatch(typeof(LogicThresholdSwitch), "Threshold", MethodType.Setter)]
	// public static class LogicThresholdSwitchPatch
	// {
	//     public static void Postfix(LogicThresholdSwitch __instance, float value)
	//     {
	//         if (BuildingConfigPacket.IsApplyingPacket) return;
	//         if (!MultiplayerSession.InSession) return;
	//
	//         var identity = __instance.GetComponent<NetworkIdentity>();
	//         if (identity == null) return;
	//
	//         var packet = new BuildingConfigPacket
	//         {
	//             NetId = identity.NetId,
	//             ConfigHash = "Threshold".GetHashCode(),
	//             Value = value
	//         };
	//
	//         if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
	//         else PacketSender.SendToHost(packet);
	//     }
	// }
}

