using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ONI_MP.DebugTools;
using ONI_MP.Networking;

namespace ONI_MP.Menus
{
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

        // Restart notification fields
        private static bool showRestartNotification = false;
        private static float restartNotificationTime = 0f;
        private const float NOTIFICATION_DURATION = 5f; // Show for 5 seconds

        // Track if mods were enabled and need restart
        private static bool allModsEnabled = false;

        // Track if any mods were modified during this session
        private static bool modsWereModified = false;

        // Installation progress tracking
        private static bool isInstalling = false;
        private static float installProgress = 0f;
        private static string installStatusMessage = "";
        private static int totalModsToInstall = 0;
        private static int completedModInstalls = 0;

        public static void ShowIncompatibilityError(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches, ulong[] steamModIds)
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Showing compatibility dialog with IMGUI");
                DebugConsole.Log($"[ModCompatibilityGUI] Steam mod IDs for auto-install: {steamModIds?.Length ?? 0}");

                // Store dialog data
                dialogReason = reason ?? "";
                dialogMissingMods = missingMods ?? new string[0];
                dialogExtraMods = extraMods ?? new string[0];
                dialogVersionMismatches = versionMismatches ?? new string[0];
                dialogSteamModIds = steamModIds ?? new ulong[0];

                // Reset enabled state
                allModsEnabled = false;

                // Reset modification tracking
                modsWereModified = false;

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

                DebugConsole.Log("[ModCompatibilityGUI] Dialog enabled");

                // 🚀 AUTO-INSTALL: Automaticamente inicia a instalação de mods em falta
                if (missingMods != null && missingMods.Length > 0)
                {
                    DebugConsole.Log($"[ModCompatibilityGUI] 🚀 AUTO-INSTALL: Detectados {missingMods.Length} mods em falta - iniciando instalação automática...");

                    // Aguarda um frame para a UI aparecer primeiro, depois inicia instalação automática
                    if (instance != null)
                    {
                        StartCoroutine(instance, DelayedAutoInstall());
                    }
                }
                else
                {
                    DebugConsole.Log("[ModCompatibilityGUI] Nenhum mod em falta detectado - só mostrando tela informativa");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Failed to show dialog: {ex.Message}");
            }
        }

        public static void CloseDialog()
        {
            showDialog = false;

            // If mods were modified during this session, save and restart
            if (modsWereModified)
            {
                try
                {
                    var modManager = Global.Instance?.modManager;
                    if (modManager != null)
                    {
                        DebugConsole.Log("[ModCompatibilityGUI] Mods were modified. Saving and restarting game...");
                        SaveAndRestart(modManager);
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] Error saving/restarting on close: {ex.Message}");
                }
            }

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

                showRestartNotification = true;
                restartNotificationTime = Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Failed to show restart notification: {ex.Message}");
            }
        }

        void OnGUI()
        {
            // Draw restart notification if active
            if (showRestartNotification)
            {
                DrawRestartNotification();
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

        void DrawRestartNotification()
        {
            // Check if notification should still be visible
            float elapsed = Time.realtimeSinceStartup - restartNotificationTime;
            if (elapsed > NOTIFICATION_DURATION)
            {
                showRestartNotification = false;
                return;
            }

            // Calculate fade out for last second
            float alpha = 1f;
            if (elapsed > NOTIFICATION_DURATION - 1f)
            {
                alpha = NOTIFICATION_DURATION - elapsed; // Fade from 1 to 0 in last second
            }

            // Position at top center of screen
            float width = 500f;
            float height = 80f;
            float x = (Screen.width - width) / 2;
            float y = 50f;

            Rect notificationRect = new Rect(x, y, width, height);

            // Draw background
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.95f * alpha);
            GUI.DrawTexture(notificationRect, Texture2D.whiteTexture);

            // Draw border
            GUI.color = new Color(1f, 0.8f, 0f, alpha); // Yellow/orange border
            GUI.Box(notificationRect, "");

            GUI.color = new Color(1f, 1f, 1f, alpha);

            // Draw text
            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 14;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.wordWrap = true;
            textStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);

            GUI.Label(notificationRect, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.MODS_ENABLED_RESTART_NOTIFICATION, textStyle);

            GUI.color = Color.white;
        }

        void DrawInstallationProgress()
        {
            // Progress header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.wordWrap = true;
            headerStyle.normal.textColor = Color.cyan;

            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALLING, headerStyle);
            GUILayout.Space(10);

            // Status message
            if (!string.IsNullOrEmpty(installStatusMessage))
            {
                GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
                statusStyle.fontSize = 14;
                statusStyle.alignment = TextAnchor.MiddleCenter;
                statusStyle.wordWrap = true;
                statusStyle.normal.textColor = Color.white;

                GUILayout.Label(installStatusMessage, statusStyle);
                GUILayout.Space(5);
            }

            // Progress bar
            Rect progressRect = GUILayoutUtility.GetRect(250, 25);

            // Background
            GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            GUI.Box(progressRect, "");

            // Progress fill
            GUI.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            Rect fillRect = new Rect(
                progressRect.x + 2,
                progressRect.y + 2,
                (progressRect.width - 4) * installProgress,
                progressRect.height - 4
            );
            GUI.Box(fillRect, "");

            // Progress text
            GUI.color = Color.white;
            GUIStyle progressTextStyle = new GUIStyle(GUI.skin.label);
            progressTextStyle.alignment = TextAnchor.MiddleCenter;
            progressTextStyle.fontStyle = FontStyle.Bold;
            progressTextStyle.fontSize = 12;

            string progressText = totalModsToInstall > 0 ?
                $"{completedModInstalls}/{totalModsToInstall} ({(installProgress * 100):F0}%)" :
                $"{(installProgress * 100):F0}%";

            GUI.Label(progressRect, progressText, progressTextStyle);

            // Instructions
            GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
            instructionStyle.fontSize = 12;
            instructionStyle.alignment = TextAnchor.MiddleCenter;
            instructionStyle.wordWrap = true;
            instructionStyle.normal.textColor = Color.yellow;

            GUILayout.Space(10);
            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.PLEASE_WAIT, instructionStyle);

            GUI.color = Color.white;
        }

        void DrawDialogWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = isInstalling ? Color.cyan : Color.red;

            string headerText = isInstalling ?
                "🚀 AUTO-INSTALL EM PROGRESSO" :
                MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.TITLE;

            GUILayout.Label(headerText, headerStyle);
            GUILayout.Space(10);

            // Scroll area for content
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(280));

            // Installation progress section (highest priority)
            if (isInstalling)
            {
                DrawInstallationProgress();
                GUILayout.Space(10);
            }
            // Restart required message (if all mods were enabled)
            else if (allModsEnabled)
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
            // Show modification message (if mods were changed but not all enabled yet)
            else if (modsWereModified)
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
            if (!allModsEnabled && dialogMissingMods != null && dialogMissingMods.Length > 0)
            {
                // Filter only mods that are truly not installed/enabled
                var trulyMissingMods = new List<string>();
                var installedButDisabledMods = new List<string>();

                foreach (var mod in dialogMissingMods)
                {
                    if (IsModEnabled(mod))
                    {
                        // If enabled, not really "missing"
                        DebugConsole.Log($"[ModCompatibilityGUI] Mod {mod} is enabled, ignoring from missing list");
                        continue;
                    }
                    else if (IsModInstalled(mod))
                    {
                        installedButDisabledMods.Add(mod);
                    }
                    else
                    {
                        trulyMissingMods.Add(mod);
                    }
                }

                // Mostrar apenas mods realmente em falta
                if (trulyMissingMods.Count > 0)
                {
                    DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.MISSING_MODS_SECTION, trulyMissingMods.ToArray(), Color.red, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL);
                    GUILayout.Space(10);
                }

                // Mostrar mods instalados mas desabilitados separadamente
                if (installedButDisabledMods.Count > 0)
                {
                    DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.DISABLED_MODS_SECTION, installedButDisabledMods.ToArray(), Color.yellow, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ENABLE);
                    GUILayout.Space(10);
                }
            }

            // Extra mods section (only show if no missing mods - policy permissive)
            if (dialogExtraMods != null && dialogExtraMods.Length > 0 &&
                (dialogMissingMods == null || dialogMissingMods.Length == 0) &&
                (dialogVersionMismatches == null || dialogVersionMismatches.Length == 0))
            {
                DrawInfoSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.EXTRA_MODS_INFO, dialogExtraMods);
                GUILayout.Space(10);
            }
            else if (dialogExtraMods != null && dialogExtraMods.Length > 0)
            {
                DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.EXTRA_MODS_SECTION, dialogExtraMods, Color.yellow, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.VIEW);
                GUILayout.Space(10);
            }

            // Version mismatches section
            if (dialogVersionMismatches != null && dialogVersionMismatches.Length > 0)
            {
                DrawModSection(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.VERSION_MISMATCH_SECTION, dialogVersionMismatches, Color.cyan, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.UPDATE);
                GUILayout.Space(10);
            }

            // Instructions
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

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Action buttons section (hide if all mods enabled)
            GUILayout.BeginHorizontal();

            if (!allModsEnabled)
            {
                // Check if there are truly missing mods or just disabled ones
                bool hasTrulyMissing = false;
                bool hasDisabled = false;

                if (dialogMissingMods != null && dialogMissingMods.Length > 0)
                {
                    foreach (var mod in dialogMissingMods)
                    {
                        if (IsModEnabled(mod))
                        {
                            // Mod is enabled, ignore
                            continue;
                        }
                        else if (IsModInstalled(mod))
                        {
                            hasDisabled = true;
                        }
                        else
                        {
                            hasTrulyMissing = true;
                        }
                    }
                }

                // Install All button (only for truly missing mods)
                if (hasTrulyMissing)
                {
                    GUIStyle installAllStyle = new GUIStyle(GUI.skin.button);
                    installAllStyle.fontSize = 12;
                    installAllStyle.fontStyle = FontStyle.Bold;
                    installAllStyle.normal.textColor = Color.cyan;

                    if (GUILayout.Button(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_ALL, installAllStyle, GUILayout.Height(35)))
                    {
                        InstallAllMods();
                    }

                    GUILayout.Space(10);
                }

                // Enable All button (only for installed but disabled mods)
                if (hasDisabled)
                {
                    GUIStyle enableAllStyle = new GUIStyle(GUI.skin.button);
                    enableAllStyle.fontSize = 12;
                    enableAllStyle.fontStyle = FontStyle.Bold;
                    enableAllStyle.normal.textColor = Color.green;

                    if (GUILayout.Button(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ENABLE_ALL, enableAllStyle, GUILayout.Height(35)))
                    {
                        EnableAllMods();
                    }

                    GUILayout.Space(10);
                }

                // Note: Removed complex auto-installation system
                // Now using simple native ONI mod management instead
            } // End of !allModsEnabled check

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

        void DrawModSection(string title, string[] mods, Color color, string buttonText)
        {
            // Section title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = color;

            GUILayout.Label(title, titleStyle);

            // Mod entries
            foreach (var mod in mods)
            {
                GUILayout.BeginHorizontal();

                // Mod name
                GUIStyle modStyle = new GUIStyle(GUI.skin.label);
                modStyle.normal.textColor = color;
                modStyle.wordWrap = true;

                GUILayout.Label($"• {mod}", modStyle);

                GUILayout.FlexibleSpace();

                // Check if mod is installed but disabled (for missing mods)
                bool isInstalled = IsModInstalled(mod);
                bool isDisabled = isInstalled && !IsModEnabled(mod);

                if (isDisabled && title.Contains("MISSING"))
                {
                    // Enable button for disabled mods
                    GUIStyle enableButtonStyle = new GUIStyle(GUI.skin.button);
                    enableButtonStyle.fontSize = 9;
                    enableButtonStyle.normal.textColor = Color.green;

                    if (GUILayout.Button(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ENABLE, enableButtonStyle, GUILayout.Width(55), GUILayout.Height(20)))
                    {
                        EnableMod(mod);
                    }

                    GUILayout.Space(5);
                }

                // Original action button
                GUIStyle modButtonStyle = new GUIStyle(GUI.skin.button);
                modButtonStyle.fontSize = 10;

                if (GUILayout.Button(buttonText, modButtonStyle, GUILayout.Width(70), GUILayout.Height(20)))
                {
                    HandleModAction(mod, buttonText);
                }

                GUILayout.EndHorizontal();
            }
        }

        void DrawInfoSection(string title, string[] mods)
        {
            // Info title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.green;

            GUILayout.Label(title, titleStyle);

            // Mod entries
            foreach (var mod in mods)
            {
                GUIStyle modStyle = new GUIStyle(GUI.skin.label);
                modStyle.normal.textColor = Color.green;
                modStyle.wordWrap = true;

                GUILayout.Label($"• {mod}", modStyle);
            }
        }

        private void OpenSteamWorkshopPage(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={modId}";

                DebugConsole.Log($"[ModCompatibilityGUI] Opening Steam Workshop: {url}");

                if (SteamManager.Initialized)
                {
                    Steamworks.SteamFriends.ActivateGameOverlayToWebPage(url);
                }
                else
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Failed to open Steam page: {ex.Message}");
            }
        }

        private string ExtractModId(string modDisplayName)
        {
            if (modDisplayName.Contains(" - "))
            {
                string[] parts = modDisplayName.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string lastPart = parts[parts.Length - 1];
                    if (System.Text.RegularExpressions.Regex.IsMatch(lastPart, @"^\d+$"))
                    {
                        return lastPart;
                    }
                }
            }

            var match = System.Text.RegularExpressions.Regex.Match(modDisplayName, @"\d+");
            return match.Success ? match.Value : modDisplayName;
        }

        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        // Mod activation functionality
        private bool HasDisabledMods()
        {
            if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                return false;

            try
            {
                foreach (var mod in dialogMissingMods)
                {
                    if (IsModInstalled(mod) && !IsModEnabled(mod))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking disabled mods: {ex.Message}");
            }

            return false;
        }

        private bool IsModInstalled(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;
                if (modManager == null) return false;

                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null)
                    {
                        // Check multiple ID formats for Steam mods
                        string defaultId = mod.label.defaultStaticID;
                        string labelId = mod.label.id;

                        // Try exact match with extracted ID
                        if (defaultId == modId || labelId == modId)
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Found installed mod: {modDisplayName} -> {defaultId}");
                            return true;
                        }

                        // Try exact match with original display name
                        if (defaultId == modDisplayName || labelId == modDisplayName)
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Found installed mod: {modDisplayName} -> {defaultId}");
                            return true;
                        }

                        // For Steam mods, try numeric ID match
                        if (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId)))
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Found installed Steam mod: {modDisplayName} -> {defaultId}");
                            return true;
                        }
                    }
                }

                DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod not found: {modDisplayName} (extracted: {modId})");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking if mod is installed: {ex.Message}");
            }

            return false;
        }

        private bool IsModEnabled(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;
                if (modManager == null) return false;

                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null)
                    {
                        // Check multiple ID formats for Steam mods
                        string defaultId = mod.label.defaultStaticID;
                        string labelId = mod.label.id;

                        // Try exact match with extracted ID
                        if (defaultId == modId || labelId == modId)
                        {
                            return mod.IsEnabledForActiveDlc();
                        }

                        // Try exact match with original display name
                        if (defaultId == modDisplayName || labelId == modDisplayName)
                        {
                            return mod.IsEnabledForActiveDlc();
                        }

                        // For Steam mods, try numeric ID match
                        if (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId)))
                        {
                            return mod.IsEnabledForActiveDlc();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking if mod is enabled: {ex.Message}");
            }

            return false;
        }

        private void CheckAllModsEnabled()
        {
            try
            {
                if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                {
                    allModsEnabled = false;
                    return;
                }

                // Check if all missing mods are now enabled
                bool allEnabled = true;
                foreach (var mod in dialogMissingMods)
                {
                    if (!IsModEnabled(mod))
                    {
                        allEnabled = false;
                        break;
                    }
                }

                if (allEnabled)
                {
                    allModsEnabled = true;
                    DebugConsole.Log("[ModCompatibilityGUI] All required mods are now enabled! Restart required.");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking all mods enabled: {ex.Message}");
            }
        }

        private void DisableMod(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;

                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] ModManager not available for disable");
                    return;
                }

                // Search for the mod using robust ID matching for Steam mods
                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null)
                    {
                        string defaultId = mod.label.defaultStaticID;
                        string labelId = mod.label.id;
                        bool foundMod = false;

                        // Check multiple ID formats for Steam mods
                        if (defaultId == modId || labelId == modId ||
                            defaultId == modDisplayName || labelId == modDisplayName ||
                            (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId))))
                        {
                            foundMod = true;
                            DebugConsole.Log($"[ModCompatibilityGUI] Found mod to disable: {modDisplayName} -> {defaultId}");
                        }

                        if (foundMod)
                        {
                            // Check if already disabled
                            if (!mod.IsEnabledForActiveDlc())
                            {
                                DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} was already disabled");
                                return;
                            }

                            try
                            {
                                // Disable the mod using proper ONI API
                                mod.SetEnabledForActiveDlc(false);

                                DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} disabled successfully!");

                                // Mark that mods were modified (restart will happen when closing dialog)
                                modsWereModified = true;

                                return;
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error disabling mod {modDisplayName}: {ex.Message}");
                                return;
                            }
                        }
                    }
                }

                DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod {modDisplayName} not found for disable");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in DisableMod: {ex.Message}");
            }
        }

        private void EnableMod(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;

                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] ModManager not available");
                    OpenSteamWorkshopPage(modDisplayName);
                    return;
                }

                // Search for the mod using robust ID matching for Steam mods
                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null)
                    {
                        string defaultId = mod.label.defaultStaticID;
                        string labelId = mod.label.id;
                        bool foundMod = false;

                        // Check multiple ID formats for Steam mods
                        if (defaultId == modId || labelId == modId ||
                            defaultId == modDisplayName || labelId == modDisplayName ||
                            (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId))))
                        {
                            foundMod = true;
                            DebugConsole.Log($"[ModCompatibilityGUI] Found mod to enable: {modDisplayName} -> {defaultId}");
                        }

                        if (foundMod)
                        {
                            // Check if already enabled using proper ONI method
                            if (mod.IsEnabledForActiveDlc())
                            {
                                DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} was already enabled");
                                CheckAllModsEnabled();
                                return;
                            }

                            // Check if mod is compatible
                            if (mod.available_content == 0)
                            {
                                DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod {modDisplayName} is not compatible - opening Steam page");
                                OpenSteamWorkshopPage(modDisplayName);
                                return;
                            }

                            try
                            {
                                // Enable the mod using proper ONI API (follows SaveGameModLoader pattern)
                                mod.SetEnabledForActiveDlc(true);

                                DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} enabled successfully!");

                                // Mark that mods were modified (restart will happen when closing dialog)
                                modsWereModified = true;

                                // Check if all mods are now enabled
                                CheckAllModsEnabled();

                                return;
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error enabling mod {modDisplayName}: {ex.Message}");
                                OpenSteamWorkshopPage(modDisplayName);
                                return;
                            }
                        }
                    }
                }

                DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod {modDisplayName} not found in list");
                OpenSteamWorkshopPage(modDisplayName);
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in EnableMod: {ex.Message}");
                OpenSteamWorkshopPage(modDisplayName);
            }
        }

        private void HandleModAction(string modDisplayName, string buttonText)
        {
            try
            {
                if (buttonText == "Enable")
                {
                    // Enable installed but disabled mod using native ONI system
                    DebugConsole.Log($"[ModCompatibilityGUI] Attempting to enable mod: {modDisplayName}");
                    EnableMod(modDisplayName);
                }
                else if (buttonText == "Install")
                {
                    // Try auto-installation first, fallback to Steam Workshop page if it fails
                    DebugConsole.Log($"[ModCompatibilityGUI] Attempting to auto-install mod: {modDisplayName}");
                    InstallSingleMod(modDisplayName);
                }
                else
                {
                    // Other buttons (View, Update) - open Steam Workshop page
                    DebugConsole.Log($"[ModCompatibilityGUI] Opening Steam Workshop for mod: {modDisplayName}");
                    OpenSteamWorkshopPage(modDisplayName);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error processing mod action {modDisplayName}: {ex.Message}");
                // Fallback to open Steam Workshop page
                OpenSteamWorkshopPage(modDisplayName);
            }
        }

        private void InstallSingleMod(string modDisplayName)
        {
            try
            {
                // Check if Steam is initialized
                if (!SteamManager.Initialized)
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] Steam not initialized - opening workshop page for: {modDisplayName}");
                    OpenSteamWorkshopPage(modDisplayName);
                    return;
                }

                // Extract and validate mod ID
                string modId = ExtractModId(modDisplayName);
                DebugConsole.Log($"[ModCompatibilityGUI] Extracted mod ID: '{modId}' from '{modDisplayName}'");

                if (string.IsNullOrEmpty(modId) || !ulong.TryParse(modId, out ulong testId))
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] Invalid mod ID '{modId}' - opening workshop page for: {modDisplayName}");
                    OpenSteamWorkshopPage(modDisplayName);
                    return;
                }

                DebugConsole.Log($"[ModCompatibilityGUI] Attempting auto-installation of mod {modId}");

                // Initialize progress for single mod installation
                StartInstallationProgress(1, string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALLING_SINGLE, modDisplayName));

                // Use WorkshopInstaller for auto-installation
                WorkshopInstaller.Instance.InstallWorkshopItem(
                    modId,
                    onReady: path => {
                        UpdateInstallationProgress(1, 1, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ACTIVATING_MOD);
                        DebugConsole.Log($"[ModCompatibilityGUI] Successfully installed mod {modId} to: {path}");

                        // Try to activate the mod automatically
                        if (WorkshopInstaller.Instance.ActivateInstalledMod(modId, path))
                        {
                            modsWereModified = true;
                            CompleteInstallationProgress(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_SUCCESS_SINGLE, modDisplayName));
                            DebugConsole.Log($"[ModCompatibilityGUI] Mod {modId} activated successfully!");
                        }
                        else
                        {
                            CompleteInstallationProgress(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_PARTIAL_SUCCESS_SINGLE, modDisplayName));
                            DebugConsole.Log($"[ModCompatibilityGUI] Mod {modId} installed but may need manual activation or game restart");
                        }
                    },
                    onError: error => {
                        CompleteInstallationProgress(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.FAILED_INSTALL_ERROR, modDisplayName, error));
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Auto-installation failed for mod {modId}: {error}");
                        DebugConsole.Log($"[ModCompatibilityGUI] Falling back to Steam Workshop page for: {modDisplayName}");
                        OpenSteamWorkshopPage(modDisplayName);
                    }
                );
            }
            catch (Exception ex)
            {
                CompleteInstallationProgress(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_FAILED_SINGLE, modDisplayName, ex.Message));
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Exception in InstallSingleMod for {modDisplayName}: {ex.Message}");
                OpenSteamWorkshopPage(modDisplayName);
            }
        }

        private void InstallAllMods()
        {
            try
            {
                if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] No missing mods to install");
                    return;
                }

                DebugConsole.Log($"[ModCompatibilityGUI] Starting installation of {dialogMissingMods.Length} mods...");

                // Check if Steam is initialized
                if (!SteamManager.Initialized)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] Steam not initialized - cannot auto-install mods");
                    // Fallback: open first Steam Workshop page
                    if (dialogMissingMods.Length > 0)
                    {
                        OpenSteamWorkshopPage(dialogMissingMods[0]);
                    }
                    return;
                }

                // Debug: Log all missing mods before processing
                DebugConsole.Log("[ModCompatibilityGUI] Missing mods list:");
                for (int i = 0; i < dialogMissingMods.Length; i++)
                {
                    DebugConsole.Log($"  [{i}] {dialogMissingMods[i]}");
                }

                // Extract mod IDs with better debugging and create mapping
                List<string> modIds = new List<string>();
                Dictionary<string, string> modIdToName = new Dictionary<string, string>();

                foreach (var mod in dialogMissingMods)
                {
                    string modId = ExtractModId(mod);
                    DebugConsole.Log($"[ModCompatibilityGUI] Extracted ID '{modId}' from '{mod}'");

                    if (!string.IsNullOrEmpty(modId) && ulong.TryParse(modId, out ulong testId))
                    {
                        modIds.Add(modId);
                        modIdToName[modId] = mod; // Mapeia ID para nome completo
                        DebugConsole.Log($"[ModCompatibilityGUI] Valid Steam Workshop ID: {modId}");
                    }
                    else
                    {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Invalid or non-numeric mod ID: '{modId}' from '{mod}'");
                    }
                }

                if (modIds.Count == 0)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] No valid Steam Workshop mod IDs found - opening workshop page instead");
                    // Fallback: open first Steam Workshop page
                    if (dialogMissingMods.Length > 0)
                    {
                        OpenSteamWorkshopPage(dialogMissingMods[0]);
                    }
                    return;
                }

                DebugConsole.Log($"[ModCompatibilityGUI] Proceeding with {modIds.Count} valid mod IDs");

                // Initialize progress tracking
                string autoInstallMsg = $"🚀 AUTO-INSTALL: {MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.PREPARING_INSTALL}";
                StartInstallationProgress(modIds.Count, autoInstallMsg);

                // Use WorkshopInstaller to install all with mod name mapping
                WorkshopInstaller.Instance.InstallMultipleItems(
                    modIds.ToArray(),
                    modIdToName,
                    onProgress: (completed, total, statusMessage) => {
                        // Mostra status específico de cada mod durante a instalação
                        UpdateInstallationProgress(completed, total, $"🚀 AUTO-INSTALL: {statusMessage}");
                        DebugConsole.Log($"[ModCompatibilityGUI] {statusMessage} ({completed}/{total})");
                    },
                    onComplete: installedPaths => {
                        UpdateInstallationProgress(modIds.Count, modIds.Count, MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ACTIVATING_MODS);
                        DebugConsole.Log($"[ModCompatibilityGUI] Batch installation completed! {installedPaths.Length} mods processed");

                        // Try to activate all installed mods
                        int activatedCount = 0;
                        for (int i = 0; i < modIds.Count && i < installedPaths.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(installedPaths[i]))
                            {
                                DebugConsole.Log($"[ModCompatibilityGUI] Attempting to activate mod {modIds[i]} from {installedPaths[i]}");
                                if (WorkshopInstaller.Instance.ActivateInstalledMod(modIds[i], installedPaths[i]))
                                {
                                    activatedCount++;
                                }
                            }
                            else
                            {
                                DebugConsole.LogWarning($"[ModCompatibilityGUI] No install path for mod {modIds[i]}");
                            }
                        }

                        DebugConsole.Log($"[ModCompatibilityGUI] {activatedCount} mods activated automatically out of {modIds.Count}");

                        if (activatedCount > 0)
                        {
                            modsWereModified = true;
                            DebugConsole.Log("[ModCompatibilityGUI] Mods were successfully installed and activated!");
                            CompleteInstallationProgress(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_SUCCESS, activatedCount));
                        }
                        else if (installedPaths.Length > 0)
                        {
                            CompleteInstallationProgress(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_PARTIAL_SUCCESS, installedPaths.Length));
                        }
                        else
                        {
                            CompleteInstallationProgress(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_NO_MODS_PROCESSED);
                        }
                    },
                    onError: error => {
                        CompleteInstallationProgress(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_FAILED_GENERIC, error));
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in batch installation: {error}");

                        // Fallback: open first Steam Workshop page
                        if (dialogMissingMods.Length > 0)
                        {
                            DebugConsole.Log("[ModCompatibilityGUI] Falling back to opening Steam Workshop page");
                            OpenSteamWorkshopPage(dialogMissingMods[0]);
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                CompleteInstallationProgress(string.Format(MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALL_FAILED_GENERIC, ex.Message));
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in InstallAllMods: {ex.Message}");
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Stack trace: {ex.StackTrace}");

                // Fallback on any exception
                if (dialogMissingMods != null && dialogMissingMods.Length > 0)
                {
                    OpenSteamWorkshopPage(dialogMissingMods[0]);
                }
            }
        }

        private void EnableAllMods()
        {
            try
            {
                if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                    return;

                var modManager = Global.Instance?.modManager;
                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] ModManager not available");
                    // Fallback to open Steam Workshop
                    if (dialogMissingMods.Length > 0)
                    {
                        OpenSteamWorkshopPage(dialogMissingMods[0]);
                    }
                    return;
                }

                int enabledCount = 0;
                int notFoundCount = 0;

                foreach (var modDisplayName in dialogMissingMods)
                {
                    if (IsModInstalled(modDisplayName) && !IsModEnabled(modDisplayName))
                    {
                        string modId = ExtractModId(modDisplayName);
                        bool modFound = false;

                        foreach (var mod in modManager.mods)
                        {
                            if (mod?.label != null)
                            {
                                string defaultId = mod.label.defaultStaticID;
                                string labelId = mod.label.id;

                                // Use robust Steam mod matching (same as other functions)
                                if (defaultId == modId || labelId == modId ||
                                    defaultId == modDisplayName || labelId == modDisplayName ||
                                    (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId))))
                                {
                                    try
                                    {
                                        // Check if mod is compatible
                                        if (mod.available_content == 0)
                                        {
                                            DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod {modDisplayName} is not compatible - skipping");
                                            modFound = true;
                                            break;
                                        }

                                        // Enable using proper ONI API (follows SaveGameModLoader pattern)
                                        mod.SetEnabledForActiveDlc(true);
                                        enabledCount++;
                                        modFound = true;
                                        DebugConsole.Log($"[ModCompatibilityGUI] Enabled: {modDisplayName} -> {defaultId}");
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Error enabling {modDisplayName}: {ex.Message}");
                                    }
                                }
                            }
                        }

                        if (!modFound)
                        {
                            notFoundCount++;
                            DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod not found: {modDisplayName}");
                        }
                    }
                }

                if (enabledCount > 0)
                {
                    try
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] {enabledCount} mods enabled successfully!");

                        if (notFoundCount > 0)
                        {
                            DebugConsole.LogWarning($"[ModCompatibilityGUI] {notFoundCount} mods were not found in the list");
                        }

                        // Mark that mods were modified (restart will happen when closing dialog)
                        modsWereModified = true;

                        // Check if all mods are now enabled
                        CheckAllModsEnabled();
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Error enabling mods: {ex.Message}");
                    }
                }
                else
                {
                    DebugConsole.Log("[ModCompatibilityGUI] No disabled mods found to enable");

                    // If none could be enabled, open Steam Workshop as fallback
                    if (dialogMissingMods.Length > 0)
                    {
                        DebugConsole.Log("[ModCompatibilityGUI] Opening Steam Workshop as fallback");
                        OpenSteamWorkshopPage(dialogMissingMods[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in EnableAllMods: {ex.Message}");
            }
        }

        private void InstallAndSyncMods()
        {
            try
            {
                DebugConsole.Log($"[ModCompatibilityGUI] Starting Install & Sync for {dialogSteamModIds.Length} Steam mods...");

                // Converte Steam IDs para array de strings
                string[] steamModIdStrings = new string[dialogSteamModIds.Length];
                for (int i = 0; i < dialogSteamModIds.Length; i++)
                {
                    steamModIdStrings[i] = dialogSteamModIds[i].ToString();
                }

                // Usa WorkshopInstaller para instalar e ativar mods automaticamente
                WorkshopInstaller.Instance.InstallMultipleItems(
                    steamModIdStrings,
                    onProgress: (completed, total, statusMessage) =>
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] Progresso da instalação: {statusMessage} ({completed}/{total})");
                    },
                    onComplete: installedPaths =>
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] Todos os {installedPaths.Length} mods foram instalados!");

                        // Agora ativa os mods instalados
                        foreach (var steamIdString in steamModIdStrings)
                        {
                            WorkshopInstaller.Instance.ActivateInstalledMod(steamIdString, "");
                        }

                        // Ajusta mods locais (habilita requeridos, desabilita extras)
                        AdjustLocalMods();

                        // Fecha o diálogo
                        CloseDialog();

                        // Reinicia o jogo
                        DebugConsole.Log("[ModCompatibilityGUI] Reiniciando o jogo para aplicar mudanças de mods...");
                        App.instance.Restart();
                    },
                    onError: error =>
                    {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro durante instalação: {error}");
                        DebugConsole.LogWarning("[ModCompatibilityGUI] Continuando com ajuste de mods locais...");

                        // Mesmo com erro, tenta ajustar mods locais
                        AdjustLocalMods();
                        CloseDialog();
                        App.instance.Restart();
                    }
                );
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro em InstallAndSyncMods: {ex.Message}");
            }
        }

        private void AdjustLocalMods()
        {
            var modManager = Global.Instance?.modManager;
            if (modManager == null)
            {
                DebugConsole.LogWarning("[ModCompatibilityGUI] ModManager não disponível");
                return;
            }

            try
            {
                // Constrói conjunto de IDs de mods requeridos
                var requiredModIds = new HashSet<string>();
                if (dialogMissingMods != null)
                {
                    foreach (var mod in dialogMissingMods)
                    {
                        string modId = ExtractModId(mod);
                        if (!string.IsNullOrEmpty(modId))
                        {
                            requiredModIds.Add(modId);
                        }
                    }
                }

                // Habilita/desabilita mods conforme necessário
                int enabledCount = 0;
                int disabledCount = 0;
                foreach (var mod in modManager.mods)
                {
                    if (mod?.label == null) continue;

                    bool shouldBeEnabled = requiredModIds.Contains(mod.label.id);
                    bool isEnabled = mod.IsEnabledForActiveDlc();

                    if (shouldBeEnabled != isEnabled)
                    {
                        mod.SetEnabledForActiveDlc(shouldBeEnabled);
                        if (shouldBeEnabled)
                        {
                            enabledCount++;
                            DebugConsole.Log($"[ModCompatibilityGUI] Habilitado: {mod.label.title}");
                        }
                        else if (dialogExtraMods != null && dialogExtraMods.Any(e => ExtractModId(e) == mod.label.id))
                        {
                            disabledCount++;
                            DebugConsole.Log($"[ModCompatibilityGUI] Desabilitado: {mod.label.title}");
                        }
                    }
                }

                DebugConsole.Log($"[ModCompatibilityGUI] Mods ajustados: {enabledCount} habilitados, {disabledCount} desabilitados");

                // Salva configuração de mods
                modManager.Save();
                DebugConsole.Log("[ModCompatibilityGUI] Configuração de mods salva");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro ao ajustar mods: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves mod configuration and triggers automatic game restart
        /// Based on SaveGameModLoader.AutoRestart() pattern
        /// </summary>
        private static void SaveAndRestart(KMod.Manager modManager)
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Saving mod configuration and triggering restart...");

                // Save mod configuration - this should trigger automatic restart
                modManager.Save();

                // Additional restart trigger if needed (following SaveGameModLoader pattern)
                try
                {
                    // Force restart via App.instance if modManager.Save() doesn't work
                    if (App.instance != null)
                    {
                        DebugConsole.Log("[ModCompatibilityGUI] Triggering manual restart via App.instance...");
                        App.instance.Restart();
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] Manual restart failed: {ex.Message}");
                    DebugConsole.Log("[ModCompatibilityGUI] Please restart the game manually to apply mod changes.");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in SaveAndRestart: {ex.Message}");
                DebugConsole.Log("[ModCompatibilityGUI] Please restart the game manually to apply mod changes.");
            }
        }

        // ==================== Installation progress functions ====================

        private static void StartInstallationProgress(int totalMods, string statusMessage)
        {
            isInstalling = true;
            installProgress = 0f;
            installStatusMessage = statusMessage;
            totalModsToInstall = totalMods;
            completedModInstalls = 0;

            DebugConsole.Log($"[ModCompatibilityGUI] Started installation progress: {totalMods} mods, status: {statusMessage}");
        }

        private static void UpdateInstallationProgress(int completed, int total, string statusMessage)
        {
            if (!isInstalling) return;

            completedModInstalls = completed;
            totalModsToInstall = total;
            installProgress = total > 0 ? (float)completed / total : 1f;
            installStatusMessage = statusMessage;

            DebugConsole.Log($"[ModCompatibilityGUI] Updated progress: {completed}/{total} ({installProgress * 100:F1}%) - {statusMessage}");
        }

        private static void CompleteInstallationProgress(string finalMessage)
        {
            if (!isInstalling) return;

            installProgress = 1f;
            installStatusMessage = finalMessage;

            DebugConsole.Log($"[ModCompatibilityGUI] Installation progress completed: {finalMessage}");

            // Hide progress after a delay to show completion message
            StartCoroutine(instance, HideProgressAfterDelay(3f)); // 3 seconds to show completion
        }

        private static System.Collections.IEnumerator HideProgressAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            isInstalling = false;
            installProgress = 0f;
            installStatusMessage = "";
            totalModsToInstall = 0;
            completedModInstalls = 0;

            DebugConsole.Log("[ModCompatibilityGUI] Installation progress hidden");
        }

        private static Coroutine StartCoroutine(MonoBehaviour behaviour, System.Collections.IEnumerator coroutine)
        {
            if (behaviour != null)
            {
                return behaviour.StartCoroutine(coroutine);
            }
            return null;
        }

        /// <summary>
        /// 🚀 AUTO-INSTALL: Corrotina que automaticamente processa mods em falta quando a tela aparece
        /// </summary>
        private static System.Collections.IEnumerator DelayedAutoInstall()
        {
            DebugConsole.Log("[ModCompatibilityGUI] 🚀 Iniciando processamento automático de mods em 2 segundos...");

            // Aguarda 2 segundos para a UI aparecer primeiro
            yield return new WaitForSeconds(2f);

            // Verifica se ainda há mods em falta (pode ter mudado)
            if (dialogMissingMods != null && dialogMissingMods.Length > 0)
            {
                DebugConsole.Log($"[ModCompatibilityGUI] 🚀 Processando {dialogMissingMods.Length} mods em falta...");

                // Separa mods em duas categorias: instalados mas desabilitados vs não instalados
                var modsToActivate = new List<string>();
                var modsToInstall = new List<string>();

                // Obtem instância para usar métodos de verificação de mod
                var guiInstance = instance;
                if (guiInstance == null)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] ❌ Instância GUI não disponível para verificar mods");
                    yield break;
                }

                foreach (var mod in dialogMissingMods)
                {
                    if (guiInstance.IsModInstalled(mod) && !guiInstance.IsModEnabled(mod))
                    {
                        modsToActivate.Add(mod);
                        DebugConsole.Log($"[ModCompatibilityGUI] ✅ Mod já instalado, só precisa ativar: {mod}");
                    }
                    else if (!guiInstance.IsModInstalled(mod))
                    {
                        modsToInstall.Add(mod);
                        DebugConsole.Log($"[ModCompatibilityGUI] 📥 Mod precisa ser instalado: {mod}");
                    }
                    else
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] ⚠️ Mod {mod} em estado indefinido - será processado como instalação");
                        modsToInstall.Add(mod);
                    }
                }

                // 🚫 VERIFICA MODS EXTRAS: Se host não aceita, desabilita automaticamente
                var modsToDisable = new List<string>();
                if (dialogExtraMods != null && dialogExtraMods.Length > 0)
                {
                    // Verifica se host permite mods extras
                    bool hostAllowsExtraMods = Configuration.Instance?.Host?.AllowExtraMods ?? true;

                    if (!hostAllowsExtraMods)
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] 🚫 Host NÃO permite mods extras - desabilitando {dialogExtraMods.Length} mods automaticamente");

                        foreach (var extraMod in dialogExtraMods)
                        {
                            // Verifica se mod está ativo usando funções estáticas corretas
                            if (guiInstance != null && guiInstance.IsModEnabled(extraMod))
                            {
                                modsToDisable.Add(extraMod);
                                DebugConsole.Log($"[ModCompatibilityGUI] 🚫 Mod extra será desabilitado: {extraMod}");
                            }
                        }
                    }
                    else
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] ✅ Host permite mods extras - mantendo {dialogExtraMods.Length} mods ativos");
                    }
                }

                DebugConsole.Log($"[ModCompatibilityGUI] 📊 Resumo: {modsToActivate.Count} para ativar, {modsToInstall.Count} para instalar, {modsToDisable.Count} para desabilitar");

                // Processa todas as fases sem try-catch para evitar problemas com yield
                var guiInstance2 = instance;
                if (guiInstance2 == null)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] ❌ Instância GUI não disponível");
                    yield break;
                }

                // 🚫 FASE 0: Desabilita mods extras se o host não aceita
                if (modsToDisable.Count > 0)
                {
                    DebugConsole.Log($"[ModCompatibilityGUI] 🚫 FASE 0: Desabilitando {modsToDisable.Count} mods extras não aceitos pelo host...");

                    StartInstallationProgress(modsToDisable.Count, "🚫 Desabilitando mods extras...");

                    for (int i = 0; i < modsToDisable.Count; i++)
                    {
                        string modName = modsToDisable[i];
                        UpdateInstallationProgress(i, modsToDisable.Count, $"🚫 Desabilitando {modName}...");

                        guiInstance2.DisableMod(modName);
                        yield return new WaitForSeconds(0.3f); // Pequena pausa entre desabilitações
                    }

                    CompleteInstallationProgress("🚫 Mods extras foram desabilitados!");
                    yield return new WaitForSeconds(1f); // Pausa para mostrar a conclusão
                }

                // 1️⃣ PRIMEIRO: Ativa mods que já estão instalados
                if (modsToActivate.Count > 0)
                {
                    DebugConsole.Log($"[ModCompatibilityGUI] 🔧 FASE 1: Ativando {modsToActivate.Count} mods já instalados...");

                    StartInstallationProgress(modsToActivate.Count, "🔧 Ativando mods já instalados...");

                    for (int i = 0; i < modsToActivate.Count; i++)
                    {
                        string modName = modsToActivate[i];
                        UpdateInstallationProgress(i, modsToActivate.Count, $"🔧 Ativando {modName}...");

                        guiInstance2.EnableMod(modName);
                        yield return new WaitForSeconds(0.3f); // Pequena pausa entre ativações
                    }

                    CompleteInstallationProgress("✅ Mods instalados foram ativados!");
                    yield return new WaitForSeconds(1f); // Pausa para mostrar a conclusão
                }

                // 2️⃣ SEGUNDO: Instala mods que não estão instalados
                if (modsToInstall.Count > 0)
                {
                    DebugConsole.Log($"[ModCompatibilityGUI] 📥 FASE 2: Instalando {modsToInstall.Count} mods não instalados...");

                    // Temporariamente substitui a lista para a função de instalação
                    var originalMissingMods = dialogMissingMods;
                    dialogMissingMods = modsToInstall.ToArray();

                    guiInstance2.InstallAllMods();

                    // Restaura a lista original
                    dialogMissingMods = originalMissingMods;
                }

                // 3️⃣ RESULTADO FINAL
                int totalProcessed = modsToDisable.Count + modsToActivate.Count + modsToInstall.Count;
                if (totalProcessed > 0)
                {
                    string summary = "";
                    if (modsToDisable.Count > 0) summary += $"{modsToDisable.Count} mods extras desabilitados, ";
                    if (modsToActivate.Count > 0) summary += $"{modsToActivate.Count} mods ativados, ";
                    if (modsToInstall.Count > 0) summary += $"{modsToInstall.Count} mods instalados, ";

                    summary = summary.TrimEnd(',', ' ');
                    DebugConsole.Log($"[ModCompatibilityGUI] ✅ Processamento completo: {summary}");
                }
                else
                {
                    DebugConsole.Log("[ModCompatibilityGUI] ✅ Nenhum processamento necessário - tudo já compatível!");
                }
            }
            else
            {
                DebugConsole.Log("[ModCompatibilityGUI] ❌ Nenhum mod em falta para processar");
            }
        }
    }
}