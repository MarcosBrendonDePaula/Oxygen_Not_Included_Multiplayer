using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace ONI_MP.Patches.KleiPatches
{
    // Patch for Play(HashedString, KAnim.PlayMode, float, float)
    [HarmonyPatch(typeof(KAnimControllerBase))]
    class KAnimControllerBasePatch
    {
        [HarmonyPatch(nameof(KAnimControllerBase.Play), new[] {
            typeof(HashedString), typeof(KAnim.PlayMode), typeof(float), typeof(float)
        })]
        [HarmonyPrefix]
        static void Play_Single_Prefix(KAnimControllerBase __instance, HashedString anim_name, KAnim.PlayMode mode, float speed, float time_offset)
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
                        IsMulti = false,
                        SingleAnimHash = anim_name.HashValue,
                        Mode = mode,
                        Speed = speed,
                        Offset = time_offset
                    };

                    PacketSender.SendToAllClients(packet);
                }
            }
        }

        // Patch for Play(HashedString[], KAnim.PlayMode)
        [HarmonyPatch(nameof(KAnimControllerBase.Play), new[] {
            typeof(HashedString[]), typeof(KAnim.PlayMode)
        })]
        [HarmonyPrefix]
        static void Play_Multi_Prefix(KAnimControllerBase __instance, HashedString[] anim_names, KAnim.PlayMode mode)
        {
            var go = __instance?.gameObject;
            if (go != null && go.TryGetComponent<KPrefabID>(out var id) && id.HasTag(GameTags.Minions.Models.Standard))
            {
                string allAnims = string.Join(", ", anim_names.Select(a => a.ToString()));
                DebugConsole.Log($"[ONI_MP] Dupe '{go.name}' playing anims [{allAnims}] | Mode: {mode}");

                if (MultiplayerSession.IsHost && go.TryGetComponent<NetworkIdentity>(out var netIdentity))
                {
                    var packet = new PlayAnimPacket
                    {
                        NetId = netIdentity.NetId,
                        IsMulti = true,
                        AnimHashes = anim_names.Select(a => a.HashValue).ToList(),
                        Mode = mode,
                        Speed = 1f,        // Default values, Play(string[]) doesn't pass speed/offset
                        Offset = 0f
                    };

                    PacketSender.SendToAllClients(packet);
                }
            }
        }
    }
}
