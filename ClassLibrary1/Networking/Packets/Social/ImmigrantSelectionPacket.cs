using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.Social
{
	public class ImmigrantSelectionPacket : IPacket
	{
		public PacketType Type => PacketType.ImmigrantSelection;

		public int SelectedIndex; // 0, 1, 2... or -1 for Reject All?

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(SelectedIndex);
		}

		public void Deserialize(BinaryReader reader)
		{
			SelectedIndex = reader.ReadInt32();
		}

		public void OnDispatched()
		{
			if (!MultiplayerSession.IsHost) return;

			// Host received selection from client.
			// Attempt to use the ImmigrantScreen logic if available.

			if (ImmigrantScreen.instance == null)
			{
				// TODO: If screen is not open, we should interact with Immigration.Instance directly.
				// For now, this requires the Host to have the screen "ready" or we force it?
				// Or maybe we can just find the Telepad and trigger acceptance of the deliverable by index.
				// var deliverable = Immigration.Instance.ActiveDeliverables[SelectedIndex];
				// Telepad.OnAcceptDelivery(deliverable);
				DebugConsole.Log("ImmigrantSelectionPacket: ImmigrantScreen.instance is null on Host. Cannot process selection yet.");
				return;
			}

			var containers = Traverse.Create(ImmigrantScreen.instance).Field("containers").GetValue<List<CharacterContainer>>();

			if (containers != null)
			{
				if (SelectedIndex >= 0 && SelectedIndex < containers.Count)
				{
					var container = containers[SelectedIndex];
					Traverse.Create(ImmigrantScreen.instance).Field("selectedContainer").SetValue(container);
					Traverse.Create(ImmigrantScreen.instance).Method("OnProceed").GetValue();
				}
				else if (SelectedIndex == -1) // Reject All logic if we support it
				{
					// Traverse.Create(ImmigrantScreen.instance).Method("OnRejectAll").GetValue(); // If exists
					ImmigrantScreen.instance.Deactivate(); // Just close?
				}
			}
		}
	}
}
