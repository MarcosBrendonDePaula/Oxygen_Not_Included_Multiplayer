using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.Architecture
{
    public enum PacketType : byte
    {
        ChatMessage = 1,
        EntityPosition = 2,
        ChoreAssignment = 3,
        WorldData = 4,          // Keeping for now, might find a use
        WorldDataRequest = 5,   // Keeping for now, might find a use
        WorldUpdate = 6,        // Batched world updates
        Instantiate = 7,        // Singular instantiation
        Instantiations = 8,     // Batched instantiations
        NavigatorPath = 9,
        SaveFileRequest = 10,
        SaveFileChunk = 11,
        Diggable = 12,
        DigComplete = 13,
        PlayAnim = 14,
        Build = 15,
        BuildComplete = 16,
        WorldDamageSpawnResource = 17,
        WorldCycle = 18,
        Cancel = 19,
        Deconstruct = 20,
        DeconstructComplete = 21,
        WireBuild = 22,
        ToggleMinionEffect = 23
    }
}
