using UnityEngine;
using UnityEngine.SceneManagement;

namespace ONI_MP.DebugTools
{
    public class HierarchyViewer : MonoBehaviour
    {
        private bool showWindow = false;
        private Vector2 scrollPos = Vector2.zero;

        public void Toggle()
        {
            showWindow = !showWindow;
            Debug.Log($"[ONI_MP] HierarchyViewer toggled: {showWindow}");
        }

        void OnGUI()
        {
            if (!showWindow) return;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                padding = new RectOffset(8, 8, 8, 8)
            };

            GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20), "Hierarchy", boxStyle);
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                DrawGameObjectRecursive(root, 0);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGameObjectRecursive(GameObject obj, int indent)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 16);
            GUILayout.Label(obj.name);
            GUILayout.EndHorizontal();

            foreach (Transform child in obj.transform)
            {
                DrawGameObjectRecursive(child.gameObject, indent + 1);
            }
        }
    }
}
