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

		public List<BaseUtilityBuildTool.PathNode> path = new List<BaseUtilityBuildTool.PathNode>();
		public List<string> MaterialTags = new List<string>();
		public string PrefabID;

		static void SerializePathNode(BinaryWriter writer, BaseUtilityBuildTool.PathNode node)
		{
			writer.Write(node.cell);
			writer.Write(node.valid);
		}
		void DeserializePathNode(BinaryReader reader)
		{
			var node = new BaseUtilityBuildTool.PathNode
			{
				cell = reader.ReadInt32(),
				valid = reader.ReadBoolean()
			};
			path.Add(node);
		}

		public UtilityBuildPacket() { }

		public UtilityBuildPacket(string prefabId, List<BaseUtilityBuildTool.PathNode> nodes, List<string> mats)
		{
			PrefabID = prefabId;
			path = nodes;
			MaterialTags = mats;
		}
		public void Serialize(BinaryWriter writer)
		{
			writer.Write(PrefabID);
			writer.Write(path.Count);
			foreach (var node in path)
			{
				SerializePathNode(writer, node);
			}
			writer.Write(MaterialTags.Count);
			foreach (var tag in MaterialTags)
			{
				writer.Write(tag);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			PrefabID = reader.ReadString();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				DeserializePathNode(reader);
			}
			int matCount = reader.ReadInt32();
			MaterialTags = new List<string>(matCount);
			for (int i = 0; i < matCount; i++)
			{
				MaterialTags.Add(reader.ReadString());
			}
		}

		public void OnDispatched()
		{
			if (path.Count == 0) return;


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
			var cachedDef = tool.def;
			var cachedPath = tool.path;
			var cachedMaterials = tool.selectedElements;

			tool.def = def;
			tool.path = path;
			tool.selectedElements = tags;

			ProcessingIncoming = true;
			tool.BuildPath();
			ProcessingIncoming = false;

			tool.def = cachedDef;
			tool.path = cachedPath;
			tool.selectedElements = cachedMaterials;
		}
	}
}
