using ONI_MP.DebugTools;
using ONI_MP.Networking;
using System.IO;
using UnityEngine;

public class DigCompletePacket : IPacket
{
    public PacketType Type => PacketType.DigComplete;

    public int Cell;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Cell);
    }

    public void Deserialize(BinaryReader reader)
    {
        Cell = reader.ReadInt32();
    }

    public void OnDispatched()
    {
        if (!Grid.IsValidCell(Cell))
            return;

        // Destroy dig placers or tile visuals
        for (int i = 0; i < (int)Grid.SceneLayer.SceneMAX; i++)
        {
            GameObject obj = Grid.Objects[Cell, i];
            if (obj != null)
                Util.KDestroyGameObject(obj);
        }

        // Read current sim data from the cell
        float mass = Grid.Mass[Cell];
        float temperature = Grid.Temperature[Cell];
        ushort element_idx = Grid.ElementIdx[Cell];
        byte disease_idx = Grid.DiseaseIdx[Cell];
        int disease_count = Grid.DiseaseCount[Cell];

        // Spawn ore + FX from the dig
        WorldDamage.Instance.OnDigComplete(Cell, mass, temperature, element_idx, disease_idx, disease_count);
        // Destroy cell via sim
        WorldDamage.Instance.DestroyCell(Cell);
        // Trigger on solid state changed
        WorldDamage.Instance.OnSolidStateChanged(Cell);

    }


}
