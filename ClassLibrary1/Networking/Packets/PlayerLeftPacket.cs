using System.IO;
using ONI_MP.UI;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets
{
    public class PlayerLeftPacket : IPacket
    {
        public CSteamID SteamId;

        public PacketType Type => PacketType.PlayerLeft;

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

            if (MultiplayerSession.ConnectedPlayers.TryGetValue(SteamId, out var player))
            {
                MultiplayerSession.ConnectedPlayers.Remove(SteamId);
                ChatScreen.QueueMessage($"<color=yellow>[System]</color> <b>{player.SteamName}</b> left the game.");
            }
        }

    }
}
