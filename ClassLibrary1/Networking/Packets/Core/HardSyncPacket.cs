using System.IO;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.Core
{
    public class HardSyncPacket : IPacket
    {
        public PacketType Type => PacketType.HardSync;

        public void Serialize(BinaryWriter writer)
        {
            // No payload needed
        }

        public void Deserialize(BinaryReader reader)
        {
            // No payload needed
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost)
                return;

            SpeedControlScreen.Instance?.Pause(false);
            MultiplayerOverlay.Show("Hard sync in progress!");
        }

    }
}
