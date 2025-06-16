using ONI_MP.Networking;
using System.IO;

public class PlayAnimPacket : IPacket
{
    public PacketType Type => PacketType.PlayAnim;

    public int NetId;
    public int AnimHash;
    public KAnim.PlayMode Mode;
    public float Speed;
    public float Offset;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NetId);
        writer.Write(AnimHash);
        writer.Write((int)Mode);
        writer.Write(Speed);
        writer.Write(Offset);
    }

    public void Deserialize(BinaryReader reader)
    {
        NetId = reader.ReadInt32();
        AnimHash = reader.ReadInt32();
        Mode = (KAnim.PlayMode)reader.ReadInt32();
        Speed = reader.ReadSingle();
        Offset = reader.ReadSingle();
    }

    public void OnDispatched()
    {
        if (MultiplayerSession.IsHost)
            return;

        if (NetEntityRegistry.TryGet(NetId, out var go) &&
            go.TryGetComponent(out KAnimControllerBase controller))
        {
            controller.Play(new HashedString(AnimHash), Mode, Speed, Offset);
        }
    }
}
