using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Cancel
{
	public class CancelPacket : FilteredDragToolPacket
	{
		public CancelPacket() : base()
		{
			ToolInstance = CancelTool.Instance;
			ToolMode = DragToolMode.OnDragTool;
		}
	}
}
