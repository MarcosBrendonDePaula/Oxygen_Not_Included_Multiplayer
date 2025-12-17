using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

namespace ONI_MP.Networking.Packets.Social
{
	public class ImmigrantOptionsPacket : IPacket
	{
		public PacketType Type => PacketType.ImmigrantOptions;

		public struct OptionEntry
		{
			public bool IsDuplicant;

			// Duplicant Data
			public string Name;
			public string Gender;
			public string PersonalityId;
			// We can add Traits/Interests generic strings later if needed for the UI card description.
			// For now, PersonalityId + Name might be enough to generate a "similar" looking dupe, 
			// but stats will be random locally unless we sync them. 
			// Let's add basic stats we can read easily.
			public int StressTraitId; // hash?
			public int JoyTraitId; // hash?

			// Care Package Data
			public string CarePackageId;
			public float Quantity;
		}

		public List<OptionEntry> Options = new List<OptionEntry>();

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Options.Count);
			foreach (var opt in Options)
			{
				writer.Write(opt.IsDuplicant);
				if (opt.IsDuplicant)
				{
					writer.Write(opt.Name ?? "Unknown");
					writer.Write(opt.Gender ?? "NB");
					writer.Write(opt.PersonalityId ?? "Hassan");
				}
				else
				{
					writer.Write(opt.CarePackageId ?? "None");
					writer.Write(opt.Quantity);
				}
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			Options = new List<OptionEntry>();
			for (int i = 0; i < count; i++)
			{
				var opt = new OptionEntry();
				opt.IsDuplicant = reader.ReadBoolean();
				if (opt.IsDuplicant)
				{
					opt.Name = reader.ReadString();
					opt.Gender = reader.ReadString();
					opt.PersonalityId = reader.ReadString();
				}
				else
				{
					opt.CarePackageId = reader.ReadString();
					opt.Quantity = reader.ReadSingle();
				}
				Options.Add(opt);
			}
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost) return;

			// Client received options.
			// Store them in the Patch class so when ImmigrantScreen opens, we use them.
			ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.AvailableOptions = Options;

			// If the screen is already open, refresh it immediately.
			if (ImmigrantScreen.instance != null && ImmigrantScreen.instance.gameObject.activeInHierarchy)
			{
				ONI_MP.Patches.GamePatches.ImmigrantScreenPatch.ApplyOptionsToScreen(ImmigrantScreen.instance);
			}
		}
	}
}
