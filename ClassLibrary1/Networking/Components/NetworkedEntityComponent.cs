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
        }

        protected override void OnCleanUp()
        {
            var t = nameof(MoveChore);
            NetEntityRegistry.Unregister(NetId);
            base.OnCleanUp();
        }
    }
}
