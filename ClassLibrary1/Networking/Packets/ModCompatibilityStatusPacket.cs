using System;
using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets
{
    public class ModCompatibilityStatusPacket : IPacket
    {
        public PacketType Type => PacketType.ModCompatibilityStatus;
        private static string logFilePath => Path.Combine(Application.persistentDataPath, "oni_mp_debug.log");
        
        public CSteamID SenderId;
        
        public enum CompatibilityStatus
        {
            Compatible,
            MissingMods,
            VersionMismatch
        }

        public class MissingModInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public string SteamWorkshopUrl { get; set; }

            public MissingModInfo(string id, string name, string version, string steamWorkshopUrl)
            {
                Id = id;
                Name = name;
                Version = version;
                SteamWorkshopUrl = steamWorkshopUrl;
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Id);
                writer.Write(Name ?? "");
                writer.Write(Version);
                writer.Write(SteamWorkshopUrl ?? "");
            }

            public static MissingModInfo Deserialize(BinaryReader reader)
            {
                return new MissingModInfo(
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadString()
                );
            }
        }

        private CompatibilityStatus _status;
        private List<MissingModInfo> _missingMods;

        public ModCompatibilityStatusPacket(CSteamID senderId, CompatibilityStatus status, List<MissingModInfo> missingMods = null)
        {
            SenderId = senderId;
            _status = status;
            _missingMods = missingMods ?? new List<MissingModInfo>();
        }

        public ModCompatibilityStatusPacket()
        {
            SenderId = CSteamID.Nil;
            _status = CompatibilityStatus.Compatible;
            _missingMods = new List<MissingModInfo>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId.m_SteamID);
            writer.Write((int)_status);
            writer.Write(_missingMods.Count);
            foreach (var mod in _missingMods)
            {
                mod.Serialize(writer);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = new CSteamID(reader.ReadUInt64());
            _status = (CompatibilityStatus)reader.ReadInt32();
            int count = reader.ReadInt32();
            _missingMods = new List<MissingModInfo>();
            for (int i = 0; i < count; i++)
            {
                _missingMods.Add(MissingModInfo.Deserialize(reader));
            }
        }

        public void OnDispatched()
        {
            // This packet is received by the server from clients
            if (MultiplayerSession.IsHost)
            {
                // Server side: Handle the compatibility response from client
                DebugConsole.Log($"[ModCompatibilityStatus] Received compatibility status from {SenderId}: {_status}");
                GameServer.OnModCompatibilityReceived(SenderId, _status);
            }
            else
            {
                // Client side: This shouldn't normally happen since clients send this packet
                DebugConsole.Log($"[ModCompatibilityStatus] Received compatibility status on client: {_status}");
            }
        }

        public static void CheckCompatibilityAndRespond(List<ModListSyncPacket.ModInfo> serverMods)
        {
            var clientMods = ONI_MP.Mods.ModLoader.GetActiveInstalledMods();
            var clientModIds = new HashSet<string>();
            var clientModVersions = new Dictionary<string, string>();

            foreach (var mod in clientMods)
            {
                clientModIds.Add(mod.label.id);
                clientModVersions[mod.label.id] = mod.label.version.ToString();
            }

            var missingMods = new List<MissingModInfo>();
            var status = CompatibilityStatus.Compatible;

            foreach (var serverMod in serverMods)
            {
                if (!clientModIds.Contains(serverMod.Id))
                {
                    // Mod is missing
                    status = CompatibilityStatus.MissingMods;
                    var workshopUrl = ONI_MP.Mods.ModLoader.GetSteamWorkshopLink(serverMod.Id);
                    missingMods.Add(new MissingModInfo(serverMod.Id, serverMod.Name, serverMod.Version, workshopUrl));
                }
                else if (clientModVersions[serverMod.Id] != serverMod.Version)
                {
                    // Version mismatch
                    status = CompatibilityStatus.VersionMismatch;
                    var workshopUrl = ONI_MP.Mods.ModLoader.GetSteamWorkshopLink(serverMod.Id);
                    missingMods.Add(new MissingModInfo(serverMod.Id, serverMod.Name, serverMod.Version, workshopUrl));
                }
            }

            // Log detailed information about mod compatibility
            if (status != CompatibilityStatus.Compatible && missingMods.Count > 0)
            {
                DebugConsole.LogWarning($"[ModCompatibility] Found {missingMods.Count} mod compatibility issue(s):");
                foreach (var mod in missingMods)
                {
                    DebugConsole.LogWarning($"  - Missing/Outdated: {mod.Id} (required version: {mod.Version})");
                }
                
                // Show the dialog to the user
                ONI_MP.Menus.ModCompatibilityDialog.ShowMissingMods(missingMods);
            }
            else
            {
                DebugConsole.Log("[ModCompatibility] All server mods are compatible!");
            }

            // Notify GameClient about mod sync completion
            GameClient.OnModSyncCompleted(status == CompatibilityStatus.Compatible);

            var packet = new ModCompatibilityStatusPacket(SteamUser.GetSteamID(), status, missingMods);
            DebugConsole.Log($"[ModCompatibility] Sending compatibility status to host: {status}");
            PacketSender.SendToHost(packet);
        }

        public CompatibilityStatus GetStatus()
        {
            return _status;
        }

        public List<MissingModInfo> GetMissingMods()
        {
            return _missingMods;
        }
    }
}
