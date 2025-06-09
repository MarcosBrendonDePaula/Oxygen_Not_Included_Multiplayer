using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.World;

namespace ONI_MP.Networking.Packets
{
    public class SaveFileChunkPacket : IPacket
    {
        public string FileName;
        public int Offset;
        public int TotalSize;
        public byte[] Chunk;

        public PacketType Type => PacketType.SaveFileChunk;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(FileName);
            writer.Write(Offset);
            writer.Write(TotalSize);
            writer.Write(Chunk.Length);
            writer.Write(Chunk);
        }

        public void Deserialize(BinaryReader reader)
        {
            FileName = reader.ReadString();
            Offset = reader.ReadInt32();
            TotalSize = reader.ReadInt32();
            int length = reader.ReadInt32();
            Chunk = reader.ReadBytes(length);
        }

        public void OnDispatched()
        {
            SaveChunkAssembler.ReceiveChunk(this);
        }
    }
}
