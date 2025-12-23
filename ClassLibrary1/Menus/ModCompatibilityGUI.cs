using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ONI_MP.DebugTools;

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

        private Vector2 scrollPosition = Vector2.zero;
        private Rect windowRect = new Rect(0, 0, 600, 400);

        public static void ShowIncompatibilityError(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityGUI] Showing compatibility dialog with IMGUI");

                // Store dialog data
                dialogReason = reason ?? "";
                dialogMissingMods = missingMods ?? new string[0];
                dialogExtraMods = extraMods ?? new string[0];
                dialogVersionMismatches = versionMismatches ?? new string[0];

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
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Failed to show dialog: {ex.Message}");
            }
        }

        public static void CloseDialog()
        {
            showDialog = false;

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

        void OnGUI()
        {
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
            headerStyle.normal.textColor = Color.red;

            GUILayout.Label("Mod Compatibility Error", headerStyle);
            GUILayout.Space(10);

            // Scroll area for content
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(280));

            // Reason text
            if (!string.IsNullOrEmpty(dialogReason))
            {
                GUIStyle reasonStyle = new GUIStyle(GUI.skin.label);
                reasonStyle.fontStyle = FontStyle.Bold;
                reasonStyle.wordWrap = true;
                reasonStyle.normal.textColor = Color.white;

                GUILayout.Label(dialogReason, reasonStyle);
                GUILayout.Space(10);
            }

            // Missing mods section - só mostrar se realmente estão em falta
            if (dialogMissingMods != null && dialogMissingMods.Length > 0)
            {
                // Filtrar apenas mods que realmente não estão instalados/habilitados
                var trulyMissingMods = new List<string>();
                var installedButDisabledMods = new List<string>();

                foreach (var mod in dialogMissingMods)
                {
                    if (IsModEnabled(mod))
                    {
                        // Se está habilitado, não é realmente "missing"
                        DebugConsole.Log($"[ModCompatibilityGUI] Mod {mod} está habilitado, ignorando da lista de missing");
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
                    DrawModSection("MISSING MODS (install these):", trulyMissingMods.ToArray(), Color.red, "Install");
                    GUILayout.Space(10);
                }

                // Mostrar mods instalados mas desabilitados separadamente
                if (installedButDisabledMods.Count > 0)
                {
                    DrawModSection("DISABLED MODS (enable these):", installedButDisabledMods.ToArray(), Color.yellow, "Enable");
                    GUILayout.Space(10);
                }
            }

            // Extra mods section (only show if no missing mods - policy permissive)
            if (dialogExtraMods != null && dialogExtraMods.Length > 0 &&
                (dialogMissingMods == null || dialogMissingMods.Length == 0) &&
                (dialogVersionMismatches == null || dialogVersionMismatches.Length == 0))
            {
                DrawInfoSection("You have extra mods (this is allowed):", dialogExtraMods);
                GUILayout.Space(10);
            }
            else if (dialogExtraMods != null && dialogExtraMods.Length > 0)
            {
                DrawModSection("EXTRA MODS (you have these):", dialogExtraMods, Color.yellow, "View");
                GUILayout.Space(10);
            }

            // Version mismatches section
            if (dialogVersionMismatches != null && dialogVersionMismatches.Length > 0)
            {
                DrawModSection("VERSION MISMATCHES (update these):", dialogVersionMismatches, Color.cyan, "Update");
                GUILayout.Space(10);
            }

            // Instructions
            GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
            instructionStyle.fontStyle = FontStyle.Italic;
            instructionStyle.wordWrap = true;
            instructionStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            if (dialogMissingMods.Length > 0 || dialogVersionMismatches.Length > 0)
            {
                GUILayout.Label("Install/disable the required mods, then try connecting again.", instructionStyle);
            }
            else if (dialogExtraMods.Length > 0)
            {
                GUILayout.Label("Connection allowed. Your extra mods shouldn't cause issues.", instructionStyle);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Action buttons section
            GUILayout.BeginHorizontal();

            // Verificar se há mods realmente missing ou apenas desabilitados
            bool hasTrulyMissing = false;
            bool hasDisabled = false;

            if (dialogMissingMods != null && dialogMissingMods.Length > 0)
            {
                foreach (var mod in dialogMissingMods)
                {
                    if (IsModEnabled(mod))
                    {
                        // Mod está habilitado, ignorar
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

            // Install All button (apenas para mods realmente missing)
            if (hasTrulyMissing)
            {
                GUIStyle installAllStyle = new GUIStyle(GUI.skin.button);
                installAllStyle.fontSize = 12;
                installAllStyle.fontStyle = FontStyle.Bold;
                installAllStyle.normal.textColor = Color.cyan;

                if (GUILayout.Button("Install All", installAllStyle, GUILayout.Height(35)))
                {
                    InstallAllMods();
                }

                GUILayout.Space(10);
            }

            // Enable All button (apenas para mods instalados mas desabilitados)
            if (hasDisabled)
            {
                GUIStyle enableAllStyle = new GUIStyle(GUI.skin.button);
                enableAllStyle.fontSize = 12;
                enableAllStyle.fontStyle = FontStyle.Bold;
                enableAllStyle.normal.textColor = Color.green;

                if (GUILayout.Button("Enable All", enableAllStyle, GUILayout.Height(35)))
                {
                    EnableAllMods();
                }
            }

            GUILayout.FlexibleSpace();

            // Close button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
            buttonStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Fechar", buttonStyle, GUILayout.Height(35)))
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

                    if (GUILayout.Button("Enable", enableButtonStyle, GUILayout.Width(55), GUILayout.Height(20)))
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
                    if (mod?.label != null && mod.label.id == modId)
                    {
                        return true;
                    }
                }
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
                    if (mod?.label != null && mod.label.id == modId)
                    {
                        // Usar o método correto do modManager
                        return modManager.IsModEnabled(mod.label);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error checking if mod is enabled: {ex.Message}");
            }

            return false;
        }

        private void EnableMod(string modDisplayName)
        {
            try
            {
                string modId = ExtractModId(modDisplayName);
                var modManager = Global.Instance?.modManager;

                if (modManager == null)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] ModManager não disponível");
                    OpenSteamWorkshopPage(modDisplayName);
                    return;
                }

                // Procura o mod na lista
                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null && mod.label.id == modId)
                    {
                        // Verifica se já está ativado
                        if (modManager.IsModEnabled(mod.label))
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} já estava ativo");
                            return;
                        }

                        try
                        {
                            // Ativa o mod
                            modManager.EnableMod(mod.label, true, null);
                            modManager.Save();

                            DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} ativado com sucesso!");
                            return;
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro ao ativar mod {modDisplayName}: {ex.Message}");
                            OpenSteamWorkshopPage(modDisplayName);
                            return;
                        }
                    }
                }

                DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod {modDisplayName} não encontrado na lista");
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
                string modId = ExtractModId(modDisplayName);

                if (buttonText == "Install")
                {
                    // Tentar instalar automaticamente primeiro
                    DebugConsole.Log($"[ModCompatibilityGUI] Tentando instalação automática do mod: {modDisplayName}");

                    WorkshopInstaller.Instance.InstallAndActivateMod(modId, success => {
                        if (success)
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Mod {modDisplayName} instalado e ativado automaticamente!");
                        }
                        else
                        {
                            DebugConsole.Log($"[ModCompatibilityGUI] Instalação automática falhou para {modDisplayName}, abrindo Steam Workshop");
                            OpenSteamWorkshopPage(modDisplayName);
                        }
                    });
                }
                else
                {
                    // Outros botões (View, Update) - abrir página do Steam
                    OpenSteamWorkshopPage(modDisplayName);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro ao processar ação do mod {modDisplayName}: {ex.Message}");
                // Fallback para abrir página
                OpenSteamWorkshopPage(modDisplayName);
            }
        }

        private void InstallAllMods()
        {
            try
            {
                if (dialogMissingMods == null || dialogMissingMods.Length == 0)
                    return;

                DebugConsole.Log($"[ModCompatibilityGUI] Iniciando instalação de {dialogMissingMods.Length} mods...");

                // Extrair IDs dos mods
                List<string> modIds = new List<string>();
                foreach (var mod in dialogMissingMods)
                {
                    string modId = ExtractModId(mod);
                    if (!string.IsNullOrEmpty(modId))
                    {
                        modIds.Add(modId);
                    }
                }

                if (modIds.Count == 0)
                {
                    DebugConsole.LogWarning("[ModCompatibilityGUI] Nenhum ID de mod válido encontrado");
                    return;
                }

                // Usar o WorkshopInstaller para instalar todos
                WorkshopInstaller.Instance.InstallMultipleItems(
                    modIds.ToArray(),
                    onProgress: (completed, total) => {
                        DebugConsole.Log($"[ModCompatibilityGUI] Progresso da instalação: {completed}/{total}");
                    },
                    onComplete: installedPaths => {
                        DebugConsole.Log($"[ModCompatibilityGUI] Instalação em lote concluída! {installedPaths.Length} mods processados");

                        // Tentar ativar todos os mods instalados
                        int activatedCount = 0;
                        for (int i = 0; i < modIds.Count && i < installedPaths.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(installedPaths[i]))
                            {
                                if (WorkshopInstaller.Instance.ActivateInstalledMod(modIds[i], installedPaths[i]))
                                {
                                    activatedCount++;
                                }
                            }
                        }

                        DebugConsole.Log($"[ModCompatibilityGUI] {activatedCount} mods ativados automaticamente");

                        if (activatedCount < modIds.Count)
                        {
                            DebugConsole.Log("[ModCompatibilityGUI] Alguns mods podem precisar de ativação manual ou reinicialização do jogo");
                        }
                    },
                    onError: error => {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro na instalação em lote: {error}");

                        // Fallback: abrir primeira página do Steam Workshop
                        if (dialogMissingMods.Length > 0)
                        {
                            OpenSteamWorkshopPage(dialogMissingMods[0]);
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro em InstallAllMods: {ex.Message}");
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
                    DebugConsole.LogWarning("[ModCompatibilityGUI] ModManager não disponível");
                    // Fallback para abrir Steam Workshop
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
                            if (mod?.label != null && mod.label.id == modId)
                            {
                                try
                                {
                                    modManager.EnableMod(mod.label, true, null);
                                    enabledCount++;
                                    modFound = true;
                                    DebugConsole.Log($"[ModCompatibilityGUI] Ativado: {modDisplayName}");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro ao ativar {modDisplayName}: {ex.Message}");
                                }
                            }
                        }

                        if (!modFound)
                        {
                            notFoundCount++;
                            DebugConsole.LogWarning($"[ModCompatibilityGUI] Mod não encontrado: {modDisplayName}");
                        }
                    }
                }

                if (enabledCount > 0)
                {
                    // Salva as mudanças
                    try
                    {
                        modManager.Save();
                        DebugConsole.Log($"[ModCompatibilityGUI] {enabledCount} mods ativados com sucesso!");

                        if (notFoundCount > 0)
                        {
                            DebugConsole.LogWarning($"[ModCompatibilityGUI] {notFoundCount} mods não foram encontrados na lista");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogWarning($"[ModCompatibilityGUI] Erro ao salvar configurações: {ex.Message}");
                    }
                }
                else
                {
                    DebugConsole.Log("[ModCompatibilityGUI] Nenhum mod desabilitado foi encontrado para ativar");

                    // Se não conseguiu ativar nenhum, abre Steam Workshop como fallback
                    if (dialogMissingMods.Length > 0)
                    {
                        DebugConsole.Log("[ModCompatibilityGUI] Abrindo Steam Workshop como fallback");
                        OpenSteamWorkshopPage(dialogMissingMods[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityGUI] Error in EnableAllMods: {ex.Message}");
            }
        }
    }
}