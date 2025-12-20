using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;
using UnityEngine;

namespace ONI_MP.Patches.World
{
	/// <summary>
	/// Patches for IThresholdSwitch implementations (logic sensors like temperature, pressure, wattage).
	/// Note: Patches are disabled until the correct method names are verified.
	/// </summary>

	// TODO: Add specific IThresholdSwitch patches when correct method names are verified
	// Common examples that need investigation:
	// - LogicTemperatureSensor.Threshold setter
	// - LogicPressureSensor.Threshold setter
	// - LogicWattageSensor.Threshold setter
	// - etc.

	public static class ThresholdSwitchSyncHelper
	{
		public static void SyncThresholdChange(GameObject go, string configId, float value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (go == null) return;

			var identity = go.GetComponent<NetworkIdentity>();
			if (identity == null) return;

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				ConfigHash = configId.GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost)
				PacketSender.SendToAllClients(packet);
			else
				PacketSender.SendToHost(packet);
		}
	}
}
