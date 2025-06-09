using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using Utils = ONI_MP.Misc.Utils;
using UnityEngine;

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

            if (GUILayout.Button("Test DoLoad"))
            {
                LoadingOverlay.Load(() =>
                {
                    LoadScreen.DoLoad("C:\\Users\\luke\\Documents\\Klei\\OxygenNotIncluded\\cloud_save_files\\76561198021490625\\Dump\\Dump.sav");
                });
            }

            if (GUILayout.Button("Create Lobby"))
                SteamLobby.CreateLobby(onSuccess: () => {
                    SpeedControlScreen.Instance?.Unpause(false);
                });

            if (GUILayout.Button("Leave lobby"))
                SteamLobby.LeaveLobby();

            GUILayout.Space(10);

            if (MultiplayerSession.InSession)
            {
                var local = MultiplayerSession.LocalPlayer;
                if (local != null)
                {
                    if (!MultiplayerSession.IsHost)
                    {
                        string pingDisplay = local.Ping >= 0 ? $"{local.Ping} ms" : "Pending...";
                        GUILayout.Label($"Ping to Host: {pingDisplay}");
                    }
                    else
                    {
                        GUILayout.Label("Hosting multiplayer session.");
                    }
                }
                else
                {
                    GUILayout.Label("Ping to Host: Unknown");
                }

                GUILayout.Label($"Packets Sent: {SteamLobby.PacketsSent} ({SteamLobby.SentPerSecond}/sec)");
                GUILayout.Label($"Packets Received: {SteamLobby.PacketsReceived} ({SteamLobby.ReceivedPerSecond}/sec)");
                GUILayout.Label($"Total Bandwidth Sent: {Utils.FormatBytes(SteamLobby.BytesSent)}");
                GUILayout.Label($"Total Bandwidth Received: {Utils.FormatBytes(SteamLobby.BytesReceived)}");
                GUILayout.Label($"Bandwidth Sent/sec: {Utils.FormatBytes(SteamLobby.BytesSentSec)}");
                GUILayout.Label($"Bandwidth Received/sec: {Utils.FormatBytes(SteamLobby.BytesReceivedSec)}");

                if (GUILayout.Button("Reset Packet Counters"))
                    SteamLobby.ResetPacketCounters();

                if (GUILayout.Button("Test Save packet"))
                    SaveFileRequestPacket.SendSaveFile(MultiplayerSession.HostSteamID);
            }
            else
            {
                GUILayout.Label("Not in a multiplayer session.");
            }

            GUILayout.Space(20);
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }
    }
}
