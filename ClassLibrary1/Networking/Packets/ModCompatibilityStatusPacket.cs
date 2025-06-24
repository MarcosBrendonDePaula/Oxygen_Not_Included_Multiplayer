using System;
using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;

namespace ONI_MP.Networking.Packets
{
    public class ModCompatibilityStatusPacket : IPacket
    {
        public PacketType Type => PacketType.ModCompatibilityStatus;

        public enum CompatibilityStatus
        {
            Compatible,
            MissingMods,
            VersionMismatch
        }

        public class MissingModInfo
        {
            public string Id { get; set; }
            public string Version { get; set; }
            public string SteamWorkshopUrl { get; set; }

            public MissingModInfo(string id, string version, string steamWorkshopUrl)
            {
                Id = id;
                Version = version;
                SteamWorkshopUrl = steamWorkshopUrl;
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Id);
                writer.Write(Version);
                writer.Write(SteamWorkshopUrl ?? "");
            }

            public static MissingModInfo Deserialize(BinaryReader reader)
            {
                return new MissingModInfo(
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadString()
                );
            }
        }

        private CompatibilityStatus _status;
        private List<MissingModInfo> _missingMods;

        public ModCompatibilityStatusPacket(CompatibilityStatus status, List<MissingModInfo> missingMods = null)
        {
            _status = status;
            _missingMods = missingMods ?? new List<MissingModInfo>();
        }

        public ModCompatibilityStatusPacket()
        {
            _status = CompatibilityStatus.Compatible;
            _missingMods = new List<MissingModInfo>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((int)_status);
            writer.Write(_missingMods.Count);
            foreach (var mod in _missingMods)
            {
                mod.Serialize(writer);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
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
                // We need to get the sender's SteamID from the packet context
                // For now, we'll iterate through connected players to find who sent this
                foreach (var player in MultiplayerSession.ConnectedPlayers.Values)
                {
                    if (player.IsConnected && !player.ModSyncCompleted)
                    {
                        // Assume this is the player who sent the response
                        GameServer.OnModCompatibilityReceived(player.SteamID, _status);
                        break;
                    }
                }
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
                    missingMods.Add(new MissingModInfo(serverMod.Id, serverMod.Version, workshopUrl));
                }
                else if (clientModVersions[serverMod.Id] != serverMod.Version)
                {
                    // Version mismatch
                    status = CompatibilityStatus.VersionMismatch;
                    var workshopUrl = ONI_MP.Mods.ModLoader.GetSteamWorkshopLink(serverMod.Id);
                    missingMods.Add(new MissingModInfo(serverMod.Id, serverMod.Version, workshopUrl));
                }
            }

            // If there are missing mods, show the dialog to the user
            if (status != CompatibilityStatus.Compatible && missingMods.Count > 0)
            {
                ONI_MP.Menus.ModCompatibilityDialog.ShowMissingMods(missingMods);
            }

            // Notify GameClient about mod sync completion
            GameClient.OnModSyncCompleted(status == CompatibilityStatus.Compatible);

            var packet = new ModCompatibilityStatusPacket(status, missingMods);
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
