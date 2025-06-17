using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Patches.KleiPatches
{
    [HarmonyPatch(typeof(KAnimControllerBase), nameof(KAnimControllerBase.Play), new[] {
        typeof(HashedString), typeof(KAnim.PlayMode), typeof(float), typeof(float)
    })]
    class KAnimControllerBasePatch
    {
        static void Prefix(KAnimControllerBase __instance, HashedString anim_name, KAnim.PlayMode mode, float speed, float time_offset)
        {
            var go = __instance?.gameObject;
            if (go != null && go.TryGetComponent<KPrefabID>(out var id) && id.HasTag(GameTags.Minions.Models.Standard))
            {
                DebugConsole.Log($"[ONI_MP] Dupe '{go.name}' playing anim '{anim_name}' | Mode: {mode}, Speed: {speed}, Offset: {time_offset}");

                if (MultiplayerSession.IsHost && go.TryGetComponent<NetworkIdentity>(out var netIdentity))
                {
                    var packet = new PlayAnimPacket
                    {
                        NetId = netIdentity.NetId,
                        AnimHash = anim_name.HashValue,
                        Mode = mode,
                        Speed = speed,
                        Offset = time_offset
                    };

                    PacketSender.SendToAllClients(packet);
                }
            }
        }
    }
}
