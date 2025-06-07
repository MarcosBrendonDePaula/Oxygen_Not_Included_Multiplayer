using System;
using System.IO;

namespace ONI_MP.Networking.Packets
{
    public class PingPacket : IPacket
    {
        public long Timestamp; // in ticks (DateTime.UtcNow.Ticks)

        public PacketType Type => PacketType.Ping;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Timestamp);
        }

        public void Deserialize(BinaryReader reader)
        {
            Timestamp = reader.ReadInt64();
        }

        public void OnDispatched()
        {
            // Client sends this to the host, so no local logic needed.
            // Host should respond with a PongPacket in the handler.
        }
    }
}
