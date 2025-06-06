using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Packets
{
    public class HelloPacket : IPacket
    {
        public string Username;

        public PacketType Type => PacketType.Hello;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Username);
        }

        public void Deserialize(BinaryReader reader)
        {
            Username = reader.ReadString();
        }

        public void OnDispatched()
        {
            DebugConsole.Log($"[Packets] Hello from {Username}");
        }
    }

}
