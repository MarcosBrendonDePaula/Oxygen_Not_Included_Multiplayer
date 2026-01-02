using UnityEngine;
using System;
using ONI_MP.DebugTools;
using ONI_MP.Networking;

namespace ONI_MP.Menus
{
    /// <summary>
    /// Main coordinator for mod compatibility interface - delegates to specialized components
    /// </summary>
    public class ModCompatibilityGUI : MonoBehaviour
    {
        private static ModCompatibilityGUI instance;
        private static bool showDialog = false;
        private static string dialogReason = "";
        private static string[] dialogMissingMods = null;
        private static string[] dialogExtraMods = null;
        private static string[] dialogVersionMismatches = null;
        private static ulong[] dialogSteamModIds = null;

        private Vector2 scrollPosition = Vector2.zero;
        private Rect windowRect = new Rect(0, 0, 600, 400);

        /// <summary>
        /// Shows the incompatibility error dialog
        /// </summary>
        public static void ShowIncompatibilityError(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches, ulong[] steamModIds)
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Showing compatibility dialog");
                DebugConsole.Log($"[ModCompatibilityGUI] Steam mod IDs for auto-install: {steamModIds?.Length ?? 0}");

                // Store dialog data
                dialogReason = reason ?? "";
                dialogMissingMods = missingMods ?? new string[0];
                dialogExtraMods = extraMods ?? new string[0];
                dialogVersionMismatches = versionMismatches ?? new string[0];
                dialogSteamModIds = steamModIds ?? new ulong[0];

                // Reset mod state management
                ModStateManager.ResetAllModStates();
                ModStateManager.ClearModVerificationCache();
                ModLogThrottler.ClearThrottling();
                ModRestartManager.Reset();

                // Create or get the GUI component
                if (instance == null)
                {
                    GameObject guiObject = new GameObject("ModCompatibilityGUI");
                    DontDestroyOnLoad(guiObject);
                    instance = guiObject.AddComponent<ModCompatibilityGUI>();
                }

                // Center the window on screen
                instance.windowRect.x = (Screen.width - instance.windowRect.width) / 2;
                instance.windowRect.y = (Screen.height - instance.windowRect.height) / 2;

                showDialog = true;

                DebugConsole.Log("[ModCompatibilityGUI] Dialog enabled - waiting for user choice");

                if (missingMods != null && missingMods.Length > 0)
                {
                    DebugConsole.Log($"[ModCompatibilityGUI] Detected {missingMods.Length} missing mods - waiting for user choice");
                }
                else
                {
                    DebugConsole.Log("[ModCompatibilityGUI] No missing mods detected - showing informational screen");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Failed to show dialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the dialog and handles restart if needed
        /// </summary>
        public static void CloseDialog()
        {
            showDialog = false;

            // Clear all caches and throttling
            ModStateManager.ClearModVerificationCache();
            ModLogThrottler.ClearThrottling();

            // Handle restart if mods were modified
            ModRestartManager.HandleRestartIfNeeded();

            // Close any multiplayer overlays
            try
            {
                MultiplayerOverlay.Close();
                DebugConsole.Log("[ModCompatibilityGUI] Closed MultiplayerOverlay");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error closing MultiplayerOverlay: {ex.Message}");
            }

            if (instance != null)
            {
                DestroyImmediate(instance.gameObject);
                instance = null;
            }
        }

        /// <summary>
        /// Shows restart notification
        /// </summary>
        public static void ShowRestartNotification()
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Showing restart notification");

                // Create or get the GUI component if not already present
                if (instance == null)
                {
                    GameObject guiObject = new GameObject("ModCompatibilityGUI");
                    DontDestroyOnLoad(guiObject);
                    instance = guiObject.AddComponent<ModCompatibilityGUI>();
                }

                ModRestartManager.ShowRestartNotification();
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Failed to show restart notification: {ex.Message}");
            }
        }

        void Update()
        {
            // Update restart notifications
            ModRestartManager.UpdateRestartNotification();
        }

        void OnGUI()
        {
            // Draw restart dialog if active
            if (ModRestartManager.ShouldShowRestartDialog)
            {
                ModCompatibilityDialogs.DrawRestartDialog();
                return; // Show only restart dialog when active
            }

            // Draw restart notification if active
            if (ModRestartManager.ShouldShowRestartNotification)
            {
                ModCompatibilityDialogs.DrawRestartNotification();
            }

            if (!showDialog) return;

            // Dark semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Create custom style for the window
            GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f, 0.95f));
            windowStyle.border = new RectOffset(5, 5, 5, 5);

            // Main dialog window
            windowRect = GUI.Window(12345, windowRect, DrawDialogWindow, "", windowStyle);
        }

        void DrawDialogWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = ModProgressTracker.IsInstalling ? Color.cyan : Color.red;

            string headerText = ModProgressTracker.IsInstalling ?
                MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALLING :
                MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.TITLE;

            GUILayout.Label(headerText, headerStyle);
            GUILayout.Space(10);

            // Scroll area for content
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(280));

            // Installation progress section (highest priority)
            if (ModProgressTracker.IsInstalling)
            {
                ModCompatibilityDialogs.DrawInstallationProgress();
                GUILayout.Space(10);
            }
            // Restart required message (if all mods were enabled)
            else if (AreAllModsEnabled())
            {
                DrawAllModsEnabledMessage();
            }
            // Show modification message (if mods were changed but not all enabled yet)
            else if (ModRestartManager.ModsWereModified)
            {
                DrawModsModifiedMessage();
            }
            // Reason text
            else if (!string.IsNullOrEmpty(dialogReason))
            {
                GUIStyle reasonStyle = new GUIStyle(GUI.skin.label);
                reasonStyle.fontStyle = FontStyle.Bold;
                reasonStyle.wordWrap = true;
                reasonStyle.normal.textColor = Color.white;

                GUILayout.Label(dialogReason, reasonStyle);
                GUILayout.Space(10);
            }

            // Missing mods section - only show if actually missing and not all enabled
            if (!AreAllModsEnabled() && dialogMissingMods != null && dialogMissingMods.Length > 0)
            {
                DrawMissingModsSection();
            }

            // Extra mods section (only show if no missing mods - policy permissive)
            if (dialogExtraMods != null && dialogExtraMods.Length > 0 &&
                (dialogMissingMods == null || dialogMissingMods.Length == 0) &&
                (dialogVersionMismatches == null || dialogVersionMismatches.Length == 0))
            {
                ModCompatibilityDialogs.DrawInfoSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.EXTRA_MODS_INFO, dialogExtraMods);
                GUILayout.Space(10);
            }
            else if (dialogExtraMods != null && dialogExtraMods.Length > 0)
            {
                ModCompatibilityDialogs.DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.EXTRA_MODS_SECTION, dialogExtraMods, Color.yellow, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.VIEW);
                GUILayout.Space(10);
            }

            // Version mismatches section
            if (dialogVersionMismatches != null && dialogVersionMismatches.Length > 0)
            {
                ModCompatibilityDialogs.DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.VERSION_MISMATCH_SECTION, dialogVersionMismatches, Color.cyan, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.UPDATE);
                GUILayout.Space(10);
            }

            // Instructions
            DrawInstructions();

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Action buttons section (hide if all mods enabled)
            GUILayout.BeginHorizontal();

            if (!AreAllModsEnabled())
            {
                ModCompatibilityDialogs.DrawActionButtons(dialogMissingMods);
            }

            GUILayout.FlexibleSpace();

            // Close button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
            buttonStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.CLOSE, buttonStyle, GUILayout.Height(35)))
            {
                CloseDialog();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow();
        }

        /// <summary>
        /// Draws the missing mods section with categorization
        /// </summary>
        private void DrawMissingModsSection()
        {
            // Filter only mods that are truly not installed/enabled
            var trulyMissingMods = new System.Collections.Generic.List<string>();
            var installedButDisabledMods = new System.Collections.Generic.List<string>();

            foreach (var mod in dialogMissingMods)
            {
                if (ModStateManager.IsModEnabled(mod))
                {
                    // If enabled, not really "missing"
                    DebugConsole.Log($"[ModCompatibilityGUI] Mod {mod} is enabled, ignoring from missing list");
                    continue;
                }
                else if (ModStateManager.IsModInstalled(mod))
                {
                    installedButDisabledMods.Add(mod);
                }
                else
                {
                    trulyMissingMods.Add(mod);
                }
            }

            // Show only truly missing mods
            if (trulyMissingMods.Count > 0)
            {
                ModCompatibilityDialogs.DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.MISSING_MODS_SECTION, trulyMissingMods.ToArray(), Color.red, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL);
                GUILayout.Space(10);
            }

            // Show installed but disabled mods separately
            if (installedButDisabledMods.Count > 0)
            {
                ModCompatibilityDialogs.DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.DISABLED_MODS_SECTION, installedButDisabledMods.ToArray(), Color.yellow, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ENABLE);
                GUILayout.Space(10);
            }
        }

        /// <summary>
        /// Draws the all mods enabled message
        /// </summary>
        private void DrawAllModsEnabledMessage()
        {
            GUIStyle restartStyle = new GUIStyle(GUI.skin.label);
            restartStyle.fontSize = 16;
            restartStyle.fontStyle = FontStyle.Bold;
            restartStyle.wordWrap = true;
            restartStyle.alignment = TextAnchor.MiddleCenter;
            restartStyle.normal.textColor = Color.green;

            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ALL_MODS_ENABLED, restartStyle);
            GUILayout.Space(10);

            GUIStyle restartInstructionStyle = new GUIStyle(GUI.skin.label);
            restartInstructionStyle.fontSize = 14;
            restartInstructionStyle.fontStyle = FontStyle.Bold;
            restartInstructionStyle.wordWrap = true;
            restartInstructionStyle.alignment = TextAnchor.MiddleCenter;
            restartInstructionStyle.normal.textColor = Color.yellow;

            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.CLOSE_TO_RESTART, restartInstructionStyle);
            GUILayout.Space(10);
        }

        /// <summary>
        /// Draws the mods modified message
        /// </summary>
        private void DrawModsModifiedMessage()
        {
            GUIStyle modifiedStyle = new GUIStyle(GUI.skin.label);
            modifiedStyle.fontSize = 14;
            modifiedStyle.fontStyle = FontStyle.Bold;
            modifiedStyle.wordWrap = true;
            modifiedStyle.alignment = TextAnchor.MiddleCenter;
            modifiedStyle.normal.textColor = Color.cyan;

            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.MODS_ENABLED_CLOSE_TO_RESTART, modifiedStyle);
            GUILayout.Space(10);
        }

        /// <summary>
        /// Draws instruction text based on current state
        /// </summary>
        private void DrawInstructions()
        {
            GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
            instructionStyle.fontStyle = FontStyle.Italic;
            instructionStyle.wordWrap = true;
            instructionStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            if (dialogMissingMods.Length > 0 || dialogVersionMismatches.Length > 0)
            {
                GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_DISABLE_INSTRUCTION, instructionStyle);
            }
            else if (dialogExtraMods.Length > 0)
            {
                GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.CONNECTION_ALLOWED_INFO, instructionStyle);
            }
        }

        /// <summary>
        /// Checks if all required mods are enabled
        /// </summary>
        private bool AreAllModsEnabled()
        {
            return ModStateManager.AreAllModsEnabled(dialogMissingMods);
        }

        /// <summary>
        /// Creates a colored texture for UI elements
        /// </summary>
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}