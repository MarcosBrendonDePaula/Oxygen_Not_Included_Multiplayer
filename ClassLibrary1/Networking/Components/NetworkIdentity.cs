using KSerialization;
using UnityEngine;
using ONI_MP.DebugTools;
using System.IO;

namespace ONI_MP.Networking.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class NetworkIdentity : KMonoBehaviour, ISaveLoadableDetails
    {
        [Serialize]
        public int NetId;

        public void Serialize(BinaryWriter writer)
        {
            DebugConsole.Log($"[NetworkIdentity] SERIALIZING: NetId = {NetId} on {name}");
        }

        public void Deserialize(IReader reader)
        {
            DebugConsole.Log($"[NetworkIdentity] DESERIALIZED: NetId = {NetId} on {name}");
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (NetId == 0)
            {
                NetId = NetEntityRegistry.Register(this);
                DebugConsole.Log($"[NetworkIdentity] Registered new NetId {NetId} for {name}");
            }
            else
            {
                NetEntityRegistry.RegisterExisting(this, NetId);
                DebugConsole.Log($"[NetworkIdentity] Restored NetId {NetId} for {name}");
            }
        }

        protected override void OnCleanUp()
        {
            NetEntityRegistry.Unregister(NetId);
            DebugConsole.Log($"[NetworkIdentity] Unregistered NetId {NetId} for {name}");
            base.OnCleanUp();
        }
    }
}
