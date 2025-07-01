using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.States;
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

        private CursorState cursorState = CursorState.NONE;

        System.Action OnCursorStateChanged;

        private Color playerColor = Color.white;
        private Shader playerCursorShader;
        private Material playerCursorMaterial;

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

            // only a single cursor image now
            cursorImage = CreateCursorImage(cursor, cursorTexture);

            // text stays the same
            cursorText = CreateCursorText(cursor, new Vector3(cursorTexture.width, -cursorTexture.height, 0));

            cursorImage.transform.SetSiblingIndex(0);      // base
            cursorText.transform.SetSiblingIndex(1);       // above

            cursor.transform.SetParent(transform, false);
            gameObject.SetLayerRecursively(LayerMask.NameToLayer("UI"));

            playerName = SteamFriends.GetFriendPersonaName(assignedPlayer);
            cursorText.text = $"{playerName}";

            OnCursorStateChanged += () => UpdateActionImage();

            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            SetColor(Color.white);
            SetVisibility(false);

            playerCursorShader = ResourceLoader.LoadShaderFromBundle("playercursorbundle", "assets/playercursoraction/playercursoraction.shader");

            if (playerCursorShader != null)
            {
                playerCursorMaterial = new Material(playerCursorShader);
                playerCursorMaterial.SetColor("_ReplacementColor", Color.white);
                playerCursorMaterial.SetFloat("_Threshold", 0.36f);
            }
        }

        private void UpdateActionImage()
        {
            string icon = GetCursorActionIcon(cursorState);

            if (string.IsNullOrEmpty(icon))
            {
                // fallback to the arrow
                var arrowTexture = Assets.GetTexture("cursor_arrow");
                cursorImage.sprite = Sprite.Create(
                    arrowTexture,
                    new Rect(0, 0, arrowTexture.width, arrowTexture.height),
                    Vector2.zero
                );
                //cursorImage.material = null; // clear any shader
                cursorImage.rectTransform.sizeDelta = new Vector2(arrowTexture.width, arrowTexture.height);

                // color the arrow with the player's assigned color
                cursorImage.color = playerColor;
            }
            else
            {
                var sprite = Assets.GetSprite(icon);
                if (sprite != null && playerCursorMaterial != null)
                {
                    cursorImage.sprite = sprite;

                    // assign sprite texture to shader
                    playerCursorMaterial.SetTexture("_MainTex", sprite.texture);

                    // assign material
                    cursorImage.material = playerCursorMaterial;

                    // resize
                    cursorImage.rectTransform.sizeDelta = new Vector2(sprite.rect.width * 0.1f, sprite.rect.height * 0.1f);

                    // keep Image.color white, let shader recolor it
                    cursorImage.color = Color.white;
                }
                else
                {
                    cursorImage.color = playerColor;
                    //cursorImage.material = null;
                    DebugConsole.LogWarning($"UpdateActionImage: Sprite '{icon}' not found or material missing.");
                }
            }
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

        private Image CreateCursorActionImage(GameObject parent, Sprite actionSprite)
        {
            var imageGameObject = new GameObject($"{name}_CursorActionImage") { transform = { parent = parent.transform } };

            var rectTransform = imageGameObject.AddComponent<RectTransform>();
            float scale = 0.1f;

            if (actionSprite != null)
            {
                float scaledWidth = actionSprite.rect.width * scale;
                float scaledHeight = actionSprite.rect.height * scale;
                rectTransform.sizeDelta = new Vector2(scaledWidth, scaledHeight);
                rectTransform.pivot = new Vector2(0.5f, 0f); // center-bottom
                rectTransform.anchoredPosition = new Vector2(0, -scaledHeight / 2f);
            }

            var imageComponent = imageGameObject.AddComponent<Image>();
            imageComponent.sprite = actionSprite;
            imageComponent.raycastTarget = false;

            return imageComponent;
        }

        private TextMeshProUGUI CreateCursorText(GameObject parent, Vector3 offset)
        {
            var textGameObject = new GameObject($"{name}_Name") { transform = { parent = parent.transform } };

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
            playerColor = col;
            if (cursorImage != null)
                cursorImage.color = playerColor;
            
            if (cursorText != null)
                cursorText.color = playerColor;

            if (playerCursorMaterial != null)
            {
                playerCursorMaterial.SetColor("_ReplacementColor", playerColor);
            }
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

        public void SetState(CursorState state)
        {
            if(this.cursorState != state) {
                this.cursorState = state;
                OnCursorStateChanged.Invoke();
            }
        }

        public static string GetCursorActionIcon(CursorState state)
        {
            switch (state)
            {
                case CursorState.NONE: return string.Empty;
                case CursorState.SELECT: return string.Empty;
                case CursorState.BUILD: return "icon_errand_build";
                case CursorState.DIG: return "icon_action_dig";
                case CursorState.CANCEL: return "icon_action_cancel";
                case CursorState.DECONSTRUCT: return "icon_action_deconstruct";
                case CursorState.PRIORITIZE: return "icon_action_prioritize";
                case CursorState.DEPRIORITIZE: return "icon_action_deprioritize";
                case CursorState.SWEEP: return "icon_action_store";
                case CursorState.MOP: return "icon_action_mop";
                case CursorState.HARVEST: return "icon_action_harvest";
                case CursorState.DISINFECT: return "icon_action_disinfect";
                case CursorState.ATTACK: return "icon_action_attack";
                case CursorState.CAPTURE: return "icon_action_capture";
                case CursorState.WRANGLE: return "icon_action_capture";
                case CursorState.EMPTY_PIPE: return "icon_action_empty_pipes";
                case CursorState.CLEAR_FLOOR: return "icon_action_store";
                case CursorState.MOVE_TO: return "icon_action_moveto";
                case CursorState.DISCONNECT: return "icon_action_disconnect";
                default: return $"[{state.ToString()}]";
            }
        }

    }
}
