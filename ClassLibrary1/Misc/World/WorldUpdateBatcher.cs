using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Misc.World
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

		public static void Update()
		{
			DebugConsole.Log("[WorldUpdateBatcher] Update START");
			flushTimer += Time.unscaledDeltaTime;
			if (flushTimer >= FlushInterval)
			{
				DebugConsole.Log("[WorldUpdateBatcher] Calling Flush");
				Flush();
				DebugConsole.Log("[WorldUpdateBatcher] Flush complete");
				flushTimer = 0f;
			}
			DebugConsole.Log("[WorldUpdateBatcher] Update END");
		}

		public static void Flush()
		{
			lock (pendingUpdates)
			{
				if (pendingUpdates.Count == 0)
					return;

				DebugConsole.Log($"[WorldUpdateBatcher] Flush START with {pendingUpdates.Count} updates");
				// Each cell update is roughly 5.38 bytes after compression (1KB / 5.38 = 188)
				const int MaxUpdatesPerPacket = 180; // Keep packet size under ~1KB

				for (int i = 0; i < pendingUpdates.Count; i += MaxUpdatesPerPacket)
				{
					var chunk = pendingUpdates.GetRange(i, Math.Min(MaxUpdatesPerPacket, pendingUpdates.Count - i));
					var packet = new WorldUpdatePacket();
					packet.Updates.AddRange(chunk);
					DebugConsole.Log($"[WorldUpdateBatcher] Sending chunk {i / MaxUpdatesPerPacket} ({chunk.Count} cells)");
					PacketSender.SendToAll(packet, sendType: SteamNetworkingSend.Unreliable); // max packet size 1200 bytes (typically 1170–1200 bytes)

					DebugConsole.Log($"[World] Sent chunked WorldUpdate ({chunk.Count} cells)");
				}

				pendingUpdates.Clear();
				DebugConsole.Log("[WorldUpdateBatcher] Flush END");
			}
		}

	}
}
