using ONI_MP.Misc;
using ONI_MP.World;
using System.IO;

public static class SaveHelper
{
    public static void RequestWorldLoad(WorldSave world)
    {
        LoadWorldSave(world.Name, world.Data);
    }

    private static void LoadWorldSave(string name, byte[] data)
    {
        var savePath = SaveLoader.GetCloudSavesDefault()
            ? SaveLoader.GetCloudSavePrefix()
            : SaveLoader.GetSavePrefixAndCreateFolder();

        var path = SecurePath.Combine(savePath, name, $"{name}.sav");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var writer = new BinaryWriter(File.OpenWrite(path)))
            writer.Write(data);

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
