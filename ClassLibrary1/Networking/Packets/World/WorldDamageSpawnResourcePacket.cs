using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Networking.Packets.World
{
    public class WorldDamageSpawnResourcePacket : IPacket
    {
        public PacketType Type => PacketType.WorldDamageSpawnResource;

        public int NetId;
        public Vector3 Position;
        public float Mass;
        public float Temperature;
        public ushort ElementIndex;
        public byte DiseaseIndex;
        public int DiseaseCount;

        public WorldDamageSpawnResourcePacket() { }

        public WorldDamageSpawnResourcePacket(int netId, Vector3 pos, float mass, float temp, ushort elementIdx, byte diseaseIdx, int diseaseCount)
        {
            NetId = netId;
            Position = pos;
            Mass = mass;
            Temperature = temp;
            ElementIndex = elementIdx;
            DiseaseIndex = diseaseIdx;
            DiseaseCount = diseaseCount;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetId);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            writer.Write(Mass);
            writer.Write(Temperature);
            writer.Write(ElementIndex);
            writer.Write(DiseaseIndex);
            writer.Write(DiseaseCount);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetId = reader.ReadInt32();
            Position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            Mass = reader.ReadSingle();
            Temperature = reader.ReadSingle();
            ElementIndex = reader.ReadUInt16();
            DiseaseIndex = reader.ReadByte();
            DiseaseCount = reader.ReadInt32();
        }

        public void OnDispatched()
        {
            try
            {
                if (ElementIndex >= ElementLoader.elements.Count)
                {
                    DebugConsole.LogError($"[WorldDamageSpawnResourcePacket] Invalid ElementIndex: {ElementIndex}");
                    return;
                }

                Element element = ElementLoader.elements[ElementIndex];
                if (element?.substance == null)
                {
                    DebugConsole.LogError($"[WorldDamageSpawnResourcePacket] Element or substance is null for index {ElementIndex}");
                    return;
                }

                float dropMass = Mass;
                if (dropMass <= 0f)
                {
                    DebugConsole.LogWarning($"[WorldDamageSpawnResourcePacket] Invalid mass: {dropMass}");
                    return;
                }

                // Validate grid position before spawning
                int cell = Grid.PosToCell(Position);
                if (!Grid.IsValidCell(cell))
                {
                    DebugConsole.LogWarning($"[WorldDamageSpawnResourcePacket] Invalid grid position: {Position} (cell: {cell})");
                    return;
                }

                // Check if Grid is initialized
                if (Grid.WidthInCells <= 0 || Grid.HeightInCells <= 0)
                {
                    DebugConsole.LogWarning("[WorldDamageSpawnResourcePacket] Grid not initialized properly");
                    return;
                }

                InvokePlaySoundForSubstance(element, Position);

                GameObject dropped = element.substance.SpawnResource(Position, dropMass, Temperature, DiseaseIndex, DiseaseCount);
                if (dropped == null)
                {
                    DebugConsole.LogWarning($"[WorldDamageSpawnResourcePacket] Failed to spawn resource at {Position}");
                    return;
                }

                NetworkIdentity identity = dropped.GetComponent<NetworkIdentity>();
                if (identity != null)
                {
                    identity.OverrideNetId(NetId);
                    DebugConsole.Log("[WorldDamageSpawnResourcePacket] Synchronized Network ID");
                }
                else
                {
                    DebugConsole.LogWarning("[WorldDamageSpawnResourcePacket] No NetworkIdentity component found on spawned resource");
                }

                Pickupable pickup = dropped.GetComponent<Pickupable>();
                if (pickup != null && pickup.GetMyWorld()?.worldInventory.IsReachable(pickup) == true)
                {
                    PopFXManager.Instance.SpawnFX(
                        PopFXManager.Instance.sprite_Resource,
                        Mathf.RoundToInt(dropMass) + " " + element.name,
                        dropped.transform
                    );
                }
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[WorldDamageSpawnResourcePacket] Error in OnDispatched: {ex}");
            }
        }

        private static void InvokePlaySoundForSubstance(Element element, Vector3 position)
        {
            var method = typeof(WorldDamage).GetMethod("PlaySoundForSubstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                DebugConsole.LogWarning("[Multiplayer] Could not find PlaySoundForSubstance via reflection.");
                return;
            }

            var worldDamage = WorldDamage.Instance;

            if (worldDamage == null)
            {
                DebugConsole.LogWarning("[Multiplayer] WorldDamage.Instance is null.");
                return;
            }

            method.Invoke(worldDamage, new object[] { element, position });
        }

    }
}
