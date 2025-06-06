using ONI_MP.Networking;
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
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showMenu = !showMenu;
                DebugConsole.Log($"[ONI_MP] Debug menu toggled: {showMenu}");
            }
        }

        private void OnGUI()
        {
            if (!showMenu) return;

            GUIStyle windowStyle = new GUIStyle(GUI.skin.window) { padding = new RectOffset(10, 10, 20, 10) };
            windowRect = GUI.ModalWindow(888, windowRect, DrawMenuContents, "DEBUG MENU", windowStyle);
        }

        private void DrawMenuContents(int windowID)
        {
            if (GUILayout.Button("Toggle Hierarchy Viewer"))
                hierarchyViewer.Toggle();

            if (GUILayout.Button("Toggle Debug Console"))
                debugConsole.Toggle();

            if (GUILayout.Button("Test steamworks"))
                DebugConsole.Log($"Steamworks Initialized: {SteamManager.Initialized}");

            if (GUILayout.Button("Create Lobby"))
                SteamLobby.CreateLobby();

            if (GUILayout.Button("Leave lobby"))
                SteamLobby.LeaveLobby();

            GUILayout.Space(20);

            GUI.DragWindow(); // Makes the window draggable
        }
    }
}
