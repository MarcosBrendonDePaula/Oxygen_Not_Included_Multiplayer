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
		
		// Flag to prevent re-syncing once options are locked for this cycle
		public static bool OptionsLocked = false;

		// Flag to skip ApplyOptionsToScreen when InitializeContainers has already created containers
		public static bool ContainersCreatedByPatch = false;

		// Clear the lock when the screen closes or duplicant is printed
		public static void ClearOptionsLock()
		{
			OptionsLocked = false;
			ContainersCreatedByPatch = false;
			AvailableOptions = null;
			
			// Also clear selectedDeliverables to prevent "add beyond limit" errors on reopen
			try
			{
				if (ImmigrantScreen.instance != null)
				{
					var selectedDeliverables = Traverse.Create(ImmigrantScreen.instance).Field("selectedDeliverables").GetValue() as System.Collections.IList;
					if (selectedDeliverables != null)
					{
						selectedDeliverables.Clear();
					}
				}
			}
			catch { }
			
			DebugConsole.Log("[ImmigrantScreen] Options lock cleared");
		}

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

			// Get ALL containers (both CharacterContainer and CarePackageContainer)
			var allContainers = new List<object>();
			if (containersObj is System.Collections.IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					if (item != null) allContainers.Add(item);
				}
			}

			if (allContainers.Count == 0)
			{
				DebugConsole.LogWarning("[ImmigrantScreen] ApplyOptionsToScreen: No containers found!");
				return;
			}

			DebugConsole.Log($"[ImmigrantScreen] ApplyOptionsToScreen: Applying {AvailableOptions.Count} options to {allContainers.Count} containers");

			// Apply options to each container
			int applyCount = System.Math.Min(AvailableOptions.Count, allContainers.Count);
			for (int i = 0; i < applyCount; i++)
			{
				var opt = AvailableOptions[i];
				var container = allContainers[i];
				var containerTraverse = Traverse.Create(container);
				var containerType = container.GetType().Name;

				try
				{
					if (opt.IsDuplicant)
					{
						var personality = Db.Get().Personalities.TryGet(opt.PersonalityId);
						if (personality == null) personality = Db.Get().Personalities.TryGet("Hassan");

						var stats = new MinionStartingStats(personality);
						stats.Name = opt.Name;
						if (!string.IsNullOrEmpty(opt.GenderStringKey))
						{
							Traverse.Create(stats).Field("GenderStringKey").SetValue(opt.GenderStringKey);
						}

						// Try to call SetInfo with MinionStartingStats
						containerTraverse.Method("SetInfo", new System.Type[] { typeof(MinionStartingStats) }, new object[] { stats }).GetValue();
						DebugConsole.Log($"[ImmigrantScreen]   Applied Duplicant {i} to {containerType}: {opt.Name}");
					}
					else
					{
						var pkg = new CarePackageInfo(opt.CarePackageId, opt.Quantity, null);
						
						// Try to call SetInfo with CarePackageInfo
						containerTraverse.Method("SetInfo", new System.Type[] { typeof(CarePackageInfo) }, new object[] { pkg }).GetValue();
						DebugConsole.Log($"[ImmigrantScreen]   Applied CarePackage {i} to {containerType}: {opt.CarePackageId}");
					}
				}
				catch (System.Exception ex)
				{
					DebugConsole.LogError($"[ImmigrantScreen]   Error applying option {i} to {containerType}: {ex.Message}");
				}
			}

			// Hide extra containers if we have fewer options
			for (int i = applyCount; i < allContainers.Count; i++)
			{
				try
				{
					var go = Traverse.Create(allContainers[i]).Method("GetGameObject").GetValue() as UnityEngine.GameObject;
					if (go != null) go.SetActive(false);
				}
				catch { }
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

			// If InitializeContainers already created containers with our data, skip
			if (ImmigrantScreenPatch.ContainersCreatedByPatch)
			{
				DebugConsole.Log("[ImmigrantScreen] Containers already created by patch, skipping Postfix");
				return;
			}

			// If options are already locked but containers weren't created by us, apply them
			if (ImmigrantScreenPatch.OptionsLocked && ImmigrantScreenPatch.AvailableOptions != null && ImmigrantScreenPatch.AvailableOptions.Count > 0)
			{
				DebugConsole.Log($"[ImmigrantScreen] Options already locked, applying {ImmigrantScreenPatch.AvailableOptions.Count} cached options");
				ImmigrantScreenPatch.ApplyOptionsToScreen(__instance);
				return;
			}

			// First-opener-wins: Whoever opens first captures and broadcasts
			// Use a delayed capture because container data isn't ready yet at Initialize time
			// Use Game.Instance because ImmigrantScreen is inactive at this point
			Game.Instance.StartCoroutine(DelayedCaptureAndBroadcast(__instance));
		}

		private static System.Collections.IEnumerator DelayedCaptureAndBroadcast(ImmigrantScreen screen)
		{
			// Wait for end of frame (let containers populate their data)
			yield return null;
			
			// Check again if locked (in case another player's packet arrived)
			if (ImmigrantScreenPatch.OptionsLocked)
			{
				DebugConsole.Log("[ImmigrantScreen] Options locked during delay, applying cached options");
				if (ImmigrantScreenPatch.AvailableOptions != null && ImmigrantScreenPatch.AvailableOptions.Count > 0)
				{
					ImmigrantScreenPatch.ApplyOptionsToScreen(screen);
				}
				yield break;
			}

			CaptureAndBroadcastOptions(screen);
		}

		private static void CaptureAndBroadcastOptions(ImmigrantScreen __instance)
		{
			string role = MultiplayerSession.IsHost ? "Host" : "Client";
			DebugConsole.Log($"[ImmigrantScreen] {role}: Capturing options from containers...");

			// Get containers from ImmigrantScreen (inherited from CharacterSelectionController)
			var containers = Traverse.Create(__instance).Field("containers").GetValue() as System.Collections.IList;
			if (containers == null || containers.Count == 0)
			{
				DebugConsole.LogWarning("[ImmigrantScreen] No containers found in ImmigrantScreen");
				return;
			}

			DebugConsole.Log($"[ImmigrantScreen] Found {containers.Count} containers");

			var packet = new ImmigrantOptionsPacket();

			foreach (var container in containers)
			{
				if (container == null) continue;

				var entry = new ImmigrantOptionsPacket.OptionEntry();

				// containers are ITelepadDeliverableContainer (CharacterContainer or CarePackageContainer)
				// We need to extract the stats or carePackageInfo from inside
				var containerTraverse = Traverse.Create(container);
				
				// Try to get stats (for CharacterContainer - duplicant)
				var stats = containerTraverse.Field("stats").GetValue<MinionStartingStats>();
				if (stats != null)
				{
					entry.IsDuplicant = true;
					entry.Name = stats.Name;
					entry.GenderStringKey = stats.GenderStringKey ?? "NB";
					entry.PersonalityId = stats.personality?.Id ?? "Hassan";
					
					// Capture traits list
					entry.TraitIds = new List<string>();
					if (stats.Traits != null)
					{
						foreach (var trait in stats.Traits)
						{
							if (trait != null) entry.TraitIds.Add(trait.Id);
						}
					}
					
					// Capture special traits
					entry.StressTraitId = stats.stressTrait?.Id ?? "";
					entry.JoyTraitId = stats.joyTrait?.Id ?? "";
					entry.CongenitalTraitId = stats.joyTrait /* TEMP: congenitalTrait removed */?.Id ?? "";
					
					// Capture other stats
					entry.VoiceIdx = stats.voiceIdx;
					entry.StickerType = stats.stickerType ?? "";
					
					// Capture skill aptitudes
					entry.SkillAptitudes = new Dictionary<string, float>();
					if (stats.skillAptitudes != null)
					{
						foreach (var kvp in stats.skillAptitudes)
						{
							if (kvp.Key != null) entry.SkillAptitudes[kvp.Key.Id] = kvp.Value;
						}
					}
					
					// Capture starting levels
					entry.StartingLevels = new Dictionary<string, int>();
					if (stats.StartingLevels != null)
					{
						foreach (var kvp in stats.StartingLevels)
						{
							entry.StartingLevels[kvp.Key] = kvp.Value;
						}
					}
					
					DebugConsole.Log($"[ImmigrantScreen]   Captured Duplicant: {entry.Name} ({entry.PersonalityId}) with {entry.TraitIds.Count} traits");
					packet.Options.Add(entry);
					continue;
				}

				// Try to get carePackageInfo (for CarePackageContainer)
				var pkg = containerTraverse.Field("carePackageInfo").GetValue<CarePackageInfo>();
				if (pkg == null)
				{
					// Also try "info" field name
					pkg = containerTraverse.Field("info").GetValue<CarePackageInfo>();
				}
				if (pkg != null)
				{
					entry.IsDuplicant = false;
					entry.CarePackageId = pkg.id;
					entry.Quantity = pkg.quantity;
					DebugConsole.Log($"[ImmigrantScreen]   Captured CarePackage: {entry.CarePackageId} x{entry.Quantity}");
					packet.Options.Add(entry);
					continue;
				}

				// Unknown container type
				var containerType = container.GetType();
				DebugConsole.Log($"[ImmigrantScreen]   Container {containerType.Name} has no stats or carePackageInfo");
			}

			if (packet.Options.Count > 0)
			{
				// Lock options for this cycle
				ImmigrantScreenPatch.AvailableOptions = packet.Options;
				ImmigrantScreenPatch.OptionsLocked = true;
				
				DebugConsole.Log($"[ImmigrantScreen] {role}: Broadcasting {packet.Options.Count} options (first-opener-wins)");
				
				if (MultiplayerSession.IsHost)
				{
					// Host sends to all clients
					PacketSender.SendToAllClients(packet);
				}
				else
				{
					// Client sends to host (host will rebroadcast)
					PacketSender.SendToHost(packet);
				}
			}
			else
			{
				DebugConsole.LogWarning($"[ImmigrantScreen] {role}: No options to broadcast!");
			}
		}
	}

	[HarmonyPatch(typeof(ImmigrantScreen), "OnProceed")]
	public static class ImmigrantScreenProceedPatch
	{
		public static bool Prefix(ImmigrantScreen __instance)
		{
			if (MultiplayerSession.IsHost)
			{
				// Host: Clear the lock after printing (Postfix will handle this)
				return true;
			}

			// Client selected something and clicked Print.
			// We need to find what was selected.
			var selectedObj = Traverse.Create(__instance).Field("selectedContainer").GetValue();

			int selectedIndex = -1;
			if (selectedObj != null)
			{
				DebugConsole.Log($"[ImmigrantScreen] Client: selectedObj type = {selectedObj.GetType().Name}");
				
				// Find index in __instance.containers
				var containersObj = Traverse.Create(__instance).Field("containers").GetValue();
				if (containersObj != null && containersObj is System.Collections.IList containersList)
				{
					DebugConsole.Log($"[ImmigrantScreen] Client: containers count = {containersList.Count}");
					
					for (int i = 0; i < containersList.Count; i++)
					{
						var container = containersList[i];
						if (container != null)
						{
							// Try both reference equality and object.Equals
							if (object.ReferenceEquals(container, selectedObj) || container == selectedObj || container.Equals(selectedObj))
							{
								selectedIndex = i;
								break;
							}
						}
					}
				}
			}
			else
			{
				DebugConsole.LogWarning("[ImmigrantScreen] Client: selectedContainer is null, trying selectedDeliverables...");
				
				// Try to find from selectedDeliverables instead
				var selectedDelis = Traverse.Create(__instance).Field("selectedDeliverables").GetValue() as System.Collections.IList;
				if (selectedDelis != null && selectedDelis.Count > 0)
				{
					var selectedDeli = selectedDelis[0]; // Get the first selected deliverable
					DebugConsole.Log($"[ImmigrantScreen] Client: Found selectedDeliverable type = {selectedDeli?.GetType().Name}");
					
					// Find which container has this deliverable
					var containersObj = Traverse.Create(__instance).Field("containers").GetValue();
					if (containersObj != null && containersObj is System.Collections.IList containersList)
					{
						for (int i = 0; i < containersList.Count; i++)
						{
							var container = containersList[i];
							if (container == null) continue;
							
							// Get the deliverable from the container and compare
							// CharacterContainer has 'stats' (MinionStartingStats)
							// CarePackageContainer has 'info' (CarePackageInfo) and 'carePackageInstanceData' (CarePackageInstanceData)
							var containerStats = Traverse.Create(container).Field("stats").GetValue();
							var containerInfo = Traverse.Create(container).Field("info").GetValue();
							var containerInstanceData = Traverse.Create(container).Field("carePackageInstanceData").GetValue();
							
							if ((containerStats != null && object.ReferenceEquals(containerStats, selectedDeli)) ||
								(containerInfo != null && object.ReferenceEquals(containerInfo, selectedDeli)) ||
								(containerInstanceData != null && object.ReferenceEquals(containerInstanceData, selectedDeli)))
							{
								selectedIndex = i;
								DebugConsole.Log($"[ImmigrantScreen] Client: Found matching container at index {i}");
								break;
							}
						}
					}
					
					if (selectedIndex == -1)
					{
						DebugConsole.LogWarning("[ImmigrantScreen] Client: Could not match selectedDeliverable to any container");
					}
				}
				else
				{
					DebugConsole.LogWarning("[ImmigrantScreen] Client: selectedDeliverables is empty/null");
				}
			}

			if (selectedIndex != -1)
			{
				DebugConsole.Log($"[ImmigrantScreen] Client: Selected index {selectedIndex}, sending to host");
				var packet = new ImmigrantSelectionPacket { SelectedDeliverableIndex = selectedIndex, PrintingPodWorldIndex = __instance.Telepad?.GetMyWorldId() ?? 0 };
				PacketSender.SendToHost(packet);
			}
			else
			{
				DebugConsole.LogWarning("[ImmigrantScreen] Client: Could not find selected index");
			}

			// Clear the options lock for the next cycle
			ImmigrantScreenPatch.ClearOptionsLock();

			// Suppress local printing - host handles it
			__instance.Deactivate();
			return false;
		}

		public static void Postfix()
		{
			// Host: Clear the lock after printing and notify clients
			if (MultiplayerSession.IsHost)
			{
				DebugConsole.Log("[ImmigrantScreen] Host: Selection made via screen, notifying clients to close");
				
				// Send -2 to close client screens
				// NOTE: For host's own selections via OnProceed, the game spawns the entity normally
				// Entity sync will be handled separately (e.g. via EntitySpawnPacket from a different hook)
				var packet = new ImmigrantSelectionPacket { SelectedDeliverableIndex = -2 };
				PacketSender.SendToAllClients(packet);
				
				ImmigrantScreenPatch.ClearOptionsLock();
			}
		}
	}

	// Patch for Reject All button
	[HarmonyPatch(typeof(ImmigrantScreen), "OnRejectAll")]
	public static class ImmigrantScreenRejectPatch
	{
		public static bool Prefix(ImmigrantScreen __instance)
		{
			if (!MultiplayerSession.InSession) return true;
			
			DebugConsole.Log("[ImmigrantScreen] Reject All clicked");
			
			if (MultiplayerSession.IsClient)
			{
				// Client: Send reject to host
				DebugConsole.Log("[ImmigrantScreen] Client: Sending Reject All to host");
				var packet = new ImmigrantSelectionPacket { SelectedDeliverableIndex = -1 };
				PacketSender.SendToHost(packet);
				
				// Clear local options and close screen
				ImmigrantScreenPatch.ClearOptionsLock();
				if (ImmigrantScreen.instance != null)
				{
					ImmigrantScreen.instance.Deactivate();
				}
				
				return false; // Don't execute original
			}
			
			// Host: Let original execute, Postfix will notify clients
			return true;
		}
		
		public static void Postfix()
		{
			if (!MultiplayerSession.InSession) return;
			
			if (MultiplayerSession.IsHost)
			{
				DebugConsole.Log("[ImmigrantScreen] Host: Reject All, notifying clients");
				
				// Notify clients to close their screens
				var packet = new ImmigrantSelectionPacket { SelectedDeliverableIndex = -1 };
				PacketSender.SendToAllClients(packet);
				
				ImmigrantScreenPatch.ClearOptionsLock();
			}
		}
	}


	// Patch InitializeContainers to create correct container types based on cached options
	[HarmonyPatch(typeof(CharacterSelectionController), "InitializeContainers")]
	public static class InitializeContainersPatch
	{
		public static bool Prefix(CharacterSelectionController __instance)
		{
			if (!MultiplayerSession.InSession) return true;
			
			// Only take control if we have locked options from another player
			if (!ImmigrantScreenPatch.OptionsLocked || ImmigrantScreenPatch.AvailableOptions == null || ImmigrantScreenPatch.AvailableOptions.Count == 0)
			{
				return true; // Let original run
			}

			DebugConsole.Log($"[ImmigrantScreen] InitializeContainers: Taking control, creating {ImmigrantScreenPatch.AvailableOptions.Count} containers based on cached options");

			try
			{
				var traverse = Traverse.Create(__instance);
				
				// Get prefabs
				var containerPrefab = traverse.Field("containerPrefab").GetValue<CharacterContainer>();
				var carePackageContainerPrefab = traverse.Field("carePackageContainerPrefab").GetValue<CarePackageContainer>();
				var containerParent = traverse.Field("containerParent").GetValue<UnityEngine.GameObject>();

				if (containerPrefab == null || containerParent == null)
				{
					DebugConsole.LogWarning("[ImmigrantScreen] InitializeContainers: Missing prefabs, falling back to original");
					return true;
				}

				// Clear existing containers
				var containers = traverse.Field("containers").GetValue() as System.Collections.IList;
				if (containers != null)
				{
					foreach (var c in containers)
					{
						if (c != null)
						{
							try 
							{ 
								var go = Traverse.Create(c).Method("GetGameObject").GetValue() as UnityEngine.GameObject;
								if (go != null) UnityEngine.Object.Destroy(go);
							}
							catch { }
						}
					}
					containers.Clear();
				}
					else
				{
					// Create new list using reflection
					containers = new List<ITelepadDeliverableContainer>();
					traverse.Field("containers").SetValue(containers);
				}

				// Clear selectedDeliverables to prevent "add beyond limit" errors
				var selectedDeliverables = traverse.Field("selectedDeliverables").GetValue() as System.Collections.IList;
				if (selectedDeliverables == null)
				{
					traverse.Field("selectedDeliverables").SetValue(new List<ITelepadDeliverable>());
				}
				else
				{
					selectedDeliverables.Clear();
				}
				
				// Count duplicants and care packages
				int duplicantCount = 0;
				int carePackageCount = 0;
				foreach (var opt in ImmigrantScreenPatch.AvailableOptions)
				{
					if (opt.IsDuplicant) duplicantCount++;
					else carePackageCount++;
				}
				
				// Set the option limits so AddDeliverable works correctly
				traverse.Field("numberOfDuplicantOptions").SetValue(duplicantCount);
				traverse.Field("numberOfCarePackageOptions").SetValue(carePackageCount);
				DebugConsole.Log($"[ImmigrantScreen] Set limits: {duplicantCount} duplicants, {carePackageCount} care packages");

				// Store containers and their corresponding options for delayed SetInfo
				var createdContainers = new List<object>();
				var containerOptions = new List<ImmigrantOptionsPacket.OptionEntry>();

				// Create containers based on cached options (BUT DON'T SET INFO YET)
				// GenerateCharacter runs in Start(), so we need to call SetInfo after that
				foreach (var opt in ImmigrantScreenPatch.AvailableOptions)
				{
					if (opt.IsDuplicant)
					{
						// Create CharacterContainer
						var newContainer = Util.KInstantiateUI<CharacterContainer>(containerPrefab.gameObject, containerParent);
						newContainer.SetController(__instance);
						containers.Add(newContainer);
						createdContainers.Add(newContainer);
						containerOptions.Add(opt);
						DebugConsole.Log($"[ImmigrantScreen]   Created CharacterContainer for: {opt.Name} (SetInfo will be called after delay)");
					}
					else
					{
						// Create CarePackageContainer
						if (carePackageContainerPrefab != null)
						{
							var newContainer = Util.KInstantiateUI<CarePackageContainer>(carePackageContainerPrefab.gameObject, containerParent);
							newContainer.SetController(__instance);
							containers.Add(newContainer);
							createdContainers.Add(newContainer);
							containerOptions.Add(opt);
							DebugConsole.Log($"[ImmigrantScreen]   Created CarePackageContainer for: {opt.CarePackageId} (SetInfo will be called after delay)");
						}
						else
						{
							DebugConsole.LogWarning($"[ImmigrantScreen]   CarePackageContainer prefab is null, skipping {opt.CarePackageId}");
						}
					}
				}

				// Disable the proceed button initially
				traverse.Method("DisableProceedButton").GetValue();

				DebugConsole.Log($"[ImmigrantScreen] InitializeContainers: Created {containers.Count} containers, starting delayed SetInfo");
				
				// Set flag to skip redundant ApplyOptionsToScreen in Initialize Postfix
				ImmigrantScreenPatch.ContainersCreatedByPatch = true;

				// Start coroutine to apply SetInfo after GenerateCharacter runs (after 1 frame)
				Game.Instance.StartCoroutine(DelayedSetInfo(createdContainers, containerOptions));
				
				return false; // Skip original
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[ImmigrantScreen] InitializeContainers failed: {ex.Message}");
				return true; // Fall back to original
			}
		}

		private static System.Collections.IEnumerator DelayedSetInfo(List<object> containers, List<ImmigrantOptionsPacket.OptionEntry> options)
		{
			// Wait for end of frame to let GenerateCharacter run in Start()
			yield return null;

			DebugConsole.Log($"[ImmigrantScreen] DelayedSetInfo: Applying synced data to {containers.Count} containers");

			for (int i = 0; i < containers.Count && i < options.Count; i++)
			{
				var container = containers[i];
				var opt = options[i];

				try
				{
					if (opt.IsDuplicant && container is CharacterContainer charContainer)
					{
						var personality = Db.Get().Personalities.TryGet(opt.PersonalityId);
						if (personality == null) personality = Db.Get().Personalities.TryGet("Hassan");

						var stats = new MinionStartingStats(personality);
						stats.Name = opt.Name;
						
						// Set GenderStringKey
						if (!string.IsNullOrEmpty(opt.GenderStringKey))
						{
							Traverse.Create(stats).Field("GenderStringKey").SetValue(opt.GenderStringKey);
						}
						
						// Apply traits
						if (opt.TraitIds != null && opt.TraitIds.Count > 0)
						{
							stats.Traits.Clear();
							foreach (var traitId in opt.TraitIds)
							{
								var trait = Db.Get().traits.TryGet(traitId);
								if (trait != null) stats.Traits.Add(trait);
							}
						}
						
						// Apply special traits
						if (!string.IsNullOrEmpty(opt.StressTraitId))
						{
							var stressTrait = Db.Get().traits.TryGet(opt.StressTraitId);
							if (stressTrait != null) stats.stressTrait = stressTrait;
						}
						if (!string.IsNullOrEmpty(opt.JoyTraitId))
						{
							var joyTrait = Db.Get().traits.TryGet(opt.JoyTraitId);
							if (joyTrait != null) stats.joyTrait = joyTrait;
						}
						if (!string.IsNullOrEmpty(opt.CongenitalTraitId))
						{
							var congenitalTrait = Db.Get().traits.TryGet(opt.CongenitalTraitId);
							if (congenitalTrait != null) stats.joyTrait /* TEMP: congenitalTrait removed */ = congenitalTrait;
						}
						
						// Apply other stats
						stats.voiceIdx = opt.VoiceIdx;
						stats.stickerType = opt.StickerType;
						
						// Apply skill aptitudes
						if (opt.SkillAptitudes != null && opt.SkillAptitudes.Count > 0)
						{
							stats.skillAptitudes.Clear();
							foreach (var kvp in opt.SkillAptitudes)
							{
								var skillGroup = Db.Get().SkillGroups.TryGet(kvp.Key);
								if (skillGroup != null) stats.skillAptitudes[skillGroup] = kvp.Value;
							}
						}
						
						// Apply starting levels
						if (opt.StartingLevels != null && opt.StartingLevels.Count > 0)
						{
							stats.StartingLevels.Clear();
							foreach (var kvp in opt.StartingLevels)
							{
								stats.StartingLevels[kvp.Key] = kvp.Value;
							}
						}

						// Set the stats field directly
						Traverse.Create(charContainer).Field("stats").SetValue(stats);
						
						// Update the visual elements
						Traverse.Create(charContainer).Method("SetInfoText").GetValue();
						Traverse.Create(charContainer).Method("SetAnimator").GetValue();
						
						DebugConsole.Log($"[ImmigrantScreen]   Applied Duplicant {i}: {opt.Name} with {opt.TraitIds?.Count ?? 0} traits");
					}
					else if (!opt.IsDuplicant && container is CarePackageContainer pkgContainer)
					{
						var pkg = new CarePackageInfo(opt.CarePackageId, opt.Quantity, null);
						
						// Clear old visual by destroying the animController's content
						try
						{
							var animController = Traverse.Create(pkgContainer).Field("animController").GetValue();
							if (animController != null)
							{
								var animGO = Traverse.Create(animController).Property("gameObject").GetValue() as UnityEngine.GameObject;
								if (animGO != null)
								{
									// Destroy all children to clear old visuals
									foreach (UnityEngine.Transform child in animGO.transform)
									{
										UnityEngine.Object.Destroy(child.gameObject);
									}
								}
							}
							
							// Also try to clear fgImage
							var fgImage = Traverse.Create(pkgContainer).Field("fgImage").GetValue() as UnityEngine.UI.Image;
							if (fgImage != null)
							{
								fgImage.sprite = null;
							}
						}
						catch { }
						
						// Set the info field directly
						Traverse.Create(pkgContainer).Field("info").SetValue(pkg);
						
						// Also try to create and set carePackageInstanceData
						try
						{
							// CarePackageInstanceData has: CarePackageInfo info, string facadeID
							var instanceDataType = typeof(CarePackageContainer).GetNestedType("CarePackageInstanceData");
							if (instanceDataType != null)
							{
								var instanceData = System.Activator.CreateInstance(instanceDataType);
								Traverse.Create(instanceData).Field("info").SetValue(pkg);
								Traverse.Create(pkgContainer).Field("carePackageInstanceData").SetValue(instanceData);
							}
						}
						catch { }
						
						// Clear old entry icons
						try
						{
							var entryIcons = Traverse.Create(pkgContainer).Field("entryIcons").GetValue() as System.Collections.IList;
							if (entryIcons != null)
							{
								foreach (var icon in entryIcons)
								{
									var go = icon as UnityEngine.GameObject;
									if (go != null) UnityEngine.Object.Destroy(go);
								}
								entryIcons.Clear();
							}
						}
						catch { }
						
						// Update the visuals using SetAnimator (don't call GenerateCharacter as it regenerates random data)
						try { Traverse.Create(pkgContainer).Method("SetAnimator").GetValue(); } catch { }
						
						// Update text
						Traverse.Create(pkgContainer).Method("SetInfoText").GetValue();
						
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
}


