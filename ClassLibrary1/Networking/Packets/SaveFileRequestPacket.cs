using System;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.World;
using Steamworks;

namespace ONI_MP.Networking.Packets
{
    public class SaveFileRequestPacket : IPacket
    {
        public CSteamID Requester;

        public PacketType Type => PacketType.SaveFileRequest;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Requester.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            Requester = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (!MultiplayerSession.IsHost)
                return;

            DebugConsole.Log($"[Packets/SaveFileRequest] Received request from {Requester}");
            SendSaveFile(Requester);
        }

        public static void SendSaveFile(CSteamID requester)
        {
            if (!MultiplayerSession.IsHost)
                return;

            try
            {
                SaveLoader.Instance.InitialSave(); // Trigger autosave
                string name = SaveHelper.WorldName;
                byte[] data = SaveHelper.GetWorldSave();

                var packet = new SaveFilePacket
                {
                    FileName = name + ".sav",
                    Data = data
                };

                PacketSender.SendToPlayer(requester, packet);
                DebugConsole.Log($"[Packets/SaveFileRequest] Sent save file '{packet.FileName}' to {requester}");
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[Packets/SaveFileRequest] Failed to send save file: {ex}");
            }
        }
    }
}
