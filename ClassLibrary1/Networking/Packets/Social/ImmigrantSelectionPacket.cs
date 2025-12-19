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
				// Host sends -2 when selection is made
				// EntitySpawnPacket is sent separately to handle actual spawning with correct NetId
				if (SelectedIndex == -2)
				{
					DebugConsole.Log("[ImmigrantSelectionPacket] Client: Host made selection, closing screen and resetting Immigration");
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
						
						// Use Deliver() instead of OnAcceptDelivery to get the spawned object
						var position = telepad.transform.position;
						var spawnedGO = stats.Deliver(position);
						
						if (spawnedGO != null)
						{
							var identity = spawnedGO.GetComponent<ONI_MP.Networking.Components.NetworkIdentity>();
							if (identity != null)
							{
								// Send EntitySpawnPacket to clients
								var spawnPacket = new ONI_MP.Networking.Packets.World.EntitySpawnPacket
								{
									NetId = identity.NetId,
									IsDuplicant = true,
									Name = opt.Name,
									PersonalityId = opt.PersonalityId,
									TraitIds = opt.TraitIds,
									PosX = position.x,
									PosY = position.y
								};
								PacketSender.SendToAllClients(spawnPacket);
								DebugConsole.Log($"[ImmigrantSelectionPacket] Host: Sent EntitySpawnPacket for duplicant {opt.Name} (NetId: {identity.NetId})");
							}
						}
						
						DebugConsole.Log($"[ImmigrantSelectionPacket] Spawned duplicant via Telepad: {opt.Name}");
					}
					else
					{
						// Spawn care package via Deliver
						var pkg = new CarePackageInfo(opt.CarePackageId, opt.Quantity, null);
						var position = telepad.transform.position;
						var spawnedGO = pkg.Deliver(position);
						
						if (spawnedGO != null)
						{
							var identity = spawnedGO.GetComponent<ONI_MP.Networking.Components.NetworkIdentity>();
							if (identity == null)
							{
								// Care packages may not have NetworkIdentity, add one
								identity = spawnedGO.AddComponent<ONI_MP.Networking.Components.NetworkIdentity>();
							}
							
							if (identity != null)
							{
								// Send EntitySpawnPacket to clients
								var spawnPacket = new ONI_MP.Networking.Packets.World.EntitySpawnPacket
								{
									NetId = identity.NetId,
									IsDuplicant = false,
									ItemId = opt.CarePackageId,
									Quantity = opt.Quantity,
									PosX = position.x,
									PosY = position.y
								};
								PacketSender.SendToAllClients(spawnPacket);
								DebugConsole.Log($"[ImmigrantSelectionPacket] Host: Sent EntitySpawnPacket for item {opt.CarePackageId} (NetId: {identity.NetId})");
							}
						}
						
						DebugConsole.Log($"[ImmigrantSelectionPacket] Spawned care package via Telepad: {opt.CarePackageId} x{opt.Quantity}");
					}
					
					// End immigration cycle
					if (Immigration.Instance != null)
					{
						Traverse.Create(Immigration.Instance).Method("EndImmigration").GetValue();
					}
					
					// Clear options and notify clients (with -2 to just close screens, EntitySpawnPacket handles spawning)
					ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.ClearOptionsLock();
					
					// Send close screen notification
					var notifyPacket = new ImmigrantSelectionPacket { SelectedIndex = -2 };
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
