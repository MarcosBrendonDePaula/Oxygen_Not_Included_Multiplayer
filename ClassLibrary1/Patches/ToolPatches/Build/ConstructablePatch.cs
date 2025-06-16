using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using System.Linq;

[HarmonyPatch(typeof(Constructable), "FinishConstruction")]
public static class ConstructablePatch
{
    public static void Prefix(Constructable __instance)
    {
        if (!MultiplayerSession.IsHost || !MultiplayerSession.InSession)
            return;

        var building = __instance.GetComponent<Building>();
        if (building == null || building.Def == null)
            return;

        int cell = Grid.PosToCell(__instance.transform.position);
        var def = building.Def;

        // Gather material tags
        var materialTags = __instance.SelectedElementsTags?.Select(tag => tag.ToString()).ToList()
                           ?? new System.Collections.Generic.List<string>();

        // Determine temperature (use delivered or fallback)
        float temp = __instance.GetComponent<PrimaryElement>()?.Temperature ?? def.Temperature;

        // Get orientation (from rotatable or default)
        var rotatable = __instance.GetComponent<Rotatable>();
        var orientation = rotatable != null ? rotatable.GetOrientation() : Orientation.Neutral;

        // Get facade (if available)
        var facade = __instance.GetComponent<BuildingFacade>()?.CurrentFacade ?? "DEFAULT_FACADE";

        var packet = new BuildCompletePacket
        {
            Cell = cell,
            PrefabID = def.PrefabID,
            Orientation = orientation,
            MaterialTags = materialTags,
            Temperature = temp,
            FacadeID = facade
        };

        PacketSender.SendToAllClients(packet);
        DebugConsole.Log($"[Host] Sent BuildCompletePacket for {def.PrefabID} at cell {cell}");
    }
}
