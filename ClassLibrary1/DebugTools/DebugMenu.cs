using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using Utils = ONI_MP.Misc.Utils;
using UnityEngine;
using ONI_MP.Networking.Components;
using KMod;

namespace ONI_MP.DebugTools
{
    public class DebugMenu : MonoBehaviour
    {
        private static DebugMenu _instance;

        private bool showMenu = false;
        private Rect windowRect = new Rect(10, 10, 250, 300); // Position and size
        private HierarchyViewer hierarchyViewer;
        private DebugConsole debugConsole;

        private Vector2 scrollPosition = Vector2.zero;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Init()
        {
            if (_instance != null) return;

            GameObject go = new GameObject("ONI_MP_DebugMenu");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DebugMenu>();
        }

        private void Awake()
        {
            hierarchyViewer = gameObject.AddComponent<HierarchyViewer>();
            debugConsole = gameObject.AddComponent<DebugConsole>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                showMenu = !showMenu;
            }
        }

        private void OnGUI()
        {
            if (!showMenu) return;

            GUIStyle windowStyle = new GUIStyle(GUI.skin.window) { padding = new RectOffset(10, 10, 20, 20) };
            windowRect = GUI.ModalWindow(888, windowRect, DrawMenuContents, "DEBUG MENU", windowStyle);
        }

        private void DrawMenuContents(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.Width(windowRect.width - 20), GUILayout.Height(windowRect.height - 40));

            if (GUILayout.Button("Toggle Hierarchy Viewer"))
                hierarchyViewer.Toggle();

            if (GUILayout.Button("Toggle Debug Console"))
                debugConsole.Toggle();

            if (GUILayout.Button("Create Lobby"))
                SteamLobby.CreateLobby(onSuccess: () => {
                    SpeedControlScreen.Instance?.Unpause(false);
                });

            if (GUILayout.Button("Leave lobby"))
                SteamLobby.LeaveLobby();

            if (GUILayout.Button("Client disconnect"))
            {
                var n = nameof(WorldDamage);
                GameClient.CacheCurrentServer();
                GameClient.Disconnect();
            }

            if (GUILayout.Button("Reconnect"))
                GameClient.ReconnectFromCache();

            GUILayout.Space(10);

            if (MultiplayerSession.InSession)
            {
                if (!MultiplayerSession.IsHost)
                {
                    int? ping = GameClient.GetPingToHost();
                    string pingDisplay = ping >= 0 ? $"{ping} ms" : "Pending...";
                    GUILayout.Label($"Ping to Host: {pingDisplay}");
                }
                else
                {
                    GUILayout.Label("Hosting multiplayer session.");
                }

                GUILayout.Label($"Packets Sent: {SteamLobby.Stats.PacketsSent} ({SteamLobby.Stats.SentPerSecond}/sec)");
                GUILayout.Label($"Packets Received: {SteamLobby.Stats.PacketsReceived} ({SteamLobby.Stats.ReceivedPerSecond}/sec)");
                GUILayout.Label($"Total Bandwidth Sent: {Utils.FormatBytes(SteamLobby.Stats.BytesSent)}");
                GUILayout.Label($"Total Bandwidth Received: {Utils.FormatBytes(SteamLobby.Stats.BytesReceived)}");
                GUILayout.Label($"Bandwidth Sent/sec: {Utils.FormatBytes(SteamLobby.Stats.BytesSentSec)}");
                GUILayout.Label($"Bandwidth Received/sec: {Utils.FormatBytes(SteamLobby.Stats.BytesReceivedSec)}");

                if (GUILayout.Button("Reset Packet Counters"))
                    SteamLobby.Stats.ResetPacketCounters();

                if (GUILayout.Button("Test Save packet"))
                    SaveFileRequestPacket.SendSaveFile(MultiplayerSession.HostSteamID);

                GUILayout.Space(10);
                DrawPlayerList();
            }
            else
            {
                GUILayout.Label("Not in a multiplayer session.");
            }

            GUILayout.Space(20);
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        private void DrawPlayerList()
        {
            GUILayout.Label("Players in Lobby:", UnityEngine.GUI.skin.label);

            if (MultiplayerSession.ConnectedPlayers.Count == 0)
            {
                GUILayout.Label("<none>", UnityEngine.GUI.skin.label);
                return;
            }

            foreach (var kvp in MultiplayerSession.ConnectedPlayers)
            {
                var player = kvp.Value;
                string prefix = (MultiplayerSession.HostSteamID == player.SteamID) ? "[HOST] " : "";
                GUILayout.Label($"{prefix}{player.SteamName} ({player.SteamID})", UnityEngine.GUI.skin.label);
            }
        }

    }
}
