using System.Collections;
using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Core
{
    public class PlayerCursorPacket : IPacket
    {
        public CSteamID SteamID;
        public Vector3 Position;

        public PacketType Type => PacketType.PlayerCursor;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SteamID.m_SteamID);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
        }

        public void Deserialize(BinaryReader reader)
        {
            SteamID = new CSteamID(reader.ReadUInt64());
            Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.TryGetCursorObject(SteamID, out var cursorGO))
            {
                var cursorComponent = cursorGO.GetComponent<PlayerCursor>();
                if (cursorComponent != null)
                {
                    cursorComponent.StopCoroutine("InterpolateCursorPosition");
                    cursorComponent.StartCoroutine(InterpolateCursorPosition(cursorComponent.transform, Position));
                }
                else
                {
                    DebugConsole.LogWarning($"[PlayerCursorPacket] GameObject exists but missing PlayerCursor for SteamID {SteamID}");
                }
            }
            else
            {
                DebugConsole.LogWarning($"[PlayerCursorPacket] No cursor object found for SteamID {SteamID}");
            }


            // Forward to others if host
            if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllExcluding(this, new HashSet<CSteamID>
                {
                    SteamID,
                    MultiplayerSession.LocalSteamID
                });
            }
        }

        private IEnumerator InterpolateCursorPosition(Transform target, Vector3 targetPos)
        {
            Vector3 start = target.position;
            float duration = CursorManager.SendInterval;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                target.position = Vector3.Lerp(start, targetPos, t);
                yield return null;
            }

            target.position = targetPos;
        }

    }
}
