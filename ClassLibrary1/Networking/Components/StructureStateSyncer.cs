using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.World;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class StructureStateSyncer : KMonoBehaviour
	{
		private float sendInterval = 0.5f; // Sync every 500ms
		private float timer;

		private Battery battery;
		private Generator generator;
		private Operational operational;
		private int cell;

		private float lastSentValue;
		private bool lastSentActive;

		protected override void OnSpawn()
		{
			base.OnSpawn();

			if (!MultiplayerSession.InSession)
			{
				enabled = false;
				return;
			}

			cell = Grid.PosToCell(this);
			battery = GetComponent<Battery>();
			generator = GetComponent<Generator>();
			operational = GetComponent<Operational>();

			if (battery == null && generator == null)
			{
				// Not a relevant structure
				enabled = false;
			}
		}

		private void Update()
		{
			if (MultiplayerSession.IsHost)
			{
				HostUpdate();
			}
		}

		private void HostUpdate()
		{
			try
			{
				timer += Time.unscaledDeltaTime;
				if (timer < sendInterval) return;
				timer = 0f;

				DebugConsole.Log($"[StructureStateSyncer] HostUpdate at cell {cell}");

				float currentValue = 0f;
				bool currentActive = false;

				if (battery != null)
				{
					DebugConsole.Log("[StructureStateSyncer] Reading battery.JoulesAvailable");
					currentValue = battery.JoulesAvailable;
					DebugConsole.Log("[StructureStateSyncer] Battery read complete");
				}

				if (operational != null)
				{
					DebugConsole.Log("[StructureStateSyncer] Reading operational.IsActive");
					currentActive = operational.IsActive;
					DebugConsole.Log("[StructureStateSyncer] Operational read complete");
				}

				// Sync if changed significantly
				if (Mathf.Abs(currentValue - lastSentValue) > 0.1f || currentActive != lastSentActive)
				{
					lastSentValue = currentValue;
					lastSentActive = currentActive;

					var packet = new StructureStatePacket
					{
						Cell = cell,
						Value = currentValue,
						IsActive = currentActive
					};
					DebugConsole.Log("[StructureStateSyncer] Sending packet");
					PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
					DebugConsole.Log("[StructureStateSyncer] Packet sent");
				}
			}
			catch (System.Exception ex)
			{
				DebugConsole.LogError($"[StructureStateSyncer] Exception: {ex}");
			}
		}

		// Static handler for client-side reception
		public static void HandlePacket(StructureStatePacket packet)
		{
			if (!Grid.IsValidCell(packet.Cell)) return;

			GameObject go = Grid.Objects[packet.Cell, (int)Grid.SceneLayer.Building];
			if (go == null) return;

			// Apply state
			var battery = go.GetComponent<Battery>();
			if (battery != null)
			{
				// JoulesAvailable is read-only, set backing field via reflection
				try
				{
					var field = typeof(Battery).GetField("joulesAvailable", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
					if (field != null)
					{
						field.SetValue(battery, packet.Value);
					}
				}
				catch (System.Exception ex)
				{
					DebugConsole.LogError($"[StructureStateSyncer] Failed to set battery joules: {ex}");
				}
			}

			var operational = go.GetComponent<Operational>();
			if (operational != null)
			{
				operational.SetActive(packet.IsActive);
			}
		}
	}
}
