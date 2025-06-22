using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager Instance { get; private set; }

        public static float SendInterval = 0.1f;

        private float timeSinceLastSend = 0f;
        private Camera mainCamera;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            mainCamera = GameScreenManager.Instance.GetCamera(GameScreenManager.UIRenderTarget.ScreenSpaceCamera);
        }

        private void Update()
        {
            if (!Utils.IsInGame())
                return;

            if (!MultiplayerSession.InSession || !MultiplayerSession.LocalSteamID.IsValid())
                return;

            timeSinceLastSend += Time.unscaledDeltaTime;
            if (timeSinceLastSend >= SendInterval)
            {
                SendCursorPosition();
                timeSinceLastSend = 0f;
            }
        }

        private void SendCursorPosition()
        {
            Vector3 cursorWorldPos = GetCursorWorldPosition();
            var packet = new PlayerCursorPacket
            {
                SteamID = MultiplayerSession.LocalSteamID,
                Position = cursorWorldPos
            };

            if(MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(packet);
            } else
            {
                PacketSender.SendToHost(packet, SteamNetworkingSend.Unreliable);
            }
        }

        private Vector3 GetCursorWorldPosition()
        {
            Vector3 screenPos = Input.mousePosition;
            screenPos.z = 0f;
            return mainCamera.ScreenToWorldPoint(screenPos);
        }
    }
}
