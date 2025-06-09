using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using ONI_MP.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONI_MP.Misc
{
    public static class Utils
    {
        public static void LogHierarchy(Transform root, string prefix = "")
        {
            if (root == null)
            {
                DebugConsole.LogWarning("LogHierarchy called with null root.");
                return;
            }

            DebugConsole.Log($"{prefix}{root.name}");

            foreach (Transform child in root)
            {
                LogHierarchy(child, prefix + "  ");
            }
        }

        public static void Inject<T>(GameObject prefab) where T : KMonoBehaviour
        {
            if (prefab.GetComponent<T>() == null)
            {
                DebugConsole.Log($"Added {typeof(T).Name} to {prefab.name}");
                prefab.AddOrGet<T>();
            }
        }

        public static void ListAllTMPFonts()
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            DebugConsole.Log($"Found {fonts.Length} TMP_FontAsset(s):");

            foreach (var font in fonts)
            {
                DebugConsole.Log($" - {font.name} (path: {font.name}, instance ID: {font.GetInstanceID()})");
            }

            if (fonts.Length == 0)
            {
                DebugConsole.Log("No TMP_FontAsset found in memory.");
            }
        }

        public static TMP_FontAsset GetDefaultTMPFont()
        {
            var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(f => f.name == "NotoSans-Regular");

            if (font == null)
            {
                DebugConsole.Log("[ONI_MP] Fallback: NotoSans-Regular not found. Attempting to use any available font.");

                var fallback = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
                if (fallback != null)
                {
                    DebugConsole.Log($"[ONI_MP] Using fallback font: {fallback.name}");
                    return fallback;
                }

                DebugConsole.Log("[ONI_MP] ERROR: No TMP_FontAsset found at all.");
                return null;
            }

            return font;
        }

        public static List<ChunkData> CollectChunks(int startX, int startY, int chunkSize, int numChunksX, int numChunksY)
        {
            var chunks = new List<ChunkData>();
            for (int cx = 0; cx < numChunksX; cx++)
                for (int cy = 0; cy < numChunksY; cy++)
                {
                    int x0 = startX + cx * chunkSize;
                    int y0 = startY + cy * chunkSize;
                    chunks.Add(CreateChunk(x0, y0, chunkSize, chunkSize));
                }
            return chunks;
        }

        private static ChunkData CreateChunk(int x0, int y0, int width, int height)
        {
            var chunk = new ChunkData
            {
                TileX = x0,
                TileY = y0,
                Width = width,
                Height = height,
                Tiles = new ushort[width * height],
                Temperatures = new float[width * height],
                Masses = new float[width * height],
                DiseaseIdx = new byte[width * height],
                DiseaseCount = new int[width * height],
            };

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    int x = x0 + i, y = y0 + j;
                    int idx = i + j * width;
                    int cell = Grid.XYToCell(x, y);

                    if (!Grid.IsValidCell(cell)) continue;

                    chunk.Tiles[idx] = Grid.ElementIdx[cell];
                    chunk.Temperatures[idx] = Grid.Temperature[cell];
                    chunk.Masses[idx] = Grid.Mass[cell];
                    chunk.DiseaseIdx[idx] = Grid.DiseaseIdx[cell];
                    chunk.DiseaseCount[idx] = Grid.DiseaseCount[cell];
                }

            return chunk;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / 1024f / 1024f:F2} MB";
        }

    }
}
