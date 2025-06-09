using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets;
using UnityEngine;

namespace ONI_MP.Patches.Navigation
{
    [HarmonyPatch(typeof(Navigator), nameof(Navigator.AdvancePath))]
    public static class NavigatorPatch
    {
        static void Prefix(Navigator __instance)
        {
            if (!__instance.path.IsValid() || __instance.path.nodes == null || __instance.path.nodes.Count == 0)
                return;

            if (MultiplayerSession.IsClient)
                return;

            if (!MultiplayerSession.InSession)
                return;

            if (!__instance.TryGetComponent<NetworkIdentity>(out var identity))
                return;

            var packet = new NavigatorPathPacket
            {
                NetId = identity.NetId
            };

            foreach (var node in __instance.path.nodes)
            {
                packet.Steps.Add(new NavigatorPathPacket.PathStep
                {
                    Cell = node.cell,
                    NavType = node.navType,
                    TransitionId = node.transitionId
                });
            }

            PacketSender.SendToAll(packet);

            string log = $"[Navigator AdvancePath] Sent path for {__instance.name} with {__instance.path.nodes.Count} steps.";
            for (int i = 0; i < __instance.path.nodes.Count; i++)
            {
                var node = __instance.path.nodes[i];
                Vector3 worldPos = Grid.CellToPosCBC(node.cell, Grid.SceneLayer.Move);
                log += $"\n  Step {i}: Cell={node.cell}, WorldPos={worldPos}, NavType={node.navType}, TransitionId={node.transitionId}";
            }

            DebugConsole.Log(log);
        }
    }

    [HarmonyPatch(typeof(Navigator), nameof(Navigator.GoTo), new[] {
        typeof(KMonoBehaviour), typeof(CellOffset[]), typeof(NavTactic)
    })]
    public static class Navigator_GoTo_Target_Patch
    {
        static bool Prefix()
        {
            if(!MultiplayerSession.InSession)
            {
                return true;
            }

            // Only allow host to initiate target-based pathing
            return MultiplayerSession.IsHost;
        }
    }
}
