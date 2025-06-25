using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets;

namespace ONI_MP.Networking.Packets.Architecture
{

    public static class PacketHandler
    {
        public static bool readyToProcess = true;

        public static void HandleIncoming(byte[] data)
        {
            if(!readyToProcess)
            {
                DebugConsole.LogWarning("[PacketHandler] Packet received but processing is disabled. Discarding packet.");
                return;
            }

            try
            {
                using (var ms = new MemoryStream(data))
                {
                    using (var reader = new BinaryReader(ms))
                    {
                        PacketType type = (PacketType)reader.ReadByte();
                        var packet = PacketRegistry.Create(type);
                        packet.Deserialize(reader);
                        Dispatch(packet);
                    }
                }
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[PacketHandler] Error processing incoming packet: {ex}");
            }
        }

        private static void Dispatch(IPacket packet)
        {
            packet.OnDispatched();
        }
    }

}
