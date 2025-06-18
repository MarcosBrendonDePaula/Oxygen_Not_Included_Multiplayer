using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.World;
using UnityEngine;

namespace ONI_MP.Patches.GamePatches
{
    [HarmonyPatch(typeof(GameClock))]
    public static class GameClockPatch
    {
        public static bool allowAddTimeForSetTime = false;
        private static float _lastSentTime = 0f;

        // Prevent clients from running AddTime
        [HarmonyPatch("AddTime")]
        [HarmonyPrefix]
        public static bool AddTime_Prefix()
        {
            if (!MultiplayerSession.InSession)
                return true;

            if (MultiplayerSession.IsClient && !allowAddTimeForSetTime)
                return false;

            return true;
        }

        // Host logic: send WorldCyclePacket every 1 second
        [HarmonyPatch("AddTime")]
        [HarmonyPostfix]
        public static void AddTime_Postfix(GameClock __instance)
        {
            if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost)
                return;

            float currentTime = __instance.GetTime();

            if (currentTime - _lastSentTime >= 1f)
            {
                _lastSentTime = currentTime;

                PacketSender.SendToAllClients(new WorldCyclePacket
                {
                    Cycle = __instance.GetCycle(),
                    CycleTime = __instance.GetTimeSinceStartOfCycle()
                }, SteamNetworkingSend.Unreliable);

                DebugConsole.Log($"[Multiplayer] WorldCyclePacket sent @ {currentTime:0.00}s");
            }
        }
    }
}
