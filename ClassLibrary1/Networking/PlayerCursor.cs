using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONI_MP.Networking
{
    public class PlayerCursor : KMonoBehaviour
    {
        [MyCmpAdd]
        private readonly Canvas canvas = null;

        private Camera camera = null;

        private Image cursorImage = null;
        private TextMeshProUGUI cursorText = null;

        private CSteamID assignedPlayer;
        string playerName = string.Empty;

        protected override void OnSpawn()
        {
            base.OnSpawn();
        }

        public void AssignPlayer(CSteamID steamId)
        {
            this.assignedPlayer = steamId;
        }

        public void Init()
        {
            camera = GameScreenManager.Instance.GetCamera(GameScreenManager.UIRenderTarget.ScreenSpaceCamera);

            var cursorTexture = Assets.GetTexture("cursor_arrow");
            var cursor = new GameObject(name);
            cursorImage = CreateCursorImage(cursor, cursorTexture);
            cursorText = CreateCursorText(cursor, new Vector3(cursorTexture.width, -cursorTexture.height, 0));
            cursor.transform.SetParent(transform, false);
            gameObject.SetLayerRecursively(LayerMask.NameToLayer("UI"));

            playerName = SteamFriends.GetFriendPersonaName(assignedPlayer);
            cursorText.text = playerName;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            SetColor(Color.white); // Default to white
            SetVisibility(false); // Hide by default
        }

        private Image CreateCursorImage(GameObject parent, Texture2D cursorTexture)
        {
            var imageGameObject = new GameObject(name) { transform = { parent = parent.transform } };
            var rectTransform = imageGameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(cursorTexture.width, cursorTexture.height);
            rectTransform.pivot = new Vector2(0, 1); // Align to top left corner.

            var imageComponent = imageGameObject.AddComponent<Image>();
            imageComponent.sprite = Sprite.Create(
                cursorTexture,
                new Rect(0, 0, cursorTexture.width, cursorTexture.height),
                Vector2.zero
            );
            imageComponent.raycastTarget = false;
            return imageComponent;
        }

        private TextMeshProUGUI CreateCursorText(GameObject parent, Vector3 offset)
        {
            var textGameObject = new GameObject(name) { transform = { parent = parent.transform } };

            var rectTransform = textGameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(50, 50);
            rectTransform.pivot = new Vector2(0, 1); // Align to top left corner.
            rectTransform.position = offset;

            var textComponent = textGameObject.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 14;
            textComponent.font = Localization.FontAsset;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            textComponent.enableWordWrapping = false;

            return textComponent;
        }

        public void SetColor(Color col)
        {
            if (cursorImage != null)
                cursorImage.color = col;
            if (cursorText != null)
                cursorText.color = col;
        }

        // Using the color make it fully transparent instead of deactivating the object
        public void SetVisibility(bool visible)
        {
            if (cursorImage != null)
            {
                var color = cursorImage.color;
                color.a = visible ? 1f : 0f;
                cursorImage.color = color;
            }

            if (cursorText != null)
            {
                var color = cursorText.color;
                color.a = visible ? 1f : 0f;
                cursorText.color = color;
            }
        }

    }
}
