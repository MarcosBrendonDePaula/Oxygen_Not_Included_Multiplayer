using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.States;
using ONI_MP.World;
using Steamworks;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveHelper
{
    public static void RequestWorldLoad(WorldSave world)
    {
        SteamNetworkingComponent.scheduler.Run(() => LoadWorldSave(Path.GetFileNameWithoutExtension(world.Name), world.Data));
    }

    private static void LoadWorldSave(string name, byte[] data)
    {
        var savePath = SaveLoader.GetCloudSavesDefault() ? SaveLoader.GetCloudSavePrefix() : SaveLoader.GetSavePrefixAndCreateFolder();

        var baseName = Path.GetFileNameWithoutExtension(name);
        var path = SecurePath.Combine(savePath, baseName, $"{baseName}.sav");

        Directory.CreateDirectory(Path.GetDirectoryName(path));

        // Write save data safely
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(data);
                writer.Flush();
            }
        }

        // We've saved a copy of the downloaded world now load it
        GameClient.CacheCurrentServer();
        GameClient.Disconnect();
        GameClient.SetState(ClientState.LoadingWorld);
        PacketHandler.readyToProcess = false;

        LoadScreen.DoLoad(path);
    }

    public static string WorldName
    {
        get
        {
            var activePath = SaveLoader.GetActiveSaveFilePath();
            return Path.GetFileNameWithoutExtension(activePath);
        }
    }

    public static byte[] GetWorldSave()
    {
        var path = SaveLoader.GetActiveSaveFilePath();
        SaveLoader.Instance.Save(path); // Saves current state to that file
        return File.ReadAllBytes(path);
    }

}
