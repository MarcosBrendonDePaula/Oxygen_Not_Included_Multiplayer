using ONI_MP.DebugTools;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
    public class NetworkedEntityComponent : KMonoBehaviour
    {
        public int NetId { get; private set; }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            NetId = NetEntityRegistry.Register(this);
            DebugConsole.Log($"Added Network Entity Component to {name} with NetId: {NetId}");
        }

        protected override void OnCleanUp()
        {
            var t = nameof(MoveChore);
            NetEntityRegistry.Unregister(NetId);
            DebugConsole.Log($"Cleaned up Network Entity Component to {name} with NetId: {NetId}");
            base.OnCleanUp();
        }
    }
}
