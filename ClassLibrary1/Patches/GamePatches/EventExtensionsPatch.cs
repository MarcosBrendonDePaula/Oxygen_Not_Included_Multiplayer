using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Events;
using System;
using UnityEngine;

namespace ONI_MP.Patches.GamePatches
{
	[HarmonyPatch(typeof(EventExtensions))]
	public static class EventExtensionsPatch
	{
		// This is a lil bit of a jank way of doing it. TODO Improve this.
		[HarmonyPrefix]
		[HarmonyPatch(nameof(EventExtensions.Trigger), new Type[] { typeof(GameObject), typeof(int), typeof(object) })]
		public static bool Prefix(GameObject go, int hash, object data)
		{
			//DebugConsole.Log($"[MP] Trigger intercepted: {go.name} -> {hash}, Data: {data}");

			KObject kObject = KObjectManager.Instance.Get(go);
			if (kObject != null && kObject.hasEventSystem)
			{
				kObject.GetEventSystem(out var eventSystem);
				eventSystem.Trigger(go, hash, data);
				if (MultiplayerSession.IsHost)
				{
					NetworkIdentity identity = go.GetComponent<NetworkIdentity>();
					if (identity != null)
					{
						var packet = new EventTriggeredPacket(identity.NetId, hash, data);
						PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
					}
				}
			}
			return false;
		}
	}
}
