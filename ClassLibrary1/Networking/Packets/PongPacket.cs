

namespace ONI_MP.Networking.Packets
{
    using System;
    using System.IO;
    using global::ONI_MP.DebugTools;

    public class PongPacket : IPacket
        {
            public long Timestamp;

            public PacketType Type => PacketType.Pong;

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
                if (MultiplayerSession.IsHost)
                    return;

                // Client receives this and calculates ping:
                long now = DateTime.UtcNow.Ticks;
                long elapsedTicks = now - Timestamp;
                int pingMs = (int)TimeSpan.FromTicks(elapsedTicks).TotalMilliseconds;

                var player = MultiplayerSession.GetPlayer(MultiplayerSession.HostSteamID);
                if (player != null)
                {
                    player.Ping = pingMs;
                    DebugConsole.Log($"[PongPacket] Ping to host: {pingMs} ms");
                }
            }
        }
    }
