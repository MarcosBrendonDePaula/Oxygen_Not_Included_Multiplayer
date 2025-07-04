using ONI_MP;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Misc.World;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using Steamworks;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveHelper
{

    public static int SAVEFILE_CHUNKSIZE_KB {
        get {
            return Configuration.GetHostProperty<int>("SaveFileTransferChunkKB");
        }
    }
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
        MultiplayerOverlay.Show("Loading...");

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

    /// <summary>
    /// Downloads a save file from a Google Drive share link to a known location. GOOGLE DRIVE DOES NOT NEED TO BE INITIALIZED HERE
    /// </summary>
    public static void DownloadSave(string shareLink, string fileName, System.Action OnCompleted, System.Action OnFailed)
    {
        MultiplayerOverlay.Show("Downloading world from host...");

        try
        {
            // consistent location, e.g. the normal saves folder
            var savePath = SaveLoader.GetCloudSavesDefault()
                ? SaveLoader.GetCloudSavePrefix()
                : SaveLoader.GetSavePrefixAndCreateFolder();

            var targetFile = SecurePath.Combine(
                savePath,
                Path.GetFileNameWithoutExtension(fileName),
                $"{Path.GetFileNameWithoutExtension(fileName)}.sav"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

            using (var web = new System.Net.WebClient())
            {
                web.DownloadProgressChanged += (s, e) =>
                {
                    MultiplayerOverlay.Show($"Downloading world from host: {e.ProgressPercentage}%");
                };

                web.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Error != null)
                    {
                        MultiplayerOverlay.Show($"Download failed: {e.Error.Message}");
                        DebugConsole.LogError($"[SaveHelper] Download failed: {e.Error.Message}");
                        OnFailed?.Invoke();
                        return;
                    }

                    MultiplayerOverlay.Show("Download complete!");
                    DebugConsole.Log($"[SaveHelper] Downloaded to: {targetFile}");
                    OnCompleted?.Invoke();
                };

                web.DownloadFileAsync(new Uri(shareLink), targetFile);

                // Wait for completion in a blocking way to keep your current sync pattern:
                while (web.IsBusy)
                {
                    System.Threading.Thread.Sleep(100); // or yield in a coroutine if you prefer
                }
            }
        }
        catch (Exception ex)
        {
            MultiplayerOverlay.Show($"Download failed: {ex.Message}");
            DebugConsole.LogError($"[SaveHelper] Download failed: {ex.Message}");
        }
    }

    public static void LoadDownloadedSave(string fileName)
    {
        var savePath = SaveLoader.GetCloudSavesDefault()
            ? SaveLoader.GetCloudSavePrefix()
            : SaveLoader.GetSavePrefixAndCreateFolder();

        var targetFile = SecurePath.Combine(
            savePath,
            Path.GetFileNameWithoutExtension(fileName),
            $"{Path.GetFileNameWithoutExtension(fileName)}.sav"
        );

        if (!File.Exists(targetFile))
        {
            MultiplayerOverlay.Show("Downloaded save file not found.");
            DebugConsole.LogError($"[SaveHelper] Could not find file to load: {targetFile}");
            return;
        }

        // We've saved a copy of the downloaded world, now load it
        GameClient.CacheCurrentServer();
        GameClient.Disconnect();
        GameClient.SetState(ClientState.LoadingWorld);
        PacketHandler.readyToProcess = false;
        MultiplayerOverlay.Show("Loading...");

        LoadScreen.DoLoad(targetFile); // use the correct variable
    }

}
