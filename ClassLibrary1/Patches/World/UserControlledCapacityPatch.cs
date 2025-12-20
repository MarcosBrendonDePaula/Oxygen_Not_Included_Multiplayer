using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Patches.World
{
	/// <summary>
	/// Patches for IUserControlledCapacity implementations to sync storage capacity sliders.
	/// Note: Patches are disabled until the correct method names are verified.
	/// </summary>

	// TODO: Add specific IUserControlledCapacity patches when correct method names are verified
	// Common examples that need investigation:
	// - StorageLocker.UserMaxCapacity setter
	// - RationBox.UserMaxCapacity setter
	// - Refrigerator.UserMaxCapacity setter
	// - ObjectDispenser.UserMaxCapacity setter
	// - SolidConduitInbox.UserMaxCapacity setter

	public static class UserControlledCapacityHelper
	{
		public static void SyncCapacityChange(UnityEngine.Component component, float value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (component == null) return;

			var identity = component.GetComponent<NetworkIdentity>();
			if (identity == null) return;

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				ConfigHash = "Capacity".GetHashCode(),
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
