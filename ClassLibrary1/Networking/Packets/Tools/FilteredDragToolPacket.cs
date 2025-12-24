using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Tools
{
	public abstract class FilteredDragToolPacket : IPacket
	{
		/// <summary>
		/// Gets a value indicating whether incoming messages are currently being processed.
		/// Use in patches to prevent recursion when applying tool changes.
		/// </summary>
		public static bool ProcessingIncoming { get; private set; } = false;

		public enum DragToolMode
		{
			Invalid = -1,
			OnDragTool = 0,
			OnDragComplete = 1
		}

		///set these two in the derived tool packet
		protected DragToolMode ToolMode = DragToolMode.Invalid;
		protected FilteredDragTool ToolInstance;

		HashSet<string> currentFilterTargets = [];
		public Vector3 downPos, upPos;
		public int cell, distFromOrigin;

		public virtual void Deserialize(BinaryReader reader)
		{
			var count = reader.ReadInt32();
			currentFilterTargets = new HashSet<string>(count);
			for (int i = 0; i < count; i++)
			{
				currentFilterTargets.Add(reader.ReadString());
			}
			switch (ToolMode)
			{
				case DragToolMode.OnDragTool:
					cell = reader.ReadInt32();
					distFromOrigin = reader.ReadInt32();
					break;
				case DragToolMode.OnDragComplete:
					downPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
					upPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
					break;
			}
		}

		public virtual void Serialize(BinaryWriter writer)
		{
			StoreFilterData(ToolInstance);

			writer.Write(currentFilterTargets.Count);
			foreach (var target in currentFilterTargets)
			{
				writer.Write(target);
			}
			switch (ToolMode)
			{
				case DragToolMode.OnDragTool:
					writer.Write(cell);
					writer.Write(distFromOrigin);
					break;
				case DragToolMode.OnDragComplete:
					writer.Write(downPos.x); writer.Write(downPos.y); writer.Write(downPos.z);
					writer.Write(upPos.x); writer.Write(upPos.y); writer.Write(upPos.z);
					break;
			}
		}
		public virtual void OnDispatched()
		{
			if (ToolInstance == null)
			{
				DebugConsole.LogWarning("[FilteredDragToolPacket] ToolInstance is null in OnDispatched");
			}
			ApplyFilterData(ToolInstance);
			ProcessingIncoming = true;
			switch (ToolMode)
			{
				case DragToolMode.OnDragTool:
					DebugConsole.Log($"[FilteredDragToolPacket] OnDispatched OnDragTool - cell: {cell}, distFromOrigin: {distFromOrigin}");
					ToolInstance.OnDragTool(cell, distFromOrigin);
					break;
				case DragToolMode.OnDragComplete:
					ToolInstance.downPos = downPos;
					DebugConsole.Log($"[FilteredDragToolPacket] OnDispatched OnDragComplete - startPos: {downPos}, endPos: {upPos}");
					ToolInstance.OnDragComplete(downPos, upPos);
					break;
				default:
					DebugConsole.LogWarning("[FilteredDragToolPacket] OnDispatched called with invalid ToolMode");
					break;
			}
			ProcessingIncoming = false;
		}
		public void ApplyFilterData(FilteredDragTool tool)
		{
			var currentFilterKeys = tool.currentFilterTargets.Keys.ToList();

			foreach (var target in currentFilterKeys)
			{
				tool.currentFilterTargets[target] = ToolParameterMenu.ToggleState.Off;
			}
			foreach(var target in currentFilterTargets)
			{
				tool.currentFilterTargets[target] = ToolParameterMenu.ToggleState.On;
			}
		}

		public void StoreFilterData(FilteredDragTool tool)
		{
			foreach (var target in tool.currentFilterTargets)
			{
				if (target.Value == ToolParameterMenu.ToggleState.On)
					currentFilterTargets.Add(target.Key);
			}
		}
	}
}
