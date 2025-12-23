using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc.World;
using ONI_MP.Networking;
using ONI_MP.Networking.States;

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
				InstantiationBatcher.Update();
				WorldUpdateBatcher.Update();
			}
		}

		[HarmonyPatch(typeof(Game), "OnSpawn")]
		[HarmonyPostfix]
		public static void OnSpawnPostfix()
		{
			DebugConsole.Log($"[GamePatch] Game.OnSpawn fired. ClientState={GameClient.State}, IsClient={MultiplayerSession.IsClient}, InSession={MultiplayerSession.InSession}");

			// Handle client reconnection after world is fully loaded
			// This is triggered AFTER the game world is completely initialized,
			// which is much safer than OnPostSceneLoaded which fires during unload
			
			// Check if we have cached connection info waiting to reconnect
			// State might be LoadingWorld or might have changed - check cache instead
			if (MultiplayerSession.IsClient && GameClient.HasCachedConnection())
			{
				DebugConsole.Log("[GamePatch] World fully loaded, reconnecting to host from cache...");
				GameClient.ReconnectFromCache();
				MultiplayerOverlay.Close();
			}
		}
	}
}

