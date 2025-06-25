using System.IO;
using System.Reflection;
using ONI_MP.DebugTools;
using UnityEngine;

namespace ONI_MP.Misc
{
    public static class ResourceLoader
    {
        public static Texture2D LoadEmbeddedTexture(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    DebugConsole.LogError($"Embedded resource not found: {resourceName}");
                    return null;
                }

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture.LoadImage(buffer);
                return texture;
            }
        }
    }
}
