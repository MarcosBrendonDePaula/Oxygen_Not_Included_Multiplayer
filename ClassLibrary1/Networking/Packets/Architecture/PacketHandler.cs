using System.IO;

namespace ONI_MP.Networking.Packets.Architecture
{

	public static class PacketHandler
	{
		public static bool readyToProcess = true;

		public static void HandleIncoming(byte[] data)
		{
			if (!readyToProcess)
			{
				return;
			}

			using (var ms = new MemoryStream(data))
			{
				using (var reader = new BinaryReader(ms))
				{
					PacketType type = (PacketType)reader.ReadInt32();
					var packet = PacketRegistry.Create(type);
					packet.Deserialize(reader);
					Dispatch(packet);
				}
			}
		}

		private static void Dispatch(IPacket packet)
		{
			packet.OnDispatched();
		}
	}

}
