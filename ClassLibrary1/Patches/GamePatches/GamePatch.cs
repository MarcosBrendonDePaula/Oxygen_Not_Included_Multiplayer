using HarmonyLib;
using ONI_MP.Misc.World;
using ONI_MP.Networking;

namespace ONI_MP.Patches.GamePatches
{
	// This single class contains BOTH patches.
	public static class GamePatch
	{
		// Patch Game.Update to run the two batchers if host
		[HarmonyPatch(typeof(Game), "Update")]
		[HarmonyPostfix]
		public static void UpdatePostfix()
		{
			if (MultiplayerSession.IsHost)
			{
				ONI_MP.DebugTools.DebugConsole.Log("[GamePatch] UpdatePostfix START");
				ONI_MP.DebugTools.DebugConsole.Log("[GamePatch] InstantiationBatcher.Update");
				InstantiationBatcher.Update();
				ONI_MP.DebugTools.DebugConsole.Log("[GamePatch] WorldUpdateBatcher.Update");
				WorldUpdateBatcher.Update();
				ONI_MP.DebugTools.DebugConsole.Log("[GamePatch] UpdatePostfix END");
			}
		}

		[HarmonyPatch(typeof(Game), "OnSpawn")]
		[HarmonyPostfix]
		public static void OnSpawnPostfix()
		{

		}
	}
}
