using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ONI_MP.Patches.ToolPatches
{
	internal class DisconnectToolPatch
	{

        [HarmonyPatch(typeof(DisconnectTool), nameof(DisconnectTool.OnDragComplete))]
        public class DisconnectTool_OnDragComplete_Patch
        {
            public static void Postfix(DisconnectTool __instance, Vector3 downPos, Vector3 upPos)
            {
                if (!MultiplayerSession.InSession)
                    return;

				//prevent recursion
				if (DisconnectPacket.ProcessingIncoming)
                    return;

				if (__instance.singleDisconnectMode)
				{
					upPos = __instance.SnapToLine(upPos);
				}
				PacketSender.SendToAllOtherPeers(new DisconnectPacket() { downPos = downPos, upPos = upPos});
			}
        }
	}
}
