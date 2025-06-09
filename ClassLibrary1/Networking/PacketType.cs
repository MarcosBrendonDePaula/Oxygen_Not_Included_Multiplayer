using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking
{
    public enum PacketType : byte
    {
        Hello = 1,
        ChatMessage = 2,
        Ping = 3,
        Pong = 4,
        EntityPosition = 5, // OLD
        ChoreAssignment = 6,
        WorldData = 7,
        WorldDataRequest = 8,
        WorldUpdate = 9,
        Instantiate = 10,
        Instantiations = 11,
        NavigatorPath = 12,
        SaveFile = 13,
        SaveFileRequest = 14
    }
}
