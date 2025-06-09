using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using ONI_MP.DebugTools;
using ONI_MP.World;
using UnityEngine;

namespace ONI_MP.Networking.Packets
{
    public class SaveFilePacket : IPacket
    {
        public string FileName;
        public byte[] Data;

        public PacketType Type => PacketType.SaveFile;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(FileName);

            using (var ms = new MemoryStream())
            {
                using (var deflate = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
                {
                    deflate.Write(Data, 0, Data.Length);
                }

                byte[] compressed = ms.ToArray();
                writer.Write(compressed.Length);
                writer.Write(compressed);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            FileName = reader.ReadString();

            int compressedLength = reader.ReadInt32();
            byte[] compressed = reader.ReadBytes(compressedLength);

            using (var compressedStream = new MemoryStream(compressed))
            using (var deflate = new DeflateStream(compressedStream, CompressionMode.Decompress))
            using (var decompressedStream = new MemoryStream())
            {
                deflate.CopyTo(decompressedStream);
                Data = decompressedStream.ToArray();
            }
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost)
                return;

            try
            {
                var world = new WorldSave(FileName, Data);
                CoroutineRunner.RunOne(DelayedLoad(world));
            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[Packets/SaveFile] Failed to prepare world load: {ex}");
            }
        }

        private static IEnumerator DelayedLoad(WorldSave world)
        {
            yield return new WaitForSecondsRealtime(0.5f); // Give filesystem time to flush
            DebugConsole.Log($"[Packets/SaveFile] Loading save: {world.Name}");
            SaveHelper.RequestWorldLoad(world);
        }
    }
}
