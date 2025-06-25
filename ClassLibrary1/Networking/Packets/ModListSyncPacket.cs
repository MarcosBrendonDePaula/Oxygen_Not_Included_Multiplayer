using System;
using System.Collections.Generic;
using System.IO;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;

namespace ONI_MP.Networking.Packets
{
    public class ModListSyncPacket : IPacket
    {
        public PacketType Type => PacketType.ModListSync;

        public class ModInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public bool IsRequired { get; set; }

            public ModInfo(string id, string name, string version, bool isRequired)
            {
                Id = id;
                Name = name;
                Version = version;
                IsRequired = isRequired;
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Id);
                writer.Write(Name ?? "");
                writer.Write(Version);
                writer.Write(IsRequired);
            }

            public static ModInfo Deserialize(BinaryReader reader)
            {
                return new ModInfo(
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadBoolean()
                );
            }
        }

        private List<ModInfo> _mods;

        public ModListSyncPacket(List<ModInfo> mods)
        {
            _mods = mods;
        }

        public ModListSyncPacket()
        {
            _mods = new List<ModInfo>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_mods.Count);
            foreach (var mod in _mods)
            {
                mod.Serialize(writer);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            _mods = new List<ModInfo>();
            for (int i = 0; i < count; i++)
            {
                _mods.Add(ModInfo.Deserialize(reader));
            }
        }

        public void OnDispatched()
        {
            // This packet is received by clients from the server
            // Client should check compatibility and respond
            ModCompatibilityStatusPacket.CheckCompatibilityAndRespond(_mods);
        }

        public static void SendModList(CSteamID targetClient)
        {
            var mods = ONI_MP.Mods.ModLoader.GetActiveInstalledMods();
            var modInfos = new List<ModInfo>();

            foreach (var mod in mods)
            {
                modInfos.Add(new ModInfo(
                    mod.label.id,
                    mod.label.title ?? mod.label.id, // Use title as name, fallback to ID
                    mod.label.version.ToString(),
                    true // For now all mods are required, we can make this configurable later
                ));
            }

            var packet = new ModListSyncPacket(modInfos);
            PacketSender.SendToPlayer(targetClient, packet);
        }

        public List<ModInfo> GetMods()
        {
            return _mods;
        }
    }
}
