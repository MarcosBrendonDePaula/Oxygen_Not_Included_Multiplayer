using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using System.IO;

namespace ONI_MP.Networking.Packets.Core
{
	public class HardSyncCompletePacket : IPacket
	{
		public void Serialize(BinaryWriter writer)
		{
			// No payload needed
		}

		public void Deserialize(BinaryReader reader)
		{
			// No payload needed
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
				return;

			SpeedControlScreen.Instance?.Unpause(false);
			MultiplayerOverlay.Close();
		}

	}
}
