using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Diagnostics;
using ONI_MP.DebugTools;
using ONI_MP.Networking;

namespace ONI_MP.UI
{
    public class ChatScreen : KScreen
    {
        public static ChatScreen instance;
        public TMP_InputField inputField;
        private RectTransform messageContainer;
        private List<TextMeshProUGUI> messages = new List<TextMeshProUGUI>();
        private RectTransform panelRectTransform;

        public static ChatScreen Instance => instance;

        private GameObject panel, scroll, input;

        public static void Show()
        {
            if (instance != null)
                return;

            var go = new GameObject("ChatScreen", typeof(RectTransform));
            instance = go.AddComponent<ChatScreen>();
            var parent = GameScreenManager.Instance.ssOverlayCanvas.transform;
            go.transform.SetParent(parent, false);
            instance.SetupUI();
        }


        private void SetupUI()
        {
            // Panel container
            panel = CreatePanel("ChatPanel", transform, new Vector2(400, 200), new Vector2(20, 20));
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            var panelRT = panel.GetComponent<RectTransform>();
            panelRectTransform = panelRT;

            // Scrollable message area
            scroll = CreateScrollArea("Scroll", panel.transform, out messageContainer);
            var scrollRT = scroll.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.pivot = new Vector2(0.5f, 1f);
            scrollRT.offsetMin = new Vector2(10, 50);   // Space for input field at bottom
            scrollRT.offsetMax = new Vector2(-10, -10); // Optional top padding

            // Manually size the message container to match panel
            messageContainer.anchorMin = new Vector2(0, 0);
            messageContainer.anchorMax = new Vector2(1, 1);
            messageContainer.offsetMin = Vector2.zero;
            messageContainer.offsetMax = Vector2.zero;
            messageContainer.pivot = new Vector2(0.5f, 1f); // Top-left alignment

            // Input field pinned to bottom
            inputField = CreateInputField("ChatInput", panel.transform, new Vector2(10, 15), new Vector2(380, 30));
            input = inputField.gameObject;

            // Optional: pre-fill debug message
            AddMessage("<color=yellow>Debug:</color> Chat initialized.");
            for(int i = 0; i < 10; i++)
            {
                AddMessage($"<color=blue>Test:</color> Test message {i}");
            }
        }

        public void AddMessage(string text)
        {
            var go = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(messageContainer, false);

            // Stretch horizontally and auto-fit height
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-10, 0);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = Utils.GetDefaultTMPFont();
            tmp.fontSize = 18;
            tmp.enableWordWrapping = true;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            tmp.margin = new Vector4(4, 4, 4, 4);

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            messages.Add(tmp);

            // Force scroll to bottom
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer);
            var scrollRect = messageContainer.GetComponentInParent<ScrollRect>();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        public static bool IsFocused()
        {
            return instance != null && instance.inputField != null && instance.inputField.isFocused;
        }

        private void Update()
        {
            panel.SetActive(MultiplayerSession.InSession);
            scroll.SetActive(MultiplayerSession.InSession);
            input.SetActive(MultiplayerSession.InSession);
            if (!MultiplayerSession.InSession)
            {
                return;
            }

            // ENTER while focused: submit and unfocus
            if (inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                string message = inputField.text;
                DebugConsole.Log($"Send message: {message}");
                if (!string.IsNullOrWhiteSpace(message))
                {
                    AddMessage($"<color=green>You:</color> {message}");
                    inputField.text = "";
                }

                inputField.DeactivateInputField();
            }

            // ENTER while NOT focused: activate input
            else if (!inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                inputField.ActivateInputField();
            }

            // ESC while focused: cancel typing
            else if (inputField.isFocused && Input.GetKeyDown(KeyCode.Escape))
            {
                inputField.DeactivateInputField();
            }
        }

        public GameObject CreatePanel(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var panel = new GameObject(name, typeof(Image));
            panel.transform.SetParent(parent, false);

            var rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = new Vector2(0.5f, 0); // Bottom center
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = position; // e.g., new Vector2(0, 20)

            var image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.5f); // translucent

            return panel;
        }

        public TMP_InputField CreateInputField(string name, Transform parent, Vector2 position, Vector2 size)
        {
            // Container with background
            var go = new GameObject(name, typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = new Vector2(0, 0); // bottom left
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = position;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Background color

            var input = go.GetComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.scrollSensitivity = 0f;

            // Viewport
            var viewportGO = new GameObject("TextViewport", typeof(RectMask2D), typeof(Image));
            viewportGO.transform.SetParent(go.transform, false);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(5, 4);
            viewportRT.offsetMax = new Vector2(-5, -4);
            viewportGO.GetComponent<Image>().color = Color.clear;

            // Text component
            var textGO = new GameObject("Text", typeof(TextMeshProUGUI));
            textGO.transform.SetParent(viewportGO.transform, false);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 18;
            tmp.enableWordWrapping = false;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            var textRT = tmp.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            // Highlight + Caret visuals
            input.textViewport = viewportRT;
            input.textComponent = tmp;
            input.placeholder = null;
            input.targetGraphic = bg;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.3f, 0.6f, 1f, 0.5f); // Light blue selection
            input.customCaretColor = true;

            return input;
        }

        public GameObject CreateScrollArea(string name, Transform parent, out RectTransform content)
        {
            var scrollGO = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();

            scrollRT.anchorMin = new Vector2(0, 0);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.pivot = new Vector2(0.5f, 0);
            scrollRT.offsetMin = new Vector2(10, 50);    // Bottom padding for input field
            scrollRT.offsetMax = new Vector2(-10, -10);  // Top padding of 10px

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0); // Transparent

            // Content container
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewport.transform, false);
            content = contentGO.GetComponent<RectTransform>();

            var layout = contentGO.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 5;

            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewportRT;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 50f;

            return scrollGO;
        }

        public static bool IsMouseOverChatPanel()
        {
            if (Instance == null || Instance.panelRectTransform == null)
                return false;

            Vector2 mousePos = Input.mousePosition;
            return RectTransformUtility.RectangleContainsScreenPoint(Instance.panelRectTransform, mousePos, Camera.main);
        }

    }
}
