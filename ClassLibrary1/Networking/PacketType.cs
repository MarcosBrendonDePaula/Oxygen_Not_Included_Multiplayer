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
        ChoreMove = 5,
        Ping = 6,
        Pong = 7,
        EntityPosition = 8
        // Add more types here
    }
}
