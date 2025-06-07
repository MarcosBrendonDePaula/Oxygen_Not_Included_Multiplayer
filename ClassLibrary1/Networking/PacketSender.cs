using System.IO;
using ONI_MP.DebugTools;
using Steamworks;

namespace ONI_MP.Networking
{
    public static class PacketSender
    {
        public static byte[] SerializePacket(IPacket packet)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write((byte)packet.Type);
                    packet.Serialize(writer);
                }
                return ms.ToArray();
            }
        }

        public static void SendToPlayer(CSteamID target, IPacket packet, EP2PSend sendType = EP2PSend.k_EP2PSendReliable)
        {
            var bytes = SerializePacket(packet);
            bool sent = SteamNetworking.SendP2PPacket(target, bytes, (uint)bytes.Length, sendType, 0);

            if (!sent)
            {
                DebugConsole.LogError($"[SteamNetworking] Failed to send packet to {target}");
            }
        }

        public static void SendToAll(IPacket packet, EP2PSend sendType = EP2PSend.k_EP2PSendReliable, CSteamID? exclude = null)
        {
            var bytes = SerializePacket(packet);

            foreach (var kvp in MultiplayerSession.ConnectedPlayers)
            {
                CSteamID peerID = kvp.Key;

                if (exclude.HasValue && peerID == exclude.Value)
                    continue;

                bool sent = SteamNetworking.SendP2PPacket(peerID, bytes, (uint)bytes.Length, sendType, 0);

                if (!sent)
                {
                    DebugConsole.LogError($"[SteamNetworking] Failed to send packet to {peerID}");
                }
            }
        }


    }



}
