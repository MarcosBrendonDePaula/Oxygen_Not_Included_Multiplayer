using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.World;
using System.IO;

public static class SaveHelper
{
    public static void RequestWorldLoad(WorldSave world)
    {
        LoadWorldSave(Path.GetFileNameWithoutExtension(world.Name), world.Data);
    }

    private static void LoadWorldSave(string name, byte[] data)
    {
        var savePath = SaveLoader.GetCloudSavesDefault()
            ? SaveLoader.GetCloudSavePrefix()
            : SaveLoader.GetSavePrefixAndCreateFolder();

        var baseName = Path.GetFileNameWithoutExtension(name);
        var path = SecurePath.Combine(savePath, baseName, $"{baseName}.sav");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var writer = new BinaryWriter(File.OpenWrite(path)))
            writer.Write(data);

        DebugConsole.Log($"Loading world: {path} : size: {Utils.FormatBytes(data.Length)}");
        //LoadScreen.DoLoad(path);
        KCrashReporter.MOST_RECENT_SAVEFILE = path;
        SaveLoader.SetActiveSaveFilePath(path);
        LoadingOverlay.Load(delegate
        {
            App.LoadScene("backend");
        });
        DebugConsole.Log($"Loaded world: {name}");
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
