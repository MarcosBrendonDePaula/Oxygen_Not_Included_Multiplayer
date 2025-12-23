using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools.Prioritize
{
	public class PrioritizePacket : FilteredDragToolPacket
	{
		public PrioritizePacket() : base()
		{
			ToolInstance = PrioritizeTool.Instance;
			ToolMode = DragToolMode.OnDragTool;
		}
		public PrioritySetting Priority;

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write((int)Priority.priority_class);
			writer.Write(Priority.priority_value);
		}

		public override void Deserialize(BinaryReader reader)
		{
			base.Deserialize(reader);
			Priority = new PrioritySetting(
					(PriorityScreen.PriorityClass)reader.ReadInt32(),
					reader.ReadInt32()
			);
		}

		public override void OnDispatched()
		{
			ToolMenu.Instance.PriorityScreen.SetScreenPriority(Priority);
			base.OnDispatched();
		}
	}
}
