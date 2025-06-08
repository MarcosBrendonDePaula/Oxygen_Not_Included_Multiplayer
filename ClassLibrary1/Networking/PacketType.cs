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
        PlayerJoined = 2,
        PlayerLeft = 3,
        ChatMessage = 4,
        Ping = 5,
        Pong = 6,
        EntityPosition = 7,
        ChoreAssignment = 8
    }
}
