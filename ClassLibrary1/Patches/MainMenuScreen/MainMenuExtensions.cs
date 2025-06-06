using System;
using UnityEngine;
using UnityEngine.UI;

namespace ONI_MP.Patches.MainMenuScreen
{
    public static class MainMenuExtensions
    {
        public static void AddClonedButton(this MainMenu menu, string text, string small_text, bool highlight, System.Action action)
        {
            var sourceButton = menu.Button_ResumeGame;
            if (sourceButton == null)
            {
                Debug.LogError("[ONI_MP] Button_ResumeGame is null");
                return;
            }

            GameObject newButtonGO = UnityEngine.Object.Instantiate(sourceButton.gameObject, sourceButton.transform.parent);
            newButtonGO.name = $"MPButton_{text.Replace(" ", "")}";

            var newButton = newButtonGO.GetComponent<KButton>();
            var texts = newButtonGO.GetComponentsInChildren<LocText>(includeInactive: true);

            // Set primary text (title)
            if (texts.Length > 0)
                texts[0].text = text;

            // Set secondary text (subtitle) to empty string
            if (texts.Length > 1)
                texts[1].text = small_text;

            newButton.onClick += action;

            Debug.Log($"[ONI_MP] Button '{text}' added to main menu.");
        }

    }
}
