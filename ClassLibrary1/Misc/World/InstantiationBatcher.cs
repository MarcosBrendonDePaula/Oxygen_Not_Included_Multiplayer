using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Misc.World
{
	public static class InstantiationBatcher
	{
		private static readonly List<InstantiationsPacket.InstantiationEntry> queue = new List<InstantiationsPacket.InstantiationEntry>();
		private static float timeSinceLastFlush = 0f;
		private const float FlushInterval = 2.0f;

		public static void Queue(InstantiationsPacket.InstantiationEntry entry)
		{
			queue.Add(entry);
		}

		public static void Update()
		{
			ONI_MP.DebugTools.DebugConsole.Log("[InstantiationBatcher] Update START");
			timeSinceLastFlush += Time.unscaledDeltaTime;

			if (timeSinceLastFlush >= FlushInterval)
			{
				ONI_MP.DebugTools.DebugConsole.Log("[InstantiationBatcher] Calling Flush");
				Flush();
				ONI_MP.DebugTools.DebugConsole.Log("[InstantiationBatcher] Flush complete");
				timeSinceLastFlush = 0f;
			}
			ONI_MP.DebugTools.DebugConsole.Log("[InstantiationBatcher] Update END");
		}

		public static void Flush()
		{
			if (queue.Count == 0)
				return;

			ONI_MP.DebugTools.DebugConsole.Log($"[InstantiationBatcher] Flush sending {queue.Count} items");
			var packet = new InstantiationsPacket
			{
				Entries = new List<InstantiationsPacket.InstantiationEntry>(queue)
			};

			PacketSender.SendToAll(packet, sendType: SteamNetworkingSend.Unreliable);
			queue.Clear();
			ONI_MP.DebugTools.DebugConsole.Log("[InstantiationBatcher] Flush done");
		}
	}
}
