using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking.Packets;

namespace ONI_MP.Networking
{
    public static class PacketRegistry
    {
        private static readonly Dictionary<PacketType, Func<IPacket>> _constructors = new Dictionary<PacketType, Func<IPacket>>();

        public static void Register(PacketType type, Func<IPacket> constructor)
        {
            _constructors[type] = constructor;
        }

        public static IPacket Create(PacketType type)
        {
            return _constructors.TryGetValue(type, out var ctor)
                ? ctor()
                : throw new InvalidOperationException($"No packet registered for type {type}");
        }

        public static void RegisterDefaults()
        {
            Register(PacketType.Hello, () => new HelloPacket());
            Register(PacketType.Ping, () => new PingPacket());
            Register(PacketType.Pong, () => new PongPacket());
            Register(PacketType.ChoreAssignment, () => new ChoreAssignmentPacket());
            Register(PacketType.EntityPosition, () => new EntityPositionPacket());
            Register(PacketType.ChatMessage, () => new ChatMessagePacket());
            Register(PacketType.WorldData, () => new WorldDataPacket());
            Register(PacketType.WorldDataRequest, () => new WorldDataRequestPacket());
            Register(PacketType.WorldUpdate, () => new WorldUpdatePacket());
            Register(PacketType.Instantiate, () => new InstantiatePacket());
            Register(PacketType.Instantiations, () => new InstantiationsPacket());
            Register(PacketType.NavigatorPath, () => new NavigatorPathPacket());
            Register(PacketType.SaveFile, () => new SaveFilePacket());
            Register(PacketType.SaveFileRequest, () => new SaveFileRequestPacket());
            // Add more registrations here
        }
    }
}
