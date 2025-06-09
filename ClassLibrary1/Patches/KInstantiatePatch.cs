using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using UnityEngine;

[HarmonyPatch(typeof(Util), nameof(Util.KInstantiate),
    new[] {
        typeof(GameObject),
        typeof(Vector3),
        typeof(Quaternion),
        typeof(GameObject),
        typeof(string),
        typeof(bool),
        typeof(int)
    })]
public static class KInstantiate_FullParams_Patch
{
    // Prefix: runs before the original method
    public static bool Prefix(GameObject original, Vector3 position, Quaternion rotation, GameObject parent, string name, bool initialize_id, int gameLayer)
    {
        // Block instantiation if we're a client
        if (MultiplayerSession.IsClient)
        {
            //DebugConsole.Log($"[MP] Blocked KInstantiate on client for prefab '{original?.name}'");
            return false; // Skip original method
        }

        return true; // Allow original method to run
    }

    // Postfix: runs after the original method
    public static void Postfix(GameObject __result, GameObject original, Vector3 position, Quaternion rotation, GameObject parent, string name, bool initialize_id, int gameLayer)
    {
        if (__result == null || original == null)
            return;

        //DebugConsole.Log($"[MP] KInstantiate Postfix - Created '{__result.name}' from '{original.name}'");

        // Only the host should broadcast instantiations
        if (MultiplayerSession.IsHost)
        {
            var packet = new InstantiatePacket
            {
                PrefabName = original.name,
                Position = position,
                Rotation = rotation,
                ObjectName = name,
                InitializeId = initialize_id,
                GameLayer = gameLayer
            };

            PacketSender.SendToAll(packet);
           // DebugConsole.Log($"Sent Instantiation packet for object: {original.name}");
        }
    }
}
