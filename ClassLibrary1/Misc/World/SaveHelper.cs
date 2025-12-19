using ONI_MP;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Misc.World;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using System;
using System.IO;
using System.Threading.Tasks;

public static class SaveHelper
{

	public static int SAVEFILE_CHUNKSIZE_KB
	{
		get
		{
			return Math.Max(64, Configuration.GetHostProperty<int>("SaveFileTransferChunkKB"));
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
	/// Saves the current world snapshot
	/// </summary>
    public static void CaptureWorldSnapshot()
    {
		if(Utils.IsInMenu())
		{
			// We are not in game, ignore
			return;
		}

        var path = SaveLoader.GetActiveSaveFilePath();
        SaveLoader.Instance.Save(path); // Saves current state to that file
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
