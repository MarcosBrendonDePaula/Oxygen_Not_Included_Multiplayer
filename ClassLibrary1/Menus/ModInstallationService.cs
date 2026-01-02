using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Steamworks;
using ONI_MP.DebugTools;

namespace ONI_MP.Menus
{
    /// <summary>
    /// Handles mod installation, Steam integration, and activation/deactivation
    /// </summary>
    public class ModInstallationService : MonoBehaviour
    {
        private static ModInstallationService instance;

        // Installation state tracking
        private Dictionary<string, bool> currentInstallations = new Dictionary<string, bool>();

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static ModInstallationService Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject serviceObject = new GameObject("ModInstallationService");
                    DontDestroyOnLoad(serviceObject);
                    instance = serviceObject.AddComponent<ModInstallationService>();
                }
                return instance;
            }
        }

        /// <summary>
        /// Enables a single mod using ONI's native system
        /// </summary>
        public void EnableMod(string modDisplayName)
        {
            try
            {
                string modId = ModStateManager.ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;

                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModInstallationService] ModManager not available");
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
                            DebugConsole.Log($"[ModInstallationService] Found mod to enable: {modDisplayName} -> {defaultId}");
                        }

                        if (foundMod)
                        {
                            // Check if already enabled using proper ONI method
                            if (mod.IsEnabledForActiveDlc())
                            {
                                DebugConsole.Log($"[ModInstallationService] Mod {modDisplayName} was already enabled");
                                return;
                            }

                            // Check if mod is compatible
                            if (mod.available_content == 0)
                            {
                                DebugConsole.LogWarning($"[ModInstallationService] Mod {modDisplayName} is not compatible - opening Steam page");
                                OpenSteamWorkshopPage(modDisplayName);
                                return;
                            }

                            try
                            {
                                // Enable the mod using proper ONI API
                                mod.SetEnabledForActiveDlc(true);
                                DebugConsole.Log($"[ModInstallationService] Mod {modDisplayName} enabled successfully!");

                                // Mark that mods were modified
                                ModRestartManager.MarkModsModified();
                                return;
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.LogWarning($"[ModInstallationService] Error enabling mod {modDisplayName}: {ex.Message}");
                                OpenSteamWorkshopPage(modDisplayName);
                                return;
                            }
                        }
                    }
                }

                DebugConsole.LogWarning($"[ModInstallationService] Mod {modDisplayName} not found in list");
                OpenSteamWorkshopPage(modDisplayName);
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModInstallationService] Error in EnableMod: {ex.Message}");
                OpenSteamWorkshopPage(modDisplayName);
            }
        }

        /// <summary>
        /// Disables a single mod using ONI's native system
        /// </summary>
        public void DisableMod(string modDisplayName)
        {
            try
            {
                string modId = ModStateManager.ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;

                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModInstallationService] ModManager not available for disable");
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
                            DebugConsole.Log($"[ModInstallationService] Found mod to disable: {modDisplayName} -> {defaultId}");
                        }

                        if (foundMod)
                        {
                            // Check if already disabled
                            if (!mod.IsEnabledForActiveDlc())
                            {
                                DebugConsole.Log($"[ModInstallationService] Mod {modDisplayName} was already disabled");
                                return;
                            }

                            try
                            {
                                // Disable the mod using proper ONI API
                                mod.SetEnabledForActiveDlc(false);
                                DebugConsole.Log($"[ModInstallationService] Mod {modDisplayName} disabled successfully!");

                                // Mark that mods were modified
                                ModRestartManager.MarkModsModified();
                                return;
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.LogWarning($"[ModInstallationService] Error disabling mod {modDisplayName}: {ex.Message}");
                                return;
                            }
                        }
                    }
                }

                DebugConsole.LogWarning($"[ModInstallationService] Mod {modDisplayName} not found for disable");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModInstallationService] Error in DisableMod: {ex.Message}");
            }
        }

        /// <summary>
        /// Enables all disabled mods in the provided list
        /// </summary>
        public void EnableAllMods(string[] modList)
        {
            try
            {
                if (modList == null || modList.Length == 0)
                    return;

                var modManager = Global.Instance?.modManager;
                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModInstallationService] ModManager not available");
                    return;
                }

                int enabledCount = 0;
                int notFoundCount = 0;

                foreach (var modDisplayName in modList)
                {
                    if (ModStateManager.IsModInstalled(modDisplayName) && !ModStateManager.IsModEnabled(modDisplayName))
                    {
                        string modId = ModStateManager.ExtractModId(modDisplayName);
                        bool modFound = false;

                        foreach (var mod in modManager.mods)
                        {
                            if (mod?.label != null)
                            {
                                string defaultId = mod.label.defaultStaticID;
                                string labelId = mod.label.id;

                                // Use robust Steam mod matching
                                if (defaultId == modId || labelId == modId ||
                                    defaultId == modDisplayName || labelId == modDisplayName ||
                                    (modId != modDisplayName && (defaultId.StartsWith(modId) || labelId.StartsWith(modId))))
                                {
                                    try
                                    {
                                        // Check if mod is compatible
                                        if (mod.available_content == 0)
                                        {
                                            DebugConsole.LogWarning($"[ModInstallationService] Mod {modDisplayName} is not compatible - skipping");
                                            modFound = true;
                                            break;
                                        }

                                        // Enable using proper ONI API
                                        mod.SetEnabledForActiveDlc(true);
                                        enabledCount++;
                                        modFound = true;
                                        DebugConsole.Log($"[ModInstallationService] Enabled: {modDisplayName} -> {defaultId}");
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugConsole.LogWarning($"[ModInstallationService] Error enabling {modDisplayName}: {ex.Message}");
                                    }
                                }
                            }
                        }

                        if (!modFound)
                        {
                            notFoundCount++;
                            ModLogThrottler.LogThrottled($"Mod not found during enable operation: {modDisplayName}", "enable_missing");
                        }
                    }
                }

                if (enabledCount > 0)
                {
                    try
                    {
                        DebugConsole.Log($"[ModInstallationService] {enabledCount} mods enabled successfully!");

                        if (notFoundCount > 0)
                        {
                            DebugConsole.LogWarning($"[ModInstallationService] {notFoundCount} mods were not found in the list");
                        }

                        // Mark that mods were modified
                        ModRestartManager.MarkModsModified();

                        // Show restart prompt
                        DebugConsole.Log($"[ModInstallationService] {enabledCount} mods enabled - showing restart prompt");
                        ModRestartManager.ShowNativeRestartPrompt();
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogWarning($"[ModInstallationService] Error enabling mods: {ex.Message}");
                    }
                }
                else
                {
                    DebugConsole.Log("[ModInstallationService] No disabled mods found to enable");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModInstallationService] Error in EnableAllMods: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribe to a single mod via Steam Workshop
        /// </summary>
        public void SubscribeSingleMod(string modDisplayName, System.Action onSuccess, System.Action<string> onError)
        {
            try
            {
                // Check if Steam is initialized
                if (!SteamManager.Initialized)
                {
                    DebugConsole.LogWarning($"[ModInstallationService] Steam not initialized for mod: {modDisplayName}");
                    onError?.Invoke("Steam not initialized - please restart the game and try again");
                    return;
                }

                // Extract and validate mod ID
                string modId = ModStateManager.ExtractModId(modDisplayName);
                DebugConsole.Log($"[ModInstallationService] Extracted mod ID: '{modId}' from '{modDisplayName}'");

                if (string.IsNullOrEmpty(modId) || !ulong.TryParse(modId, out ulong testId))
                {
                    DebugConsole.LogWarning($"[ModInstallationService] Invalid mod ID '{modId}' for mod: {modDisplayName}");
                    onError?.Invoke($"Invalid mod ID: {modId} - check mod name format");
                    return;
                }

                DebugConsole.Log($"[ModInstallationService] Calling WorkshopInstaller for mod {modId}");

                // Use WorkshopInstaller's subscribe function
                WorkshopInstaller.Instance.SubscribeToWorkshopItem(
                    modId,
                    onSuccess: subscribedModId => {
                        DebugConsole.Log($"[ModInstallationService] Successfully subscribed to mod {subscribedModId}");
                        DebugConsole.Log($"[ModInstallationService] Steam will now handle installation automatically");

                        // Start monitoring for Steam's automatic installation
                        StartCoroutine(MonitorSteamInstallation(modId, modDisplayName, onSuccess, onError));
                    },
                    onError: error => {
                        DebugConsole.LogWarning($"[ModInstallationService] Subscribe failed for mod {modId}: {error}");
                        onError?.Invoke($"Steam subscription failed: {error}");
                    }
                );
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModInstallationService] Exception in SubscribeSingleMod for {modDisplayName}: {ex.Message}");
                onError?.Invoke($"Subscription error: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribe to all missing mods
        /// </summary>
        public void SubscribeAllMods(string[] missingMods, System.Action<int, int> onProgress, System.Action<List<string>, List<string>> onComplete)
        {
            StartCoroutine(SubscribeAllModsCoroutine(missingMods, onProgress, onComplete));
        }

        /// <summary>
        /// Coroutine to subscribe to all missing mods sequentially
        /// </summary>
        private IEnumerator SubscribeAllModsCoroutine(string[] missingMods, System.Action<int, int> onProgress, System.Action<List<string>, List<string>> onComplete)
        {
            var trulyMissingMods = new List<string>();
            foreach (var mod in missingMods)
            {
                if (!ModStateManager.IsModEnabled(mod) && !ModStateManager.IsModInstalled(mod))
                {
                    trulyMissingMods.Add(mod);
                }
            }

            if (trulyMissingMods.Count == 0)
            {
                DebugConsole.LogWarning("[ModInstallationService] No truly missing mods found - all are already installed");
                onComplete?.Invoke(new List<string>(), new List<string>());
                yield break;
            }

            int totalMods = trulyMissingMods.Count;
            int completedMods = 0;
            List<string> successfulMods = new List<string>();
            List<string> failedMods = new List<string>();

            DebugConsole.Log($"[ModInstallationService] Starting subscription to {totalMods} mods...");

            foreach (string modDisplayName in trulyMissingMods)
            {
                string modId = ModStateManager.ExtractModId(modDisplayName);

                if (string.IsNullOrEmpty(modId) || !ulong.TryParse(modId, out ulong testId))
                {
                    DebugConsole.LogWarning($"[ModInstallationService] Invalid mod ID for: {modDisplayName}");
                    failedMods.Add(modDisplayName);
                    completedMods++;
                    onProgress?.Invoke(completedMods, totalMods);
                    continue;
                }

                DebugConsole.Log($"[ModInstallationService] Subscribing to mod {completedMods + 1}/{totalMods}: {modDisplayName}");

                // Set mod to subscribing state
                ModStateManager.SetModSubscribing(modDisplayName);

                bool subscribeComplete = false;
                bool subscribeSuccess = false;

                // Use the simple subscribe function
                WorkshopInstaller.Instance.SubscribeToWorkshopItem(
                    modId,
                    onSuccess: subscribedModId => {
                        subscribeComplete = true;
                        subscribeSuccess = true;
                        DebugConsole.Log($"[ModInstallationService] Successfully subscribed to {modDisplayName}");
                        successfulMods.Add(modDisplayName);
                    },
                    onError: error => {
                        subscribeComplete = true;
                        subscribeSuccess = false;
                        DebugConsole.LogWarning($"[ModInstallationService] Failed to subscribe to {modDisplayName}: {error}");
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
                    DebugConsole.LogWarning($"[ModInstallationService] Timeout subscribing to {modDisplayName}");
                    failedMods.Add(modDisplayName);
                }

                // Update mod state
                ModStateManager.UpdateModStateAfterOperation(modDisplayName);
                completedMods++;
                onProgress?.Invoke(completedMods, totalMods);

                // Small pause between subscriptions
                yield return new WaitForSeconds(1f);
            }

            // Summary
            DebugConsole.Log($"[ModInstallationService] Subscription complete: {successfulMods.Count} successful, {failedMods.Count} failed");
            onComplete?.Invoke(successfulMods, failedMods);
        }

        /// <summary>
        /// Monitors Steam's automatic installation process
        /// </summary>
        private IEnumerator MonitorSteamInstallation(string modId, string modDisplayName, System.Action onSuccess, System.Action<string> onError)
        {
            if (!ulong.TryParse(modId, out ulong fileIdULong))
            {
                DebugConsole.LogWarning($"[ModInstallationService] Invalid mod ID for monitoring: {modId}");
                onError?.Invoke("Invalid mod ID");
                yield break;
            }

            PublishedFileId_t fileId = new PublishedFileId_t(fileIdULong);
            DebugConsole.Log($"[ModInstallationService] Starting Steam installation monitoring for mod {modId}...");

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
                        DebugConsole.Log($"[ModInstallationService] Steam status for mod {modId}: Subscribed={subscribed}, Installed={installed}, Downloading={downloading}, Pending={downloadPending}");
                        lastLogTime = Time.time;
                    }

                    // Check if installation completed
                    if (subscribed && installed && !downloading && !downloadPending)
                    {
                        DebugConsole.Log($"[ModInstallationService] Steam completed installation of mod {modId}!");

                        // Try to activate the mod
                        string installedPath = GetSteamModPath(fileId);
                        if (WorkshopInstaller.Instance.ActivateInstalledMod(modId, installedPath))
                        {
                            DebugConsole.Log($"[ModInstallationService] Mod {modDisplayName} successfully activated!");
                            ModRestartManager.MarkModsModified();
                            if (onSuccess != null) onSuccess();
                        }
                        else
                        {
                            DebugConsole.Log($"[ModInstallationService] Mod {modDisplayName} installed but activation pending");
                            if (onSuccess != null) onSuccess(); // Still consider it a success
                        }

                        yield break;
                    }

                    // If subscription was lost, stop monitoring
                    if (!subscribed)
                    {
                        DebugConsole.LogWarning($"[ModInstallationService] Lost subscription to mod {modId} - stopping monitoring");
                        onError?.Invoke("Lost subscription");
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[ModInstallationService] Error monitoring mod {modId}: {ex.Message}");
                }

                yield return new WaitForSeconds(5f); // Check every 5 seconds
            }

            // Timeout reached
            DebugConsole.LogWarning($"[ModInstallationService] Steam monitoring timeout for mod {modId} after 5 minutes");
            onError?.Invoke("Monitoring timeout");
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
                DebugConsole.LogWarning($"[ModInstallationService] Error getting Steam mod path: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Opens Steam Workshop page for a mod
        /// </summary>
        public void OpenSteamWorkshopPage(string modDisplayName)
        {
            try
            {
                string modId = ModStateManager.ExtractModId(modDisplayName);
                string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={modId}";

                DebugConsole.Log($"[ModInstallationService] Opening Steam Workshop: {url}");

                if (SteamManager.Initialized)
                {
                    SteamFriends.ActivateGameOverlayToWebPage(url);
                }
                else
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModInstallationService] Failed to open Steam page: {ex.Message}");
            }
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