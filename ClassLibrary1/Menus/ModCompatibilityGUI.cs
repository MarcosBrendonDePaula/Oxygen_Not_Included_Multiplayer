using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using Steamworks;

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

        // Individual mod state tracking for dynamic buttons
        private static Dictionary<string, ModButtonState> modStates = new Dictionary<string, ModButtonState>();

        // Log throttling to reduce spam
        private static Dictionary<string, float> lastLogTime = new Dictionary<string, float>();
        private static HashSet<string> missingModsCache = new HashSet<string>();
        private static float lastCacheUpdate = 0f;
        private static readonly float LOG_THROTTLE_SECONDS = 5f; // Only log same message every 5 seconds
        private static readonly float CACHE_REFRESH_SECONDS = 10f; // Refresh cache every 10 seconds

        // Mod state verification cache to reduce excessive checks (3 second intervals)
        private static Dictionary<string, bool> modInstalledCache = new Dictionary<string, bool>();
        private static Dictionary<string, bool> modEnabledCache = new Dictionary<string, bool>();
        private static Dictionary<string, float> modCacheTime = new Dictionary<string, float>();
        private static readonly float MOD_CACHE_SECONDS = 3f; // Verify mod status only every 3 seconds

        // Enum for mod button states (Subscribe -> Progress -> Enable -> Disable)
        public enum ModButtonState
        {
            Subscribe,    // Mod not subscribed - "Subscribe to Mod X"
            Subscribing,  // During subscription - "Subscribing..."
            Enable,       // Mod subscribed but disabled - "Enable Mod X"
            Disable       // Mod enabled - "Disable Mod X"
        }

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

                // Reset all mod states for fresh dialog (ItsLuke feedback: dynamic buttons)
                ResetAllModStates();

                // Clear log throttling and mod verification cache for fresh session
                ClearLogThrottling();
                ClearModVerificationCache();

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

                // Note: Auto-installation removed based on ItsLuke feedback
                // Users must now explicitly click "Install All" or individual "Install" buttons
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

        public static void CloseDialog()
        {
            showDialog = false;

            // Clear log throttling and mod verification cache when closing
            ClearLogThrottling();
            ClearModVerificationCache();

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
            // Draw restart dialog if active (ItsLuke feedback: native restart prompt)
            if (showRestartDialog)
            {
                DrawRestartDialog();
                return; // Show only restart dialog when active
            }

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
                MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.INSTALLING :
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

                // Subscribe All button (only for truly missing mods)
                if (hasTrulyMissing)
                {
                    // Count truly missing mods for clear user feedback
                    int trulyMissingCount = 0;
                    foreach (var mod in dialogMissingMods)
                    {
                        if (!IsModEnabled(mod) && !IsModInstalled(mod))
                        {
                            trulyMissingCount++;
                        }
                    }

                    GUIStyle subscribeAllStyle = new GUIStyle(GUI.skin.button);
                    subscribeAllStyle.fontSize = 12;
                    subscribeAllStyle.fontStyle = FontStyle.Bold;
                    subscribeAllStyle.normal.textColor = Color.cyan;

                    // Show transparent button text with count
                    string subscribeAllText = $"Subscribe All ({trulyMissingCount} mods)";
                    if (GUILayout.Button(subscribeAllText, subscribeAllStyle, GUILayout.Height(35)))
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] User clicked Subscribe All - will subscribe to {trulyMissingCount} mods");
                        SubscribeAllMods();
                    }

                    GUILayout.Space(10);
                }

                // Enable All button (only for installed but disabled mods)
                if (hasDisabled)
                {
                    // Count disabled mods for clear user feedback
                    int disabledCount = 0;
                    foreach (var mod in dialogMissingMods)
                    {
                        if (IsModInstalled(mod) && !IsModEnabled(mod))
                        {
                            disabledCount++;
                        }
                    }

                    GUIStyle enableAllStyle = new GUIStyle(GUI.skin.button);
                    enableAllStyle.fontSize = 12;
                    enableAllStyle.fontStyle = FontStyle.Bold;
                    enableAllStyle.normal.textColor = Color.green;

                    // Show transparent button text with count
                    string enableAllText = $"{MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ENABLE_ALL} ({disabledCount} mods)";
                    if (GUILayout.Button(enableAllText, enableAllStyle, GUILayout.Height(35)))
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] User clicked Enable All - will enable {disabledCount} mods");
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

                // Dynamic button based on mod state (ItsLuke feedback: Install -> Progress -> Enable -> Disable)
                ModButtonState currentState = GetModButtonState(mod);
                string dynamicButtonText = GetModButtonText(mod);

                // Button color and style based on state
                GUIStyle modButtonStyle = new GUIStyle(GUI.skin.button);
                modButtonStyle.fontSize = 10;

                // Set button color based on state
                switch (currentState)
                {
                    case ModButtonState.Subscribe:
                        modButtonStyle.normal.textColor = Color.cyan;
                        break;
                    case ModButtonState.Subscribing:
                        modButtonStyle.normal.textColor = Color.yellow;
                        break;
                    case ModButtonState.Enable:
                        modButtonStyle.normal.textColor = Color.green;
                        break;
                    case ModButtonState.Disable:
                        modButtonStyle.normal.textColor = Color.red;
                        break;
                }

                // Disable button during subscription
                bool buttonEnabled = currentState != ModButtonState.Subscribing;
                bool wasEnabled = GUI.enabled;
                GUI.enabled = buttonEnabled;

                if (GUILayout.Button(dynamicButtonText, modButtonStyle, GUILayout.Width(90), GUILayout.Height(20)))
                {
                    HandleDynamicModAction(mod, currentState);
                }

                GUI.enabled = wasEnabled;

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
                // Check temporal cache first to reduce excessive checks
                float currentTime = Time.realtimeSinceStartup;
                string cacheKey = $"installed_{modDisplayName}";

                if (modCacheTime.ContainsKey(cacheKey) &&
                    (currentTime - modCacheTime[cacheKey]) < MOD_CACHE_SECONDS)
                {
                    // Return cached result if within time window
                    return modInstalledCache.ContainsKey(cacheKey) ? modInstalledCache[cacheKey] : false;
                }

                // Perform actual mod lookup
                bool isInstalled = CheckModInstalled(modDisplayName);

                // Update cache
                modInstalledCache[cacheKey] = isInstalled;
                modCacheTime[cacheKey] = currentTime;

                return isInstalled;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking if mod is installed: {ex.Message}");
                return false;
            }
        }

        private bool CheckModInstalled(string modDisplayName)
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
                            LogThrottled($"Found installed mod: {modDisplayName} -> {defaultId}", "mod_found");
                            return true;
                        }

                        // Try exact match with original display name
                        if (defaultId == modDisplayName || labelId == modDisplayName)
                        {
                            LogThrottled($"Found installed mod: {modDisplayName} -> {defaultId}", "mod_found");
                            return true;
                        }

                        // For Steam mods, try numeric ID match
                        if (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId)))
                        {
                            LogThrottled($"Found installed Steam mod: {modDisplayName} -> {defaultId}", "mod_found");
                            return true;
                        }
                    }
                }

                // Cache missing mod and use throttled logging
                UpdateMissingModsCache();
                missingModsCache.Add(modDisplayName);
                LogThrottled($"Mod not found: {modDisplayName} (extracted: {modId})", "missing_mods");
                return false;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in CheckModInstalled: {ex.Message}");
                return false;
            }
        }

        private bool IsModEnabled(string modDisplayName)
        {
            try
            {
                // Check temporal cache first to reduce excessive checks
                float currentTime = Time.realtimeSinceStartup;
                string cacheKey = $"enabled_{modDisplayName}";

                if (modCacheTime.ContainsKey(cacheKey) &&
                    (currentTime - modCacheTime[cacheKey]) < MOD_CACHE_SECONDS)
                {
                    // Return cached result if within time window
                    return modEnabledCache.ContainsKey(cacheKey) ? modEnabledCache[cacheKey] : false;
                }

                // Perform actual mod lookup
                bool isEnabled = CheckModEnabled(modDisplayName);

                // Update cache
                modEnabledCache[cacheKey] = isEnabled;
                modCacheTime[cacheKey] = currentTime;

                return isEnabled;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking if mod is enabled: {ex.Message}");
                return false;
            }
        }

        private bool CheckModEnabled(string modDisplayName)
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

                return false;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in CheckModEnabled: {ex.Message}");
                return false;
            }
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

        /// <summary>
        /// Handles mod actions based on dynamic button state (ItsLuke feedback: Install -> Progress -> Enable -> Disable)
        /// </summary>
        private void HandleDynamicModAction(string modDisplayName, ModButtonState currentState)
        {
            try
            {
                DebugConsole.Log($"[ModCompatibilityGUI] User clicked {currentState} for mod: {modDisplayName}");

                switch (currentState)
                {
                    case ModButtonState.Subscribe:
                        // Subscribe to mod and set to subscribing state
                        DebugConsole.Log($"[ModCompatibilityGUI] Starting subscription to mod: {modDisplayName}");
                        SetModSubscribing(modDisplayName);
                        SubscribeSingleMod(modDisplayName);
                        break;

                    case ModButtonState.Subscribing:
                        // Button should be disabled during subscription, this shouldn't happen
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Subscribe button clicked while subscribing: {modDisplayName}");
                        break;

                    case ModButtonState.Enable:
                        // Enable installed but disabled mod
                        DebugConsole.Log($"[ModCompatibilityGUI] Enabling mod: {modDisplayName}");
                        EnableMod(modDisplayName);
                        UpdateModStateAfterOperation(modDisplayName);

                        // Show restart prompt after enabling mod (ItsLuke feedback: native restart like in normal game)
                        if (modsWereModified)
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} enabled - showing restart prompt");
                            ShowNativeRestartPrompt();
                        }
                        break;

                    case ModButtonState.Disable:
                        // Disable enabled mod
                        DebugConsole.Log($"[ModCompatibilityGUI] Disabling mod: {modDisplayName}");
                        DisableMod(modDisplayName);
                        UpdateModStateAfterOperation(modDisplayName);

                        // Show restart prompt after disabling mod (ItsLuke feedback: native restart like in normal game)
                        if (modsWereModified)
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} disabled - showing restart prompt");
                            ShowNativeRestartPrompt();
                        }
                        break;

                    default:
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Unknown button state {currentState} for mod: {modDisplayName}");
                        OpenSteamWorkshopPage(modDisplayName);
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in HandleDynamicModAction for {modDisplayName}: {ex.Message}");

                // Reset mod state on error
                UpdateModStateAfterOperation(modDisplayName);

                // Fallback to open Steam Workshop page
                OpenSteamWorkshopPage(modDisplayName);
            }
        }

        // Legacy function kept for compatibility
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
                else if (buttonText == "Subscribe")
                {
                    // Try auto-subscription first, fallback to Steam Workshop page if it fails
                    DebugConsole.Log($"[ModCompatibilityGUI] Attempting to subscribe to mod: {modDisplayName}");
                    SubscribeSingleMod(modDisplayName);
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


        /// <summary>
        /// Subscribe to all missing mods (simplified approach - Steam handles installation)
        /// </summary>
        private void SubscribeAllMods()
        {
            try
            {
                if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] No missing mods to subscribe to");
                    return;
                }

                // Count truly missing mods (not just disabled)
                var trulyMissingMods = new List<string>();
                foreach (var mod in dialogMissingMods)
                {
                    if (!IsModEnabled(mod) && !IsModInstalled(mod))
                    {
                        trulyMissingMods.Add(mod);
                    }
                }

                if (trulyMissingMods.Count == 0)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] No truly missing mods found - all are already installed");
                    return;
                }

                DebugConsole.Log($"[ModCompatibilityGUI] Starting subscription to {trulyMissingMods.Count} mods...");

                // Check Steam initialization
                if (!SteamManager.Initialized)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] Steam not initialized - opening workshop pages");
                    OpenSteamWorkshopPage(trulyMissingMods[0]);
                    return;
                }

                // Start subscribing to each mod one by one
                StartCoroutine(SubscribeAllModsCoroutine(trulyMissingMods));
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in SubscribeAllMods: {ex.Message}");

                // Fallback on any exception
                if (dialogMissingMods != null && dialogMissingMods.Length > 0)
                {
                    OpenSteamWorkshopPage(dialogMissingMods[0]);
                }
            }
        }

        /// <summary>
        /// Coroutine to subscribe to all missing mods sequentially
        /// </summary>
        private System.Collections.IEnumerator SubscribeAllModsCoroutine(List<string> modsToSubscribe)
        {
            int totalMods = modsToSubscribe.Count;
            int completedMods = 0;
            List<string> successfulMods = new List<string>();
            List<string> failedMods = new List<string>();

            DebugConsole.Log($"[ModCompatibilityGUI] 🚀 Starting subscription to {totalMods} mods...");

            foreach (string modDisplayName in modsToSubscribe)
            {
                string modId = ExtractModId(modDisplayName);

                if (string.IsNullOrEmpty(modId) || !ulong.TryParse(modId, out ulong testId))
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] ❌ Invalid mod ID for: {modDisplayName}");
                    failedMods.Add(modDisplayName);
                    completedMods++;
                    continue;
                }

                DebugConsole.Log($"[ModCompatibilityGUI] 📝 Subscribing to mod {completedMods + 1}/{totalMods}: {modDisplayName}");

                // Set mod to subscribing state
                SetModSubscribing(modDisplayName);

                bool subscribeComplete = false;
                bool subscribeSuccess = false;

                // Use the simple subscribe function
                WorkshopInstaller.Instance.SubscribeToWorkshopItem(
                    modId,
                    onSuccess: subscribedModId => {
                        subscribeComplete = true;
                        subscribeSuccess = true;
                        DebugConsole.Log($"[ModCompatibilityGUI] ✅ Successfully subscribed to {modDisplayName}");
                        successfulMods.Add(modDisplayName);

                        // Start monitoring for this mod
                        StartSteamInstallationMonitoring(modId, modDisplayName);
                    },
                    onError: error => {
                        subscribeComplete = true;
                        subscribeSuccess = false;
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] ❌ Failed to subscribe to {modDisplayName}: {error}");
                        failedMods.Add(modDisplayName);
                    }
                );

                // Wait for subscription to complete (with timeout)
                float timeoutTime = Time.time + 30f;
                while (!subscribeComplete && Time.time < timeoutTime)
                {
                    yield return null;
                }

                if (!subscribeComplete)
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] ⏰ Timeout subscribing to {modDisplayName}");
                    failedMods.Add(modDisplayName);
                }

                // Update mod state
                UpdateModStateAfterOperation(modDisplayName);
                completedMods++;

                // Small pause between subscriptions to avoid spamming Steam
                yield return new WaitForSeconds(1f);
            }

            // Summary
            DebugConsole.Log($"[ModCompatibilityGUI] 📊 Subscription complete: {successfulMods.Count} successful, {failedMods.Count} failed");

            if (successfulMods.Count > 0)
            {
                DebugConsole.Log($"[ModCompatibilityGUI] ✅ Successfully subscribed to: {string.Join(", ", successfulMods)}");
                DebugConsole.Log($"[ModCompatibilityGUI] 👀 Steam will now install these mods automatically - monitoring...");
            }

            if (failedMods.Count > 0)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] ❌ Failed to subscribe to: {string.Join(", ", failedMods)}");
                // Could show a dialog or open workshop pages for failed mods
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

                // Use new SubscribeAllMods approach - simpler and more reliable
                DebugConsole.Log($"[ModCompatibilityGUI] 📝 Using new subscribe-only approach for {modIds.Count} mods");
                CompleteInstallationProgress("🚀 Subscribing to mods - Steam will handle installation automatically...");
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
                            // Use throttled logging for missing mods during enable operations
                            LogThrottled($"Mod not found during enable operation: {modDisplayName}", "enable_missing");
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

                        // Show restart prompt after enabling multiple mods (ItsLuke feedback: native restart like in normal game)
                        DebugConsole.Log($"[ModCompatibilityGUI] {enabledCount} mods enabled - showing restart prompt");
                        ShowNativeRestartPrompt();
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

                // Skip automatic Steam mod installation - user should manually subscribe via Steam or use Subscribe buttons
                DebugConsole.Log($"[ModCompatibilityGUI] 📝 Skipping Steam mod installation - adjusting local mods only");
                DebugConsole.Log("[ModCompatibilityGUI] 💡 For Steam mods, please use 'Subscribe' buttons or manually subscribe via Steam Workshop");

                // Ajusta mods locais (habilita requeridos, desabilita extras)
                AdjustLocalMods();

                // Fecha o diálogo
                CloseDialog();

                // Reinicia o jogo
                DebugConsole.Log("[ModCompatibilityGUI] Reiniciando o jogo para aplicar mudanças de mods...");
                App.instance.Restart();
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

        // ==================== Log Throttling System ====================

        /// <summary>
        /// Logs a message but throttles it to avoid spam
        /// </summary>
        private static void LogThrottled(string message, string category = "general")
        {
            float currentTime = Time.realtimeSinceStartup;
            string key = $"{category}:{message}";

            if (!lastLogTime.ContainsKey(key) || (currentTime - lastLogTime[key]) >= LOG_THROTTLE_SECONDS)
            {
                lastLogTime[key] = currentTime;
                DebugConsole.LogWarning($"[ModCompatibilityGUI] {message}");
            }
        }

        /// <summary>
        /// Updates the missing mods cache to reduce redundant checks
        /// </summary>
        private static void UpdateMissingModsCache()
        {
            float currentTime = Time.realtimeSinceStartup;

            if ((currentTime - lastCacheUpdate) >= CACHE_REFRESH_SECONDS)
            {
                // Log consolidated missing mods report if we have any
                if (missingModsCache.Count > 0)
                {
                    DebugConsole.Log($"[ModCompatibilityGUI] Summary: {missingModsCache.Count} mods not found in last {CACHE_REFRESH_SECONDS}s");
                }

                missingModsCache.Clear();
                lastCacheUpdate = currentTime;
            }
        }

        /// <summary>
        /// Clears all throttling caches (useful when dialog opens/closes)
        /// </summary>
        private static void ClearLogThrottling()
        {
            lastLogTime.Clear();
            missingModsCache.Clear();
            lastCacheUpdate = 0f;
        }

        /// <summary>
        /// Clears mod verification cache to ensure fresh checks
        /// </summary>
        private static void ClearModVerificationCache()
        {
            modInstalledCache.Clear();
            modEnabledCache.Clear();
            modCacheTime.Clear();
            DebugConsole.Log("[ModCompatibilityGUI] Mod verification cache cleared for fresh checks");
        }

        // ==================== Dynamic Button State System (ItsLuke feedback) ====================

        /// <summary>
        /// Gets the current state for a mod button based on mod status
        /// </summary>
        private ModButtonState GetModButtonState(string modDisplayName)
        {
            // Check if we have a manual state override (e.g., during installation)
            if (modStates.ContainsKey(modDisplayName))
            {
                return modStates[modDisplayName];
            }

            // Auto-determine state based on mod status
            if (IsModEnabled(modDisplayName))
            {
                return ModButtonState.Disable; // Mod is enabled - show "Disable"
            }
            else if (IsModInstalled(modDisplayName))
            {
                return ModButtonState.Enable;  // Mod installed but disabled - show "Enable"
            }
            else
            {
                return ModButtonState.Subscribe; // Mod not subscribed - show "Subscribe"
            }
        }

        /// <summary>
        /// Gets the button text for a mod based on its current state
        /// </summary>
        private string GetModButtonText(string modDisplayName)
        {
            ModButtonState state = GetModButtonState(modDisplayName);

            switch (state)
            {
                case ModButtonState.Subscribe:
                    return "Subscribe";

                case ModButtonState.Subscribing:
                    // Show subscribing status
                    return "Subscribing...";

                case ModButtonState.Enable:
                    return MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.ENABLE;

                case ModButtonState.Disable:
                    return MP_STRINGS.UI.MODCOMPATIBILITY.POPUP.DISABLE;

                default:
                    return "Subscribe";
            }
        }

        /// <summary>
        /// Sets a mod to subscribing state
        /// </summary>
        private static void SetModSubscribing(string modDisplayName)
        {
            modStates[modDisplayName] = ModButtonState.Subscribing;
            DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} state set to Subscribing");
        }

        /// <summary>
        /// Subscribe to a single mod (simplified approach - Steam handles installation)
        /// </summary>
        private void SubscribeSingleMod(string modDisplayName)
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
                DebugConsole.Log($"[ModCompatibilityGUI] 🔍 EXTRACTED MOD ID: '{modId}' from '{modDisplayName}'");

                if (string.IsNullOrEmpty(modId) || !ulong.TryParse(modId, out ulong testId))
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] ❌ INVALID MOD ID '{modId}' - opening workshop page for: {modDisplayName}");
                    OpenSteamWorkshopPage(modDisplayName);
                    return;
                }

                DebugConsole.Log($"[ModCompatibilityGUI] 🚀 CALLING WorkshopInstaller.SubscribeToWorkshopItem for mod {modId}");

                // Use new simplified subscribe function
                WorkshopInstaller.Instance.SubscribeToWorkshopItem(
                    modId,
                    onSuccess: subscribedModId => {
                        DebugConsole.Log($"[ModCompatibilityGUI] ✅ Successfully subscribed to mod {subscribedModId}");
                        DebugConsole.Log($"[ModCompatibilityGUI] 👀 Steam will now handle installation automatically - monitoring...");

                        // Update button state back to normal (let normal logic handle it)
                        UpdateModStateAfterOperation(modDisplayName);

                        // Start monitoring for Steam's automatic installation
                        StartSteamInstallationMonitoring(modId, modDisplayName);
                    },
                    onError: error => {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] ❌ Subscribe failed for mod {modId}: {error}");
                        DebugConsole.Log($"[ModCompatibilityGUI] 🌐 Opening Steam Workshop as fallback for: {modDisplayName}");

                        // Reset button state on error
                        UpdateModStateAfterOperation(modDisplayName);
                        OpenSteamWorkshopPage(modDisplayName);
                    }
                );
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Exception in SubscribeSingleMod for {modDisplayName}: {ex.Message}");

                // Reset button state on exception
                UpdateModStateAfterOperation(modDisplayName);
                OpenSteamWorkshopPage(modDisplayName);
            }
        }

        /// <summary>
        /// Starts monitoring Steam's automatic installation after subscription
        /// </summary>
        private void StartSteamInstallationMonitoring(string modId, string modDisplayName)
        {
            StartCoroutine(MonitorSteamInstallation(modId, modDisplayName));
        }

        /// <summary>
        /// Monitors Steam's automatic installation process
        /// </summary>
        private System.Collections.IEnumerator MonitorSteamInstallation(string modId, string modDisplayName)
        {
            if (!ulong.TryParse(modId, out ulong fileIdULong))
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] ❌ Invalid mod ID for monitoring: {modId}");
                yield break;
            }

            PublishedFileId_t fileId = new PublishedFileId_t(fileIdULong);
            DebugConsole.Log($"[ModCompatibilityGUI] 👀 Starting Steam installation monitoring for mod {modId}...");

            float timeoutTime = Time.time + 300f; // 5 minutes max monitoring
            float lastLogTime = Time.time;

            while (Time.time < timeoutTime)
            {
                try
                {
                    uint currentState = SteamUGC.GetItemState(fileId);
                    bool subscribed = (currentState & (uint)EItemState.k_EItemStateSubscribed) != 0;
                    bool installed = (currentState & (uint)EItemState.k_EItemStateInstalled) != 0;
                    bool downloading = (currentState & (uint)EItemState.k_EItemStateDownloading) != 0;
                    bool downloadPending = (currentState & (uint)EItemState.k_EItemStateDownloadPending) != 0;

                    // Log status every 30 seconds
                    if (Time.time - lastLogTime > 30f)
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] 📊 Steam status for mod {modId}:");
                        DebugConsole.Log($"[ModCompatibilityGUI]   • Subscribed: {subscribed} | Installed: {installed}");
                        DebugConsole.Log($"[ModCompatibilityGUI]   • Downloading: {downloading} | Pending: {downloadPending}");
                        lastLogTime = Time.time;
                    }

                    // Check if installation completed
                    if (subscribed && installed && !downloading && !downloadPending)
                    {
                        DebugConsole.Log($"[ModCompatibilityGUI] ✅ Steam completed installation of mod {modId}!");
                        DebugConsole.Log($"[ModCompatibilityGUI] 🔄 Attempting to activate mod in game...");

                        // Try to activate the mod using WorkshopInstaller's activation system
                        string installedPath = GetSteamModPath(fileId);
                        if (WorkshopInstaller.Instance.ActivateInstalledMod(modId, installedPath))
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] ✅ Mod {modDisplayName} successfully activated!");
                            modsWereModified = true;
                        }
                        else
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] ⚠️ Mod {modDisplayName} installed but activation pending - added to queue");
                        }

                        // Update UI state
                        UpdateModStateAfterOperation(modDisplayName);
                        yield break;
                    }

                    // If subscription was lost, stop monitoring
                    if (!subscribed)
                    {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] ⚠️ Lost subscription to mod {modId} - stopping monitoring");
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[ModCompatibilityGUI] Error monitoring mod {modId}: {ex.Message}");
                }

                yield return new WaitForSeconds(5f); // Check every 5 seconds
            }

            // Timeout reached
            DebugConsole.LogWarning($"[ModCompatibilityGUI] ⏰ Steam monitoring timeout for mod {modId} after 5 minutes");
            DebugConsole.Log($"[ModCompatibilityGUI] 💡 Steam may still be installing in background - check Steam Downloads");
        }

        /// <summary>
        /// Gets the installation path of a Steam mod
        /// </summary>
        private string GetSteamModPath(PublishedFileId_t fileId)
        {
            try
            {
                ulong sizeOnDisk;
                uint timeStamp;
                string folder;
                bool ok = SteamUGC.GetItemInstallInfo(fileId, out sizeOnDisk, out folder, 1024, out timeStamp);

                if (ok && !string.IsNullOrEmpty(folder))
                {
                    return folder;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error getting Steam mod path: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Updates a mod state after installation/operation completes
        /// </summary>
        private static void UpdateModStateAfterOperation(string modDisplayName)
        {
            // Remove manual override to auto-determine state
            if (modStates.ContainsKey(modDisplayName))
            {
                modStates.Remove(modDisplayName);
            }

            // Clear cache for this specific mod to get fresh status immediately
            string installedCacheKey = $"installed_{modDisplayName}";
            string enabledCacheKey = $"enabled_{modDisplayName}";

            if (modInstalledCache.ContainsKey(installedCacheKey))
            {
                modInstalledCache.Remove(installedCacheKey);
            }
            if (modEnabledCache.ContainsKey(enabledCacheKey))
            {
                modEnabledCache.Remove(enabledCacheKey);
            }
            if (modCacheTime.ContainsKey(installedCacheKey))
            {
                modCacheTime.Remove(installedCacheKey);
            }
            if (modCacheTime.ContainsKey(enabledCacheKey))
            {
                modCacheTime.Remove(enabledCacheKey);
            }

            DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} state and cache reset for fresh verification");
        }

        /// <summary>
        /// Resets all mod states (e.g., when dialog opens)
        /// </summary>
        private static void ResetAllModStates()
        {
            modStates.Clear();
            DebugConsole.Log("[ModCompatibilityGUI] All mod states reset");
        }

        // ==================== Restart Prompt System (ItsLuke feedback) ====================

        /// <summary>
        /// Shows native-style restart prompt when mods have been modified
        /// Based on ONI's native mod management restart pattern
        /// </summary>
        private static void ShowNativeRestartPrompt()
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Showing native restart prompt due to mod changes");

                // TODO: Integrate with ONI's native confirmation dialog system in future
                // For now, using custom restart prompt to ensure compatibility
                DebugConsole.Log("[ModCompatibilityGUI] Using custom restart prompt (native integration planned for future)");

                // Fallback to custom restart prompt if native system not available
                ShowCustomRestartPrompt();
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error showing native restart prompt: {ex.Message}");

                // Final fallback to notification system
                ShowRestartNotification();
            }
        }

        /// <summary>
        /// Custom restart prompt as fallback when native system not available
        /// </summary>
        private static void ShowCustomRestartPrompt()
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Showing custom restart prompt as fallback");

                // Mark that we need to show restart dialog
                showRestartDialog = true;
                restartDialogTime = Time.realtimeSinceStartup;

                // Ensure GUI component exists to show the dialog
                if (instance == null)
                {
                    GameObject guiObject = new GameObject("ModCompatibilityGUI_RestartPrompt");
                    DontDestroyOnLoad(guiObject);
                    instance = guiObject.AddComponent<ModCompatibilityGUI>();
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in ShowCustomRestartPrompt: {ex.Message}");

                // Final fallback
                ShowRestartNotification();
            }
        }

        /// <summary>
        /// Triggers game restart using ONI's native restart system
        /// </summary>
        private static void TriggerGameRestart()
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Triggering game restart...");

                // Save mod configuration first
                var modManager = Global.Instance?.modManager;
                if (modManager != null)
                {
                    modManager.Save();
                    DebugConsole.Log("[ModCompatibilityGUI] Mod configuration saved");
                }

                // Close our dialog
                CloseDialog();

                // Trigger restart via App.instance (ONI standard way)
                if (App.instance != null)
                {
                    DebugConsole.Log("[ModCompatibilityGUI] Restarting game via App.instance.Restart()");
                    App.instance.Restart();
                }
                else
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] App.instance not available - manual restart required");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error triggering restart: {ex.Message}");
                DebugConsole.Log("[ModCompatibilityGUI] Please restart the game manually to apply mod changes");
            }
        }

        // Custom restart dialog fields
        private static bool showRestartDialog = false;
        private static float restartDialogTime = 0f;

        /// <summary>
        /// Draws custom restart dialog as fallback when native system not available
        /// </summary>
        void DrawRestartDialog()
        {
            // Dark semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Dialog window
            float width = 500f;
            float height = 200f;
            float x = (Screen.width - width) / 2;
            float y = (Screen.height - height) / 2;

            Rect dialogRect = new Rect(x, y, width, height);

            // Create window style
            GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = CreateColorTexture(new Color(0.15f, 0.15f, 0.15f, 0.95f));
            windowStyle.border = new RectOffset(10, 10, 10, 10);

            // Draw window
            GUI.Window(54321, dialogRect, DrawRestartDialogWindow, MP_STRINGS.UI.MODCOMPATIBILITY.RESTART_REQUIRED_TITLE, windowStyle);
        }

        void DrawRestartDialogWindow(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Space(10);

            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.white;
            titleStyle.wordWrap = true;

            GUILayout.Label(MP_STRINGS.UI.MODCOMPATIBILITY.RESTART_REQUIRED_MESSAGE, titleStyle);
            GUILayout.Space(20);

            // Buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Restart Now button
            GUIStyle restartButtonStyle = new GUIStyle(GUI.skin.button);
            restartButtonStyle.fontSize = 14;
            restartButtonStyle.fontStyle = FontStyle.Bold;
            restartButtonStyle.normal.textColor = Color.green;

            if (GUILayout.Button(MP_STRINGS.UI.MODCOMPATIBILITY.RESTART_NOW, restartButtonStyle, GUILayout.Height(40), GUILayout.Width(150)))
            {
                DebugConsole.Log("[ModCompatibilityGUI] User confirmed restart via custom dialog");
                showRestartDialog = false;
                TriggerGameRestart();
            }

            GUILayout.Space(20);

            // Restart Later button
            GUIStyle laterButtonStyle = new GUIStyle(GUI.skin.button);
            laterButtonStyle.fontSize = 14;
            laterButtonStyle.fontStyle = FontStyle.Bold;
            laterButtonStyle.normal.textColor = Color.yellow;

            if (GUILayout.Button(MP_STRINGS.UI.MODCOMPATIBILITY.RESTART_LATER, laterButtonStyle, GUILayout.Height(40), GUILayout.Width(150)))
            {
                DebugConsole.Log("[ModCompatibilityGUI] User chose to restart later via custom dialog");
                showRestartDialog = false;
                CloseDialog();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();

            // Make window non-draggable (centered confirmation dialog)
        }

        // Note: DelayedAutoInstall() function removed based on ItsLuke feedback
        // Users must now explicitly choose to install mods via button clicks
        // Auto-enable for installed but disabled mods is preserved as approved by ItsLuke
    }
}