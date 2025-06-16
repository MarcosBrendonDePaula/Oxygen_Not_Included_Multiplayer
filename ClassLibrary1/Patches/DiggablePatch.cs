using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using UnityEngine;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(Diggable), "OnStopWork")]
    public static class DiggablePatch
    {
        public static void Prefix(Diggable __instance)
        {
            if (!MultiplayerSession.IsHost || !MultiplayerSession.InSession)
                return;

            int cell = __instance.GetCell();

            var packet = new DigCompletePacket
            {
                Cell = cell
            };

            PacketSender.SendToAllClients(packet);
            DebugConsole.Log($"[Dig Complete] Host sent DigCompletePacket for cell {cell}");
        }
    }
}
