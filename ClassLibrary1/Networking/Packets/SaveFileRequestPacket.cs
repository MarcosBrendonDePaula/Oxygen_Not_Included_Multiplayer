using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.World;
using Steamworks;
using UnityEngine;

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
                string fileName = name + ".sav";

                const int ChunkSize = 256 * 1024; // Split into 256kb chunks
                var chunkPackets = new List<SaveFileChunkPacket>();

                for (int offset = 0; offset < data.Length; offset += ChunkSize)
                {
                    int size = Math.Min(ChunkSize, data.Length - offset);
                    byte[] chunk = new byte[size];
                    Buffer.BlockCopy(data, offset, chunk, 0, size);

                    var chunkPacket = new SaveFileChunkPacket
                    {
                        FileName = fileName,
                        Offset = offset,
                        TotalSize = data.Length,
                        Chunk = chunk
                    };

                    chunkPackets.Add(chunkPacket);
                }

                CoroutineRunner.RunOne(SendChunksThrottled(chunkPackets, requester));
                DebugConsole.Log($"[SaveFileRequest] Sent '{fileName}' in {Math.Ceiling(data.Length / (float)ChunkSize)} chunks to {requester}");

            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[SaveFileRequest] Failed to send save file: {ex}");
            }
        }

        private static IEnumerator SendChunksThrottled(List<SaveFileChunkPacket> chunkPackets, CSteamID steamID)
        {
            foreach (var chunkPacket in chunkPackets)
            {
                PacketSender.SendToPlayer(steamID, chunkPacket);
                yield return new WaitForSeconds(1f); // Wait one frame
            }
            DebugConsole.Log($"[SaveFileRequest] All chunks sent to {steamID}.");
        }

    }
}
