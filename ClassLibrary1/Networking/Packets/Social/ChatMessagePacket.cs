using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Social
{
	public class ChatMessagePacket : IPacket
	{
		public CSteamID SenderId;
		public string Message;
		public Color PlayerColor;
		public long Timestamp;

		public ChatMessagePacket()
		{
		}

		public ChatMessagePacket(string message)
		{
			SenderId = MultiplayerSession.LocalSteamID;
			Message = message;
			PlayerColor = CursorManager.Instance.color;
			Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(SenderId.m_SteamID);
			writer.Write(Message);
			writer.Write(PlayerColor.r);
			writer.Write(PlayerColor.g);
			writer.Write(PlayerColor.b);
			writer.Write(PlayerColor.a);
			writer.Write(Timestamp);
		}

		public void Deserialize(BinaryReader reader)
		{
			SenderId = new CSteamID(reader.ReadUInt64());
			Message = reader.ReadString();
			float r = reader.ReadSingle();
			float g = reader.ReadSingle();
			float b = reader.ReadSingle();
			float a = reader.ReadSingle();
			PlayerColor = new Color(r, g, b, a);
			Timestamp = reader.ReadInt64();
		}

		public void OnDispatched()
		{
			var senderName = SteamFriends.GetFriendPersonaName(SenderId);
			string colorHex = ColorUtility.ToHtmlStringRGB(PlayerColor);
			ChatScreen.QueueMessage($"<color=#{colorHex}>{senderName}:</color> {Message}");

			if (MultiplayerSession.IsHost)
			{
				// Broadcast the chat to all other clients except sender and host
				PacketSender.SendToAllExcluding(this, new HashSet<CSteamID> { SenderId, MultiplayerSession.LocalSteamID });
			}
		}
	}
}
