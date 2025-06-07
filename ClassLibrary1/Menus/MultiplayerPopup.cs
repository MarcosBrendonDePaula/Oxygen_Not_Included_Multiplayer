using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Networking;
using ONI_MP.Patches.MainMenuScreen;
using UnityEngine;

namespace ONI_MP.Menus
{
    public static class MultiplayerPopup
    {
        public static void Show(Transform parent)
        {
            // Create base popup container
            GameObject popup = new GameObject("MP_Popup", typeof(RectTransform), typeof(CanvasGroup));
            popup.transform.SetParent(parent, worldPositionStays: false);

            var rt = popup.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 200);
            rt.anchoredPosition = Vector2.zero;

            var canvasGroup = popup.GetComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // Create buttons using existing resume button as style
            AddPopupButton(popup.transform, "Host Game", new Vector2(0, 50), () =>
            {
                Debug.Log("Host Game clicked");
                MainMenuPatch.Instance.Button_ResumeGame.SignalClick(KKeyCode.Mouse0);
                SteamLobby.CreateLobby();
                UnityEngine.Object.Destroy(popup);
            });

            AddPopupButton(popup.transform, "Join Game", new Vector2(0, 0), () =>
            {
                Debug.Log("Join Game clicked");
                UnityEngine.Object.Destroy(popup);
            });

            AddPopupButton(popup.transform, "Cancel", new Vector2(0, -50), () =>
            {
                UnityEngine.Object.Destroy(popup);
            });

            Debug.Log("Multiplayer popup opened.");
        }

        private static void AddPopupButton(Transform parent, string text, Vector2 position, System.Action onClick)
        {
            // Find a template button in the scene to clone
            var template = UnityEngine.Object.FindObjectOfType<MainMenu>()?.Button_ResumeGame;
            if (template == null)
            {
                Debug.LogError("Cannot find template button to clone.");
                return;
            }

            GameObject btnGO = UnityEngine.Object.Instantiate(template.gameObject, parent);
            btnGO.name = $"MP_{text.Replace(" ", "")}_Button";

            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchoredPosition = position;

            var btn = btnGO.GetComponent<KButton>();
            var label = btnGO.GetComponentInChildren<LocText>();
            if (label != null)
                label.text = text;

            btn.onClick += () => onClick();
        }
    }

}
