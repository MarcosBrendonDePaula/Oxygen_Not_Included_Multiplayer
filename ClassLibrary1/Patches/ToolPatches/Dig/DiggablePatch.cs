using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;

namespace ONI_MP.Patches.ToolPatches.Dig
{
	[HarmonyPatch(typeof(Diggable), "OnStopWork")]
	public static class DiggablePatch
	{
		public static void Prefix(Diggable __instance)
	{
		DebugConsole.Log("[DiggablePatch] Prefix START");
		if (!MultiplayerSession.IsHost || !MultiplayerSession.InSession)
		{
			DebugConsole.Log("[DiggablePatch] Prefix skipped (not host)");
			return;
		}

		DebugConsole.Log("[DiggablePatch] Getting cell");
		int cell = __instance.GetCell();

		if (!Grid.IsValidCell(cell))
		{
			DebugConsole.Log("[DiggablePatch] Invalid cell, returning");
			return;
		}

		DebugConsole.Log($"[DiggablePatch] Creating packet for cell {cell}");
		var packet = new DigCompletePacket
		{
			Cell = cell,
			Mass = Grid.Mass[cell],
			Temperature = Grid.Temperature[cell],
			ElementIdx = Grid.ElementIdx[cell],
			DiseaseIdx = Grid.DiseaseIdx[cell],
			DiseaseCount = Grid.DiseaseCount[cell]
		};

		DebugConsole.Log("[DiggablePatch] Sending packet");
		PacketSender.SendToAllClients(packet);
		DebugConsole.Log($"[Dig Complete] Host sent DigCompletePacket for cell {cell}");
		DebugConsole.Log("[DiggablePatch] Prefix END - about to return to original OnStopWork");
	}
	}
}
