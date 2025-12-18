using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Social;
using System.Collections.Generic;

namespace ONI_MP.Patches.GamePatches
{
	// Note: ImmigrantScreen logic is complex. This is a partial implementation.
	// We need to sync the containers (Care Packages / Duplicants)

	public static class ImmigrantScreenPatch
	{
		public static List<ImmigrantOptionsPacket.OptionEntry> AvailableOptions;

		public static void ApplyOptionsToScreen(ImmigrantScreen screen)
		{
			if (AvailableOptions == null || AvailableOptions.Count == 0 || screen == null)
			{
				DebugConsole.LogWarning($"[ImmigrantScreen] ApplyOptionsToScreen: Cannot apply - Options:{AvailableOptions?.Count ?? 0}, Screen:{(screen != null ? "valid" : "null")}");
				return;
			}

			var containersObj = Traverse.Create(screen).Field("containers").GetValue();
			if (containersObj == null)
			{
				DebugConsole.LogWarning("[ImmigrantScreen] ApplyOptionsToScreen: containers field is null!");
				return;
			}

			var containers = new List<CharacterContainer>();
			if (containersObj is System.Collections.IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					if (item is CharacterContainer cc) containers.Add(cc);
				}
			}

			if (containers.Count == 0)
			{
				DebugConsole.LogWarning("[ImmigrantScreen] ApplyOptionsToScreen: No containers found!");
				return;
			}

			DebugConsole.Log($"[ImmigrantScreen] ApplyOptionsToScreen: Applying {AvailableOptions.Count} options to {containers.Count} containers");

			for (int i = 0; i < containers.Count; i++)
			{
				if (i >= AvailableOptions.Count)
				{
					containers[i].gameObject.SetActive(false);
					continue;
				}

				var opt = AvailableOptions[i];
				var container = containers[i];
				container.gameObject.SetActive(true);

				try
				{
					if (opt.IsDuplicant)
					{
						var personality = Db.Get().Personalities.TryGet(opt.PersonalityId);
						if (personality == null) personality = Db.Get().Personalities.TryGet("Hassan");

						var stats = new MinionStartingStats(personality);
						stats.Name = opt.Name;
						if (!string.IsNullOrEmpty(opt.Gender))
						{
							Traverse.Create(stats).Field("genderString").SetValue(opt.Gender);
						}

						Traverse.Create(container).Method("SetInfo", new object[] { stats }).GetValue();
						DebugConsole.Log($"[ImmigrantScreen]   Applied Duplicant {i}: {opt.Name}");
					}
					else
					{
						var pkg = new CarePackageInfo(opt.CarePackageId, opt.Quantity, null);
						Traverse.Create(container).Method("SetInfo", new object[] { pkg }).GetValue();
						DebugConsole.Log($"[ImmigrantScreen]   Applied CarePackage {i}: {opt.CarePackageId}");
					}
				}
				catch (System.Exception ex)
				{
					DebugConsole.LogError($"[ImmigrantScreen]   Error applying option {i}: {ex.Message}");
				}
			}
		}
	}

	[HarmonyPatch(typeof(ImmigrantScreen), "Initialize")]
	public static class ImmigrantScreenInitializePatch
	{
		public static void Postfix(ImmigrantScreen __instance)
		{
			if (!MultiplayerSession.InSession) return;

			DebugConsole.Log("[ImmigrantScreen] Initialize postfix triggered");

			var containersObj = Traverse.Create(__instance).Field("containers").GetValue();
			if (containersObj == null)
			{
				DebugConsole.LogWarning("[ImmigrantScreen] containers field is null!");
				return;
			}

			// Convert to list - containers might be an array or other collection
			var containers = new List<CharacterContainer>();
			if (containersObj is System.Collections.IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					if (item is CharacterContainer cc) containers.Add(cc);
				}
			}

			DebugConsole.Log($"[ImmigrantScreen] Found {containers.Count} containers");

			if (containers.Count == 0) return;

			if (MultiplayerSession.IsHost)
			{
				DebugConsole.Log("[ImmigrantScreen] Host: Capturing options to broadcast...");
				// Host: Capture options and broadcast
				var packet = new ImmigrantOptionsPacket();
				foreach (var container in containers)
				{
					var stats = Traverse.Create(container).Field("stats").GetValue<MinionStartingStats>();
					var pkg = Traverse.Create(container).Field("carePackageInfo").GetValue<CarePackageInfo>();

					var entry = new ImmigrantOptionsPacket.OptionEntry();
					if (stats != null)
					{
						entry.IsDuplicant = true;
						entry.Name = stats.Name;
						// Stats
						string gender = Traverse.Create(stats).Field("genderString").GetValue<string>();
						if (gender == null) gender = "Unknown";
						entry.Gender = gender;
						entry.PersonalityId = stats.personality != null ? stats.personality.Id : "Hassan";
						DebugConsole.Log($"[ImmigrantScreen]   Capturing Duplicant: {entry.Name} ({entry.PersonalityId})");
					}
					else if (pkg != null)
					{
						entry.IsDuplicant = false;
						entry.CarePackageId = pkg.id;
						entry.Quantity = pkg.quantity;
						DebugConsole.Log($"[ImmigrantScreen]   Capturing CarePackage: {entry.CarePackageId} x{entry.Quantity}");
					}
					else
					{
						DebugConsole.LogWarning("[ImmigrantScreen]   Container has no stats or carePackageInfo, skipping");
						continue;
					}
					packet.Options.Add(entry);
				}

				if (packet.Options.Count > 0)
				{
					ImmigrantScreenPatch.AvailableOptions = packet.Options;
					DebugConsole.Log($"[ImmigrantScreen] Host: Broadcasting {packet.Options.Count} options to all clients");
					PacketSender.SendToAllClients(packet);
				}
				else
				{
					DebugConsole.LogWarning("[ImmigrantScreen] Host: No options to broadcast!");
				}
			}
			else
			{
				// Client: Apply received options if available
				if (ImmigrantScreenPatch.AvailableOptions != null && ImmigrantScreenPatch.AvailableOptions.Count > 0)
				{
					DebugConsole.Log($"[ImmigrantScreen] Client: Applying {ImmigrantScreenPatch.AvailableOptions.Count} cached options");
					ImmigrantScreenPatch.ApplyOptionsToScreen(__instance);
				}
				else
				{
					DebugConsole.LogWarning("[ImmigrantScreen] Client: No cached options available yet!");
				}
			}
		}
	}

	[HarmonyPatch(typeof(ImmigrantScreen), "OnProceed")]
	public static class ImmigrantScreenProceedPatch
	{
		public static bool Prefix(ImmigrantScreen __instance)
		{
			if (MultiplayerSession.IsHost) return true;

			// Client selected something and clicked Print.
			// We need to find what was selected.
			// __instance.selectedContainer is the selected option.

			// We get the index of the selected container.

			// Traverse to get private field
			var selectedContainer = Traverse.Create(__instance).Field("selectedContainer").GetValue<CharacterContainer>();

			int selectedIndex = -1;
			if (selectedContainer != null)
			{
				// Find index in __instance.containers
				var containersObj = Traverse.Create(__instance).Field("containers").GetValue();
				if (containersObj != null && containersObj is System.Collections.IEnumerable enumerable)
				{
					var containersList = new List<CharacterContainer>();
					foreach (var item in enumerable)
					{
						if (item is CharacterContainer cc) containersList.Add(cc);
					}
					selectedIndex = containersList.IndexOf(selectedContainer);
				}
			}

			if (selectedIndex != -1)
			{
				var packet = new ImmigrantSelectionPacket { SelectedIndex = selectedIndex };
				PacketSender.SendToHost(packet);
			}

			// Suppress local printing execution logic which creates the minion immediately on client (which might desync or crash if not ready)
			// Ideally we wait for Host to spawn it and sync via other means.
			// Or we allow it if the spawn logic is deterministic and we just told host "I did this".
			// But safer to suppress and let Host handle the spawn.

			// Close the screen locally though?
			__instance.Deactivate();

			return false;
		}
	}
}
