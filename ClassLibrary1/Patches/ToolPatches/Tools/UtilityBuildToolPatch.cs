using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Tools.Build;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ONI_MP.Patches.ToolPatches.Build
{
	// Try patching BuildPath - called when drag is complete and building is placed
	[HarmonyPatch(typeof(BaseUtilityBuildTool), nameof(BaseUtilityBuildTool.BuildPath))]
	public static class UtilityBuildToolPatch
	{
		public static void Prefix(BaseUtilityBuildTool __instance)
		{
			if (!MultiplayerSession.InSession)
			{
				//DebugConsole.Log($"[UtilityBuildToolPatch] Not in MP session, returning");
				return;
			}
			DebugConsole.Log($"[UtilityBuildToolPatch] Prefix called! Tool type: {__instance.GetType().Name}");
			var pathList = __instance.path;
			if (pathList == null || pathList.Count < 2)
			{
				// Typically needs at least 2 nodes or valid drag to apply? 
				// Actually BaseUtilityBuildTool checks this. We should trust the tool is committing.
				return;
			}

			var def = __instance.def;
			if (def == null) return;

			// Reflect selected elements (BaseUtlityBuildTool -> BaseTool? No, it inherits. But selectedElements might be on BaseUtilityBuildTool or BuildTool?)
			// BaseUtilityBuildTool inherits from DragTool -> InterfaceTool -> PlayerController (wrapper).
			// Actually it has 'selectedElements' usually in BaseUtilityBuildTool or one of its parents.
			// Let's check AccessTools or use reflection on instance.
			// "selectedElements" is typically 'IList<Tag>'

			var selectedElements = __instance.selectedElements;;

			// Prepare Packet
			var packet = new UtilityBuildPacket();
			packet.PrefabID = def.PrefabID;
			packet.SenderId = MultiplayerSession.LocalSteamID;
			packet.Path = new List<UtilityBuildPacket.Node>();

			if (selectedElements != null)
			{
				foreach (var tag in selectedElements) packet.MaterialTags.Add(tag.Name);
			}

			// First pass: extract all cells from pathList
			var cellList = new List<int>();
			var validList = new List<bool>();
			var cellField = pathList[0].GetType().GetField("cell");
			var validField = pathList[0].GetType().GetField("valid");

			foreach (var node in pathList)
			{
				cellList.Add((int)cellField.GetValue(node));
				validList.Add((bool)validField.GetValue(node));
			}

			// Second pass: calculate connection directions based on neighbors in path
			for (int i = 0; i < cellList.Count; i++)
			{
				int cell = cellList[i];
				bool valid = validList[i];

				// Determine which directions this cell connects to based on path neighbors
				bool connectsUp = false, connectsDown = false, connectsLeft = false, connectsRight = false;

				// Check previous node in path
				if (i > 0)
				{
					int prevCell = cellList[i - 1];
					if (prevCell == Grid.CellAbove(cell)) connectsUp = true;
					else if (prevCell == Grid.CellBelow(cell)) connectsDown = true;
					else if (prevCell == Grid.CellLeft(cell)) connectsLeft = true;
					else if (prevCell == Grid.CellRight(cell)) connectsRight = true;
				}

				// Check next node in path
				if (i < cellList.Count - 1)
				{
					int nextCell = cellList[i + 1];
					if (nextCell == Grid.CellAbove(cell)) connectsUp = true;
					else if (nextCell == Grid.CellBelow(cell)) connectsDown = true;
					else if (nextCell == Grid.CellLeft(cell)) connectsLeft = true;
					else if (nextCell == Grid.CellRight(cell)) connectsRight = true;
				}

				packet.Path.Add(new UtilityBuildPacket.Node
				{
					Cell = cell,
					Valid = valid,
					ConnectsUp = connectsUp,
					ConnectsDown = connectsDown,
					ConnectsLeft = connectsLeft,
					ConnectsRight = connectsRight
				});
			}

			// Send
			DebugConsole.Log($"[UtilityBuildToolPatch] Sending UtilityBuildPacket: PrefabID={packet.PrefabID}, Nodes={packet.Path.Count}, Materials={packet.MaterialTags.Count}, IsHost={MultiplayerSession.IsHost}");

			if (MultiplayerSession.IsHost)
			{
				ONI_MP.Networking.PacketSender.SendToAllClients(packet);
				DebugConsole.Log($"[UtilityBuildToolPatch] Sent to all clients");
			}
			else
			{
				ONI_MP.Networking.PacketSender.SendToHost(packet);
				DebugConsole.Log($"[UtilityBuildToolPatch] Sent to host");
			}

			DebugConsole.Log($"[UtilityBuild] Sent packet for {def.PrefabID} with {packet.Path.Count} nodes.");
		}
	}
}
