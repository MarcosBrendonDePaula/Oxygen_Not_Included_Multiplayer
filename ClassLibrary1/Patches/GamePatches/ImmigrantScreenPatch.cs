using HarmonyLib;
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
			if (AvailableOptions == null || AvailableOptions.Count == 0 || screen == null) return;

			var containersObj = Traverse.Create(screen).Field("containers").GetValue();
			if (containersObj == null) return;

			var containers = new List<CharacterContainer>();
			if (containersObj is System.Collections.IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					if (item is CharacterContainer cc) containers.Add(cc);
				}
			}

			if (containers.Count == 0) return;

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
				}
				else
				{
					var pkg = new CarePackageInfo(opt.CarePackageId, opt.Quantity, null);
					Traverse.Create(container).Method("SetInfo", new object[] { pkg }).GetValue();
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

			var containersObj = Traverse.Create(__instance).Field("containers").GetValue();
			if (containersObj == null) return;

			// Convert to list - containers might be an array or other collection
			var containers = new List<CharacterContainer>();
			if (containersObj is System.Collections.IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					if (item is CharacterContainer cc) containers.Add(cc);
				}
			}

			if (containers.Count == 0) return;

			if (MultiplayerSession.IsHost)
			{
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
					}
					else if (pkg != null)
					{
						entry.IsDuplicant = false;
						entry.CarePackageId = pkg.id;
						entry.Quantity = pkg.quantity;
					}
					else
					{
						continue;
					}
					packet.Options.Add(entry);
				}

				if (packet.Options.Count > 0)
				{
					ImmigrantScreenPatch.AvailableOptions = packet.Options; // Host also sets local cache? Not strictly needed but consistent.
					PacketSender.SendToAllClients(packet);
				}
			}
			else
			{
				// Client: Apply received options if available
				ImmigrantScreenPatch.ApplyOptionsToScreen(__instance);
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
