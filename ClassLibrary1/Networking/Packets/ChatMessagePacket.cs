using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.UI;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets
{
    public class ChatMessagePacket : IPacket
    {
        public CSteamID SenderId;
        public string Message;

        public PacketType Type => PacketType.ChatMessage;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId.m_SteamID);
            writer.Write(Message);
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = new CSteamID(reader.ReadUInt64());
            Message = reader.ReadString();
        }

        public void OnDispatched()
        {
            // Ignore if this packet came from the local player
            //if (SenderId == MultiplayerSession.LocalSteamID)
             //   return;

            var sender = MultiplayerSession.GetPlayer(SenderId);
            var senderName = sender != null ? sender.SteamName : SenderId.ToString();

            // Add message to chat
            ChatScreen.Instance?.AddMessage($"<color=cyan>{senderName}:</color> {Message}");
        }
    }
}
