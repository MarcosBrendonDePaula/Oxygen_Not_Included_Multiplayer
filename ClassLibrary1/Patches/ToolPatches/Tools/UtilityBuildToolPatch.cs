using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Tools;
using ONI_MP.Networking.Packets.Tools.Build;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ONI_MP.Patches.ToolPatches.Build
{
	// Try patching BuildPath - called when drag is complete and building is placed
	[HarmonyPatch(typeof(BaseUtilityBuildTool), nameof(BaseUtilityBuildTool.BuildPath))]
	public static class UtilityBuildToolPatch
	{
		public static void Prefix(BaseUtilityBuildTool __instance)
		{
			//DebugConsole.Log($"[UtilityBuildToolPatch] Prefix called! Tool type: {__instance.GetType().Name}");
			if (!MultiplayerSession.InSession)
			{
				return;
			}

			//prevent recursion
			if (UtilityBuildPacket.ProcessingIncoming)
				return;

			if (__instance.path == null || __instance.path.Count < 2 || __instance.def == null)
			{
				return;
			}

			PacketSender.SendToAllOtherPeers(new UtilityBuildPacket(__instance.def.PrefabID, __instance.path, [.. __instance.selectedElements.Select(t => t.ToString())]));
			DebugConsole.Log($"[UtilityBuild] Sent packet for {__instance.def.PrefabID} with {__instance.path} nodes.");
		}
	}
}
