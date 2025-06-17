using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using System.Collections.Generic;
using System.IO;

public class PlayAnimPacket : IPacket
{
    public PacketType Type => PacketType.PlayAnim;

    public int NetId;
    public bool IsMulti;
    public List<int> AnimHashes = new List<int>(); // For multi
    public int SingleAnimHash;                     // For single
    public KAnim.PlayMode Mode;
    public float Speed;
    public float Offset;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NetId);
        writer.Write(IsMulti);
        writer.Write((int)Mode);
        writer.Write(Speed);
        writer.Write(Offset);

        if (IsMulti)
        {
            writer.Write(AnimHashes.Count);
            foreach (var hash in AnimHashes)
                writer.Write(hash);
        }
        else
        {
            writer.Write(SingleAnimHash);
        }
    }

    public void Deserialize(BinaryReader reader)
    {
        NetId = reader.ReadInt32();
        IsMulti = reader.ReadBoolean();
        Mode = (KAnim.PlayMode)reader.ReadInt32();
        Speed = reader.ReadSingle();
        Offset = reader.ReadSingle();

        if (IsMulti)
        {
            int count = reader.ReadInt32();
            AnimHashes = new List<int>(count);
            for (int i = 0; i < count; i++)
                AnimHashes.Add(reader.ReadInt32());
        }
        else
        {
            SingleAnimHash = reader.ReadInt32();
        }
    }

    public void OnDispatched()
    {
        if (MultiplayerSession.IsHost)
            return;

        if (NetEntityRegistry.TryGet(NetId, out var go) &&
            go.TryGetComponent(out KAnimControllerBase controller))
        {
            if (IsMulti)
            {
                var hashedStrings = AnimHashes.ConvertAll(hash => new HashedString(hash)).ToArray();
                controller.Play(hashedStrings, Mode);
            }
            else
            {
                controller.Play(new HashedString(SingleAnimHash), Mode, Speed, Offset);
            }
        }
    }
}
