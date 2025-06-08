using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.UI;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets
{
    public class PlayerJoinedPacket : IPacket
    {
        public CSteamID SteamId;

        public PacketType Type => PacketType.PlayerJoined;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SteamId.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            SteamId = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost)
                return;

            if (!MultiplayerSession.ConnectedPlayers.TryGetValue(SteamId, out var player))
            {
                player = new MultiplayerPlayer(SteamId);
                MultiplayerSession.ConnectedPlayers.Add(SteamId, player);
                ChatScreen.QueueMessage($"<color=yellow>[System]</color> <b>{player.SteamName}</b> joined the game.");
            }
        }
    }
}
