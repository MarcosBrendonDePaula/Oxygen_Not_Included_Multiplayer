using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Events;
using UnityEngine;
using static STRINGS.UI.OUTFITS;

namespace ONI_MP.Patches.GamePatches
{
    [HarmonyPatch(typeof(EventExtensions))]
    public static class Patch_EventExtensions_Trigger
    {
        // Patch the static method: public static void Trigger(this GameObject go, int hash, object data = null)
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EventExtensions.Trigger))]
        public static bool Prefix(GameObject go, int hash, object data)
        {
            Debug.Log($"[MP] Trigger intercepted: {go.name} -> {hash}, Data: {data}");

            KObject kObject = KObjectManager.Instance.Get(go);
            if (kObject != null && kObject.hasEventSystem)
            {
                kObject.GetEventSystem().Trigger(go, hash, data);
                if(MultiplayerSession.IsHost)
                {
                    NetworkIdentity identity = go.GetComponent<NetworkIdentity>();
                    if(identity != null)
                    {
                        var packet = new EventTriggeredPacket(identity.NetId, hash, data);
                        PacketSender.SendToAllClients(packet);
                    }
                }
            }
            return false;
        }
    }
}
