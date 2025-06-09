using System;
using System.Collections.Generic;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using Steamworks;

namespace ONI_MP.World
{
    public static class WorldUpdateBatcher
    {
        private static readonly List<WorldUpdatePacket.CellUpdate> pendingUpdates = new List<WorldUpdatePacket.CellUpdate>();
        private static float flushTimer = 0f;
        private const float FlushInterval = 10f; // Seconds

        public static void Queue(WorldUpdatePacket.CellUpdate update)
        {
            lock (pendingUpdates)
            {
                pendingUpdates.Add(update);
            }
        }

        public static void Update(float dt)
        {
            flushTimer += dt;
            if (flushTimer >= FlushInterval)
            {
                Flush();
                flushTimer = 0f;
            }
        }

        public static void Flush()
        {
            lock (pendingUpdates)
            {
                if (pendingUpdates.Count == 0)
                    return;

                const int MaxUpdatesPerPacket = 50; // Keep packet size under ~1KB

                for (int i = 0; i < pendingUpdates.Count; i += MaxUpdatesPerPacket)
                {
                    var chunk = pendingUpdates.GetRange(i, Math.Min(MaxUpdatesPerPacket, pendingUpdates.Count - i));
                    var packet = new WorldUpdatePacket();
                    packet.Updates.AddRange(chunk);
                    PacketSender.SendToAll(packet, EP2PSend.k_EP2PSendUnreliable);

                    DebugConsole.Log($"[World] Sent chunked WorldUpdate ({chunk.Count} cells)");
                }

                pendingUpdates.Clear();
            }
        }

    }
}
