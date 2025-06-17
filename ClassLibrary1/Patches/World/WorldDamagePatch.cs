using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.World;
using UnityEngine;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch(typeof(WorldDamage), nameof(WorldDamage.OnDigComplete))]
    public static class WorldDamagePatch
    {
        [HarmonyPrefix]
        public static void Postfix(
            int cell,
            float mass,
            float temperature,
            ushort element_idx,
            byte disease_idx,
            int disease_count)
        {
            // Only intercept on host
            if (MultiplayerSession.IsHost)
            {
                Vector3 pos = Grid.CellToPos(cell, CellAlignment.RandomInternal, Grid.SceneLayer.Ore);

                var packet = new WorldDamageSpawnResourcePacket
                {
                    Position = pos,
                    Mass = mass * 0.5f,
                    Temperature = temperature,
                    ElementIndex = element_idx,
                    DiseaseIndex = disease_idx,
                    DiseaseCount = disease_count
                };

                PacketSender.SendToAllClients(packet);
            }
        }
    }
}
