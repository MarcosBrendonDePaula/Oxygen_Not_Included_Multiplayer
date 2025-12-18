using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.Social
{
	public class ImmigrantSelectionPacket : IPacket
	{
		public PacketType Type => PacketType.ImmigrantSelection;

		public int SelectedIndex; // 0, 1, 2... or -1 for Reject All?

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(SelectedIndex);
		}

		public void Deserialize(BinaryReader reader)
		{
			SelectedIndex = reader.ReadInt32();
		}

		public void OnDispatched()
		{
			DebugConsole.Log($"[ImmigrantSelectionPacket] Received selection: index {SelectedIndex}, IsHost: {MultiplayerSession.IsHost}");

			// Handle client receiving notification from host
			if (!MultiplayerSession.IsHost)
			{
				// Host made a selection - client should also spawn
				if (SelectedIndex >= 0)
				{
					DebugConsole.Log($"[ImmigrantSelectionPacket] Client: Host selected index {SelectedIndex}, spawning locally");
					
					var options = ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.AvailableOptions;
					if (options != null && SelectedIndex < options.Count)
					{
						var opt = options[SelectedIndex];
						
						try
						{
							var telepad = UnityEngine.Object.FindObjectOfType<Telepad>();
							if (telepad != null)
							{
								if (opt.IsDuplicant)
								{
									var personality = Db.Get().Personalities.TryGet(opt.PersonalityId);
									if (personality == null) personality = Db.Get().Personalities.TryGet("Hassan");
									
									var stats = new MinionStartingStats(personality);
									stats.Name = opt.Name;
									
									if (opt.TraitIds != null)
									{
										stats.Traits.Clear();
										foreach (var traitId in opt.TraitIds)
										{
											var trait = Db.Get().traits.TryGet(traitId);
											if (trait != null) stats.Traits.Add(trait);
										}
									}
									
									telepad.OnAcceptDelivery(stats);
									DebugConsole.Log($"[ImmigrantSelectionPacket] Client: Spawned duplicant: {opt.Name}");
								}
								else
								{
									var pkg = new CarePackageInfo(opt.CarePackageId, opt.Quantity, null);
									telepad.OnAcceptDelivery(pkg);
									DebugConsole.Log($"[ImmigrantSelectionPacket] Client: Spawned care package: {opt.CarePackageId} x{opt.Quantity}");
								}
							}
						}
						catch (System.Exception ex)
						{
							DebugConsole.LogError($"[ImmigrantSelectionPacket] Client spawn failed: {ex.Message}");
						}
					}
					
					// Clear options, close screen, reset Immigration
					ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.ClearOptionsLock();
					
					if (ImmigrantScreen.instance != null && ImmigrantScreen.instance.gameObject.activeInHierarchy)
					{
						ImmigrantScreen.instance.Deactivate();
					}
					
					try
					{
						if (Immigration.Instance != null)
						{
							Traverse.Create(Immigration.Instance).Method("EndImmigration").GetValue();
						}
					}
					catch { }
				}
				else if (SelectedIndex == -2)
				{
					// Fallback: just close screen (legacy)
					DebugConsole.Log("[ImmigrantSelectionPacket] Client: Closing screen (no spawn data)");
					ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.ClearOptionsLock();
					
					if (ImmigrantScreen.instance != null && ImmigrantScreen.instance.gameObject.activeInHierarchy)
					{
						ImmigrantScreen.instance.Deactivate();
					}
					
					try
					{
						if (Immigration.Instance != null)
						{
							Traverse.Create(Immigration.Instance).Method("EndImmigration").GetValue();
						}
					}
					catch { }
				}
				return;
			}

			// Host received selection from client - ALWAYS use direct spawn from AvailableOptions
			// (Host's screen might have different containers than client's AvailableOptions)
			DebugConsole.Log("[ImmigrantSelectionPacket] Host: Processing client selection using AvailableOptions");
			
			// Close host's screen if open
			if (ImmigrantScreen.instance != null && ImmigrantScreen.instance.gameObject.activeInHierarchy)
			{
				ImmigrantScreen.instance.Deactivate();
			}
			{
				// Screen is closed - spawn directly using cached options
				DebugConsole.Log("[ImmigrantSelectionPacket] Host: Screen is closed, spawning using cached options");
				
				var options = ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.AvailableOptions;
				if (options == null || SelectedIndex < 0 || SelectedIndex >= options.Count)
				{
					DebugConsole.LogWarning($"[ImmigrantSelectionPacket] Invalid selection - options: {options?.Count ?? 0}, index: {SelectedIndex}");
					return;
				}
				
				var opt = options[SelectedIndex];
				
				try
				{
					// Find the Telepad
					var telepad = UnityEngine.Object.FindObjectOfType<Telepad>();
					if (telepad == null)
					{
						DebugConsole.LogWarning("[ImmigrantSelectionPacket] Cannot find Telepad");
						return;
					}
					
					if (opt.IsDuplicant)
					{
						// Spawn duplicant via Telepad.OnAcceptDelivery
						var personality = Db.Get().Personalities.TryGet(opt.PersonalityId);
						if (personality == null) personality = Db.Get().Personalities.TryGet("Hassan");
						
						var stats = new MinionStartingStats(personality);
						stats.Name = opt.Name;
						
						// Apply traits etc from synced data
						if (opt.TraitIds != null)
						{
							stats.Traits.Clear();
							foreach (var traitId in opt.TraitIds)
							{
								var trait = Db.Get().traits.TryGet(traitId);
								if (trait != null) stats.Traits.Add(trait);
							}
						}
						
						// Use proper telepad spawn method
						telepad.OnAcceptDelivery(stats);
						DebugConsole.Log($"[ImmigrantSelectionPacket] Spawned duplicant via Telepad: {opt.Name}");
					}
					else
					{
						// Spawn care package via Telepad.OnAcceptDelivery
						var pkg = new CarePackageInfo(opt.CarePackageId, opt.Quantity, null);
						telepad.OnAcceptDelivery(pkg);
						DebugConsole.Log($"[ImmigrantSelectionPacket] Spawned care package via Telepad: {opt.CarePackageId} x{opt.Quantity}");
					}
					
					// End immigration cycle
					if (Immigration.Instance != null)
					{
						Traverse.Create(Immigration.Instance).Method("EndImmigration").GetValue();
					}
					
					// Clear options and notify clients
					ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.ClearOptionsLock();
					
					// Notify all clients with the actual selected index so they can spawn too
					var notifyPacket = new ImmigrantSelectionPacket { SelectedIndex = SelectedIndex };
					PacketSender.SendToAllClients(notifyPacket);
				}
				catch (System.Exception ex)
				{
					DebugConsole.LogError($"[ImmigrantSelectionPacket] Failed to spawn: {ex.Message}");
				}
			}
			
			if (SelectedIndex == -1) // Reject All
			{
				if (ImmigrantScreen.instance != null)
				{
					ImmigrantScreen.instance.Deactivate();
				}
				DebugConsole.Log("[ImmigrantSelectionPacket] Host rejected all");
			}
		}
	}
}
