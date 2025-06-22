using HarmonyLib;
using UnityEngine;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.DebugTools;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(SpeedControlScreen))]
    public static class SpeedControlScreen_SendSpeedPacketPatch
    {
        [HarmonyPatch("SetSpeed")]
        [HarmonyPostfix]
        public static bool SetSpeed_Postfix(int Speed)
        {
            return true;

            var packet = new SpeedChangePacket((SpeedChangePacket.SpeedState)Speed);

            if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(packet);
            } else
            {
                PacketSender.SendToHost(packet);
            }
            DebugConsole.Log($"[SpeedControl] Sent SpeedChangePacket: {packet.Speed}");
        }

        [HarmonyPatch("TogglePause")]
        [HarmonyPostfix]
        public static bool TogglePause_Postfix(SpeedControlScreen __instance)
        {
            return true;

            var speedState = __instance.IsPaused
                ? SpeedChangePacket.SpeedState.Paused
                : (SpeedChangePacket.SpeedState)__instance.GetSpeed();

            var packet = new SpeedChangePacket(speedState);
            if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(packet);
            }
            else
            {
                PacketSender.SendToHost(packet);
            }
            DebugConsole.Log($"[SpeedControl] Sent SpeedChangePacket (pause toggle): {packet.Speed}");
        }
    }
}
