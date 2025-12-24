using Newtonsoft.Json;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static TUNING.BUILDINGS.UPGRADES;

namespace ONI_MP.Networking.Packets.Tools.Build
{
	public class UtilityBuildPacket : IPacket
	{
		/// <summary>
		/// Gets a value indicating whether incoming messages are currently being processed.
		/// Use in patches to prevent recursion when applying tool changes.
		/// </summary>
		public static bool ProcessingIncoming { get; private set; } = false;

		public List<BaseUtilityBuildTool.PathNode> path = [];
		public List<string> MaterialTags = [];
		public string PrefabID, FacadeID;
		public PrioritySetting Priority;

		static void SerializePathNode(ref BinaryWriter writer, ref BaseUtilityBuildTool.PathNode node)
		{
			writer.Write(node.cell);
			writer.Write(node.valid);
		}
		void DeserializePathNode(ref BinaryReader reader, ref List<BaseUtilityBuildTool.PathNode> toAdd)
		{
			var node = new BaseUtilityBuildTool.PathNode
			{
				cell = reader.ReadInt32(),
				valid = reader.ReadBoolean()
			};
			toAdd.Add(node);
		}

		public UtilityBuildPacket() { }

		public UtilityBuildPacket(string prefabId, List<BaseUtilityBuildTool.PathNode> nodes, List<string> mats, string skin)
		{
			PrefabID = prefabId ?? string.Empty;
			path = nodes ?? [];
			MaterialTags = mats ?? [];
			FacadeID = skin ?? string.Empty;
		}
		public void Serialize(BinaryWriter writer)
		{
			writer.Write(PrefabID);
			writer.Write(FacadeID);
			writer.Write(path.Count);
			if (path.Any())
			{
				for(int i = 0; i < path.Count; i++)
				{
					var node = path[i];
					SerializePathNode(ref writer, ref node);
				}
			}
			writer.Write(MaterialTags.Count);
			if (MaterialTags.Any())
			{
				foreach (var tag in MaterialTags)
				{
					writer.Write(tag);
				}
			}
			writer.Write((int)Priority.priority_class);
			writer.Write(Priority.priority_value);
		}


		public void Deserialize(BinaryReader reader)
		{
			DebugConsole.Log("[UtilityBuildPacket] Deserializing UtilityBuildPacket");
			PrefabID = reader.ReadString();
			FacadeID = reader.ReadString();
			int count = reader.ReadInt32();
			path = new List<BaseUtilityBuildTool.PathNode>(count);
			for (int i = 0; i < count; i++)
			{
				DeserializePathNode(ref reader, ref path);
			}
			int matCount = reader.ReadInt32();
			MaterialTags = new List<string>(matCount);
			if (matCount > 0)
			{
				for (int i = 0; i < matCount; i++)
				{
					MaterialTags.Add(reader.ReadString());
				}
			}
			Priority = new PrioritySetting(
					(PriorityScreen.PriorityClass)reader.ReadInt32(),
					reader.ReadInt32());
		}

		public void OnDispatched()
		{
			DebugConsole.Log("[UtilityBuildPacket] OnDispatched");
			if (path.Count == 0)
			{
				DebugConsole.LogWarning("[UtilityBuildPacket] Received empty path, ignoring.");
				return;
			}


			var def = Assets.GetBuildingDef(PrefabID);
			if (def == null)
			{
				DebugConsole.LogError($"[UtilityBuildPacket] Unknown PrefabID: {PrefabID}");
				return;
			}

			var tags = MaterialTags.Select(t => new Tag(t)).ToList();
			if (tags.Count == 0)
			{
				tags.AddRange(def.DefaultElements());
			}
			///mirrored from BuildMenu OnRecipeElementsFullySelected
			BaseUtilityBuildTool tool = def.BuildingComplete.TryGetComponent<Wire>(out _) ? WireBuildTool.Instance : UtilityBuildTool.Instance;

			///caching existing stuff on the tool
			BuildingDef cachedDef = tool.def;
			List<BaseUtilityBuildTool.PathNode> cachedPath = tool.path != null ? [.. tool.path] : [];
			IList<Tag> cachedMaterials = tool.selectedElements != null ? [.. tool.selectedElements] : [];
			IUtilityNetworkMgr cachedMgr = tool.conduitMgr;
			PrioritySetting cachedPriority = ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority();

			IHaveUtilityNetworkMgr conduitManagerHaver = def.BuildingComplete.GetComponent<IHaveUtilityNetworkMgr>();

			tool.def = def;
			tool.path = path;
			tool.selectedElements = tags;
			tool.conduitMgr = conduitManagerHaver.GetNetworkManager();

			ProcessingIncoming = true;
			ToolMenu.Instance.PriorityScreen.SetScreenPriority(Priority);
			DebugConsole.Log($"[UtilityBuildPacket] Building path with {path.Count} nodes of prefab {def.PrefabID}");
			tool.BuildPath();
			ProcessingIncoming = false;

			tool.def = cachedDef;
			tool.path = cachedPath;
			tool.selectedElements = cachedMaterials;
			tool.conduitMgr = cachedMgr;
			ToolMenu.Instance.PriorityScreen.SetScreenPriority(cachedPriority);
		}
	}
}
