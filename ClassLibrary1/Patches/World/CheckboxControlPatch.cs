using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Patches.World
{
	/// <summary>
	/// Patches for ICheckboxControl implementations to sync checkbox toggles.
	/// Note: Most checkbox controls in ONI use ICheckboxControl interface but require
	/// patching specific implementations. Add patches as needed when specific 
	/// checkbox controls are identified.
	/// </summary>

	// TODO: Add specific ICheckboxControl patches as needed
	// Common examples that need investigation:
	// - Compost toggle
	// - Auto-disinfect toggle
	// - Various building toggles
	
	// Placeholder class so the file compiles
	public static class CheckboxControlPatches
	{
		// Helper method for sending checkbox state changes
		public static void SyncCheckboxChange(UnityEngine.Component component, string configId, bool value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (component == null) return;

			var identity = component.GetComponent<NetworkIdentity>();
			if (identity == null) return;

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				ConfigHash = configId.GetHashCode(),
				Value = value ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost)
				PacketSender.SendToAllClients(packet);
			else
				PacketSender.SendToHost(packet);
		}
	}
}
