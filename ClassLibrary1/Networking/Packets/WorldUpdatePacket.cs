using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets
{
    namespace ONI_MP.Networking.Packets
    {
        public class WorldUpdatePacket : IPacket
        {
            public PacketType Type => PacketType.WorldUpdate;
            public List<CellUpdate> Updates = new List<CellUpdate>();

            public struct CellUpdate
            {
                public int Cell;
                public ushort ElementIdx;
                public float Temperature, Mass;
                public byte DiseaseIdx;
                public int DiseaseCount;
            }

            public void Serialize(BinaryWriter w)
            {
                w.Write(Updates.Count);
                foreach (var u in Updates)
                {
                    w.Write(u.Cell);
                    w.Write(u.ElementIdx);
                    w.Write(u.Temperature);
                    w.Write(u.Mass);
                    w.Write(u.DiseaseIdx);
                    w.Write(u.DiseaseCount);
                }
            }
            public void Deserialize(BinaryReader r)
            {
                int count = r.ReadInt32();
                Updates = new List<CellUpdate>(count);
                for (int i = 0; i < count; i++)
                {
                    Updates.Add(new CellUpdate
                    {
                        Cell = r.ReadInt32(),
                        ElementIdx = r.ReadUInt16(),
                        Temperature = r.ReadSingle(),
                        Mass = r.ReadSingle(),
                        DiseaseIdx = r.ReadByte(),
                        DiseaseCount = r.ReadInt32()
                    });
                }
            }

            public void OnDispatched()
            {
                if (MultiplayerSession.IsHost) return;

                foreach (var u in Updates)
                {
                    SimMessages.ModifyCell(
                        u.Cell, u.ElementIdx,
                        u.Temperature, u.Mass,
                        u.DiseaseIdx, u.DiseaseCount,
                        SimMessages.ReplaceType.Replace
                    );
                }
            }
        }
    }
}
