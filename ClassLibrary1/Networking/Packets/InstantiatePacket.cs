using System.IO;
using UnityEngine;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Packets
{
    public class InstantiatePacket : IPacket
    {
        public string PrefabName;
        public Vector3 Position;
        public Quaternion Rotation;
        public string ObjectName;
        public bool InitializeId;
        public int GameLayer;

        public PacketType Type => PacketType.Instantiate;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PrefabName ?? "");
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);

            writer.Write(Rotation.x);
            writer.Write(Rotation.y);
            writer.Write(Rotation.z);
            writer.Write(Rotation.w);

            writer.Write(ObjectName ?? "");
            writer.Write(InitializeId);
            writer.Write(GameLayer);
        }

        public void Deserialize(BinaryReader reader)
        {
            PrefabName = reader.ReadString();

            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            Position = new Vector3(x, y, z);

            float rx = reader.ReadSingle();
            float ry = reader.ReadSingle();
            float rz = reader.ReadSingle();
            float rw = reader.ReadSingle();
            Rotation = new Quaternion(rx, ry, rz, rw);

            ObjectName = reader.ReadString();
            InitializeId = reader.ReadBoolean();
            GameLayer = reader.ReadInt32();
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost)
                return;

            GameObject prefab = Assets.GetPrefab(PrefabName);
            if (prefab == null)
            {
                DebugConsole.LogWarning($"[InstantiatePacket] Could not find prefab '{PrefabName}' via Assets.");
                return;
            }

            GameObject obj = Instantiate(prefab, Position, Rotation, ObjectName, InitializeId, GameLayer);

            if (obj != null)
                obj.SetActive(true);
        }

        private GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, string name, bool initializeId, int gameLayer)
        {
            if (original == null)
            {
                DebugUtil.LogWarningArgs("Missing prefab");
                return null;
            }

            GameObject gameObject;

            if (original.GetComponent<RectTransform>() != null)
            {
                gameObject = Object.Instantiate(original, position, rotation);
                // No parenting in this version, since clients can't trust object hierarchy (yet)
                // TODO pass in the parent? NEEDS TESTING
                // SetParent could be added here later if needed
            }
            else
            {
                gameObject = Object.Instantiate(original, position, rotation);
            }

            if (gameObject == null)
                return null;

            if (gameLayer != 0)
            {
                gameObject.SetLayerRecursively(gameLayer);
            }

            gameObject.name = name ?? original.name;

            KPrefabID instanceId = gameObject.GetComponent<KPrefabID>();
            if (instanceId != null)
            {
                if (initializeId)
                {
                    instanceId.InstanceID = KPrefabID.GetUniqueID();
                    KPrefabIDTracker.Get().Register(instanceId);
                }

                instanceId.InitializeTags(force_initialize: true);

                KPrefabID sourceId = original.GetComponent<KPrefabID>();
                if (sourceId != null)
                {
                    instanceId.CopyTags(sourceId);
                    instanceId.CopyInitFunctions(sourceId);
                }

                instanceId.RunInstantiateFn();
            }

            return gameObject;
        }

    }
}
