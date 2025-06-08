using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.ONI_MP.Networking.Packets;
using Steamworks;
using static STRINGS.UI.SANDBOXTOOLS.SETTINGS;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(SimMessages), nameof(SimMessages.ModifyCell))]
    public static class SimMessagesPatch
    {
        [HarmonyPrefix]
        public static void Prefix(
            int gameCell,
            ushort elementIdx,
            float temperature,
            float mass,
            byte disease_idx,
            int disease_count,
            SimMessages.ReplaceType replace_type,
            bool do_vertical_solid_displacement,
            int callbackIdx
        )
        {
            if (!MultiplayerSession.IsHost) return;
            if (!Grid.IsValidCell(gameCell)) return;

            var packet = new WorldUpdatePacket();
            packet.Updates.Add(new WorldUpdatePacket.CellUpdate
            {
                Cell = gameCell,
                ElementIdx = elementIdx,
                Temperature = temperature,
                Mass = mass,
                DiseaseIdx = disease_idx,
                DiseaseCount = disease_count
            });

            PacketSender.SendToAll(packet, EP2PSend.k_EP2PSendUnreliable);
            //DebugConsole.Log("[World] Sent World Update Packet to clients!");
        }
    }
}
