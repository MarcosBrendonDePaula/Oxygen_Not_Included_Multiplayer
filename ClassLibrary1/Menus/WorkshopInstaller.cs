using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using ONI_MP.DebugTools;

namespace ONI_MP.Menus
{
    public class WorkshopInstaller : MonoBehaviour
    {
        private static WorkshopInstaller instance;
        private Dictionary<PublishedFileId_t, InstallOperation> activeInstalls = new Dictionary<PublishedFileId_t, InstallOperation>();

        private struct InstallOperation
        {
            public PublishedFileId_t FileId;
            public Action<string> OnReady;
            public Action<string> OnError;
            public bool IsComplete;
        }

        public static WorkshopInstaller Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject installerObject = new GameObject("WorkshopInstaller");
                    DontDestroyOnLoad(installerObject);
                    instance = installerObject.AddComponent<WorkshopInstaller>();
                }
                return instance;
            }
        }

        void Update()
        {
            // Necessário para callbacks da Steam funcionarem
            if (SteamManager.Initialized)
            {
                SteamAPI.RunCallbacks();
            }

            // Verifica pendências de ativação a cada 5 segundos
            if (Time.time - lastActivationCheck > 5f)
            {
                CheckForPendingActivations();
                lastActivationCheck = Time.time;
            }
        }

        private float lastActivationCheck = 0f;
        private Dictionary<string, float> pendingActivations = new Dictionary<string, float>();

        /// <summary>
        /// Verifica se há mods instalados aguardando ativação
        /// </summary>
        private void CheckForPendingActivations()
        {
            if (pendingActivations.Count == 0) return;

            var modManager = Global.Instance?.modManager;
            if (modManager == null) return;

            var keysToRemove = new List<string>();

            foreach (var kvp in pendingActivations)
            {
                string modId = kvp.Key;
                float pendingTime = kvp.Value;

                // Remove mods que estão pendentes há mais de 2 minutos
                if (Time.time - pendingTime > 120f)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] ⏰ Timeout: Mod {modId} removido da fila de ativação pendente");
                    keysToRemove.Add(modId);
                    continue;
                }

                // Tenta ativar mods pendentes
                try
                {
                    modManager.Report(null); // Refresh da lista

                    foreach (var mod in modManager.mods)
                    {
                        if (mod?.label != null && (mod.label.id == modId || mod.label.id.Contains(modId)))
                        {
                            if (!modManager.IsModEnabled(mod.label))
                            {
                                modManager.EnableMod(mod.label, true, null);
                                modManager.Save();
                                DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} ativado automaticamente em background!");
                            }
                            else
                            {
                                DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} já estava ativo!");
                            }
                            keysToRemove.Add(modId);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Erro ao ativar mod pendente {modId}: {ex.Message}");
                }
            }

            // Remove mods processados
            foreach (string key in keysToRemove)
            {
                pendingActivations.Remove(key);
            }
        }

        /// <summary>
        /// Adiciona um mod à fila de ativação pendente
        /// </summary>
        public void AddToPendingActivation(string modId)
        {
            if (!pendingActivations.ContainsKey(modId))
            {
                pendingActivations[modId] = Time.time;
                DebugConsole.Log($"[WorkshopInstaller] 📋 Mod {modId} adicionado à fila de ativação pendente");
            }
        }

        /// <summary>
        /// Instala um mod do Workshop automaticamente
        /// </summary>
        public void InstallWorkshopItem(string modId, Action<string> onReady, Action<string> onError)
        {
            if (!SteamManager.Initialized)
            {
                onError?.Invoke("Steam não inicializada");
                return;
            }

            // Converte string ID para PublishedFileId_t
            if (!ulong.TryParse(modId, out ulong fileIdULong))
            {
                onError?.Invoke($"ID de mod inválido: {modId}");
                return;
            }

            PublishedFileId_t fileId = new PublishedFileId_t(fileIdULong);
            DebugConsole.Log($"[WorkshopInstaller] Iniciando instalação do mod {modId}");

            StartCoroutine(InstallWorkshopItemCoroutine(fileId, onReady, onError));
        }

        /// <summary>
        /// Instala múltiplos mods em sequência
        /// </summary>
        public void InstallMultipleItems(string[] modIds, Action<int, int, string> onProgress, Action<string[]> onComplete, Action<string> onError)
        {
            StartCoroutine(InstallMultipleItemsCoroutine(modIds, onProgress, onComplete, onError));
        }

        /// <summary>
        /// Instala múltiplos mods em sequência com mapeamento ID->Nome para melhor UI
        /// </summary>
        public void InstallMultipleItems(string[] modIds, Dictionary<string, string> modIdToName, Action<int, int, string> onProgress, Action<string[]> onComplete, Action<string> onError)
        {
            StartCoroutine(InstallMultipleItemsCoroutineWithNames(modIds, modIdToName, onProgress, onComplete, onError));
        }

        private IEnumerator InstallMultipleItemsCoroutineWithNames(string[] modIds, Dictionary<string, string> modIdToName, Action<int, int, string> onProgress, Action<string[]> onComplete, Action<string> onError)
        {
            List<string> installedPaths = new List<string>();
            int completed = 0;
            bool hasError = false;

            foreach (string modId in modIds)
            {
                if (hasError) break;

                bool installSuccess = false;
                string installPath = "";
                string installError = "";

                // Pega o nome do mod do mapping, ou usa o ID se não encontrar
                string modName = modIdToName.ContainsKey(modId) ? modIdToName[modId] : modId;

                DebugConsole.Log($"[WorkshopInstaller] 📥 Iniciando instalação do mod: {modName}");
                onProgress?.Invoke(completed, modIds.Length, $"📥 Instalando {modName}...");

                InstallWorkshopItem(modId,
                    onReady: path => {
                        installSuccess = true;
                        installPath = path;
                    },
                    onError: err => {
                        installError = err;
                    }
                );

                // Espera a instalação terminar
                yield return new WaitUntil(() => installSuccess || !string.IsNullOrEmpty(installError));

                if (installSuccess)
                {
                    installedPaths.Add(installPath);
                    DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modName} instalado com sucesso");
                    onProgress?.Invoke(completed + 1, modIds.Length, $"✅ {modName} instalado! Ativando...");
                }
                else
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Falha ao instalar mod {modName}: {installError}");
                    hasError = true;
                    onError?.Invoke($"Falha na instalação de {modName}: {installError}");
                    break;
                }

                completed++;

                // Pequena pausa entre instalações
                yield return new WaitForSeconds(0.5f);
            }

            if (!hasError)
            {
                onComplete?.Invoke(installedPaths.ToArray());
            }
        }

        private IEnumerator InstallMultipleItemsCoroutine(string[] modIds, Action<int, int, string> onProgress, Action<string[]> onComplete, Action<string> onError)
        {
            List<string> installedPaths = new List<string>();
            int completed = 0;
            bool hasError = false;

            foreach (string modId in modIds)
            {
                if (hasError) break;

                bool installSuccess = false;
                string installPath = "";
                string installError = "";

                DebugConsole.Log($"[WorkshopInstaller] 📥 Iniciando instalação do mod ID: {modId}");
                onProgress?.Invoke(completed, modIds.Length, $"📥 Instalando mod {modId}...");

                InstallWorkshopItem(modId,
                    onReady: path => {
                        installSuccess = true;
                        installPath = path;
                    },
                    onError: err => {
                        installError = err;
                    }
                );

                // Espera a instalação terminar
                yield return new WaitUntil(() => installSuccess || !string.IsNullOrEmpty(installError));

                if (installSuccess)
                {
                    installedPaths.Add(installPath);
                    DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} instalado com sucesso");
                    onProgress?.Invoke(completed + 1, modIds.Length, $"✅ Mod {modId} instalado! Ativando...");
                }
                else
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Falha ao instalar mod {modId}: {installError}");
                    hasError = true;
                    onError?.Invoke($"Falha na instalação de {modId}: {installError}");
                    break;
                }

                completed++;

                // Pequena pausa entre instalações
                yield return new WaitForSeconds(0.5f);
            }

            if (!hasError)
            {
                onComplete?.Invoke(installedPaths.ToArray());
            }
        }

        private IEnumerator InstallWorkshopItemCoroutine(PublishedFileId_t fileId, Action<string> onReady, Action<string> onError)
        {
            // Verifica se já está instalado primeiro
            uint currentState = SteamUGC.GetItemState(fileId);
            bool alreadyInstalled = (currentState & (uint)EItemState.k_EItemStateInstalled) != 0;
            bool needsUpdate = (currentState & (uint)EItemState.k_EItemStateNeedsUpdate) != 0;

            if (alreadyInstalled && !needsUpdate)
            {
                // Já instalado, só pega o caminho
                string existingPath = GetInstalledItemPath(fileId);
                if (!string.IsNullOrEmpty(existingPath))
                {
                    DebugConsole.Log($"[WorkshopInstaller] Mod {fileId} já estava instalado em: {existingPath}");
                    onReady?.Invoke(existingPath);
                    yield break;
                }
            }

            // 1) Subscribe
            var subscribeCall = SteamUGC.SubscribeItem(fileId);
            var subscribeResult = new CallResult<RemoteStorageSubscribePublishedFileResult_t>();
            bool subscribeDone = false;
            RemoteStorageSubscribePublishedFileResult_t subData = default;
            bool subIOFailure = false;

            subscribeResult.Set(subscribeCall, (data, ioFailure) =>
            {
                subData = data;
                subIOFailure = ioFailure;
                subscribeDone = true;
            });

            // Espera callback do subscribe
            float timeoutTime = Time.time + 30f; // 30 segundos timeout
            while (!subscribeDone && Time.time < timeoutTime)
            {
                yield return null;
            }

            if (!subscribeDone)
            {
                onError?.Invoke($"Timeout ao assinar item {fileId}");
                yield break;
            }

            if (subIOFailure || subData.m_eResult != EResult.k_EResultOK)
            {
                onError?.Invoke($"Falha ao assinar item {fileId}. Result={subData.m_eResult}");
                yield break;
            }

            DebugConsole.Log($"[WorkshopInstaller] Mod {fileId} assinado com sucesso");

            // 2) Força download
            SteamUGC.DownloadItem(fileId, true);

            // 3) Monitora instalação com logs detalhados
            timeoutTime = Time.time + 120f; // 2 minutos timeout (alguns mods são grandes)
            bool hasStartedDownload = false;
            float lastProgressTime = Time.time;

            DebugConsole.Log($"[WorkshopInstaller] Iniciando monitoramento da instalação do mod {fileId}");

            while (Time.time < timeoutTime)
            {
                uint state = SteamUGC.GetItemState(fileId);

                bool installed = (state & (uint)EItemState.k_EItemStateInstalled) != 0;
                bool updating = (state & (uint)EItemState.k_EItemStateNeedsUpdate) != 0;
                bool downloading = (state & (uint)EItemState.k_EItemStateDownloading) != 0;
                bool downloadPending = (state & (uint)EItemState.k_EItemStateDownloadPending) != 0;
                bool subscribed = (state & (uint)EItemState.k_EItemStateSubscribed) != 0;

                // Log estado atual para debug
                if (Time.time - lastProgressTime > 5f) // Log a cada 5 segundos
                {
                    DebugConsole.Log($"[WorkshopInstaller] Mod {fileId} - Estado: Subscribed={subscribed}, Downloading={downloading}, DownloadPending={downloadPending}, Updating={updating}, Installed={installed}");
                    lastProgressTime = Time.time;
                }

                // Detecta se download começou
                if ((downloading || downloadPending) && !hasStartedDownload)
                {
                    hasStartedDownload = true;
                    DebugConsole.Log($"[WorkshopInstaller] Mod {fileId} começou a baixar");
                }

                // Força download novamente se necessário
                if (updating || (!hasStartedDownload && !downloading && !downloadPending && subscribed))
                {
                    SteamUGC.DownloadItem(fileId, true);
                    DebugConsole.Log($"[WorkshopInstaller] Forçando download do mod {fileId}");
                }

                // Verifica se instalação terminou
                if (installed && !updating && !downloading && !downloadPending)
                {
                    DebugConsole.Log($"[WorkshopInstaller] Mod {fileId} instalação completada!");
                    break;
                }

                // Verifica se não está progredindo (timeout dinâmico)
                if (hasStartedDownload && !downloading && !downloadPending && !installed && Time.time - lastProgressTime > 30f)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Mod {fileId} parece ter parado de baixar, tentando novamente...");
                    SteamUGC.DownloadItem(fileId, true);
                    lastProgressTime = Time.time;
                }

                yield return new WaitForSeconds(0.5f); // Check a cada 0.5 segundos
            }

            // 4) Pega a pasta onde o Steam instalou
            string finalPath = GetInstalledItemPath(fileId);
            if (!string.IsNullOrEmpty(finalPath))
            {
                DebugConsole.Log($"[WorkshopInstaller] Mod {fileId} instalado em: {finalPath}");
                onReady?.Invoke(finalPath);
            }
            else
            {
                onError?.Invoke($"Item {fileId} instalado, mas não consegui obter o diretório");
            }
        }

        private string GetInstalledItemPath(PublishedFileId_t fileId)
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
                DebugConsole.LogWarning($"[WorkshopInstaller] Erro ao obter caminho do item {fileId}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Ativa um mod no sistema do jogo após instalação
        /// </summary>
        public bool ActivateInstalledMod(string modId, string installedPath)
        {
            try
            {
                DebugConsole.Log($"[WorkshopInstaller] 🔄 Iniciando ativação automática do mod {modId}");
                DebugConsole.Log($"[WorkshopInstaller] Caminho de instalação: {installedPath}");

                var modManager = Global.Instance?.modManager;
                if (modManager == null)
                {
                    DebugConsole.LogWarning("[WorkshopInstaller] ❌ ModManager não disponível - aguardando sistema carregar...");
                    return false;
                }

                DebugConsole.Log($"[WorkshopInstaller] 📋 Total de mods carregados no sistema: {modManager.mods?.Count ?? 0}");

                // Força recarregamento do mod manager para detectar novos mods recém-instalados
                try
                {
                    DebugConsole.Log("[WorkshopInstaller] 🔄 Recarregando lista de mods para detectar novos...");
                    modManager.Report(null);
                    DebugConsole.Log($"[WorkshopInstaller] ✅ Recarregamento completo. Mods disponíveis: {modManager.mods?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    DebugConsole.LogWarning($"[WorkshopInstaller] ⚠️ Erro ao recarregar mod manager: {ex.Message}");
                }

                // Lista todos os mods para debug
                if (modManager.mods != null)
                {
                    DebugConsole.Log("[WorkshopInstaller] 📜 Lista de mods detectados:");
                    int count = 0;
                    foreach (var mod in modManager.mods)
                    {
                        if (mod?.label != null)
                        {
                            count++;
                            string status = modManager.IsModEnabled(mod.label) ? "✅ ATIVO" : "⚪ INATIVO";
                            DebugConsole.Log($"[WorkshopInstaller]   [{count}] {mod.label.id} - {mod.title} - {status}");
                        }
                    }
                }

                // Procura o mod na lista usando múltiplos métodos de busca
                DebugConsole.Log($"[WorkshopInstaller] 🔍 Procurando mod com ID: '{modId}'");

                foreach (var mod in modManager.mods)
                {
                    if (mod?.label != null)
                    {
                        string actualId = mod.label.id;
                        string staticId = mod.label.defaultStaticID;

                        // Múltiplas formas de match (exata, contém, numérica)
                        bool isMatch = actualId == modId ||
                                      staticId == modId ||
                                      actualId.Contains(modId) ||
                                      staticId.Contains(modId) ||
                                      modId.Contains(actualId) ||
                                      modId.Contains(staticId);

                        if (isMatch)
                        {
                            DebugConsole.Log($"[WorkshopInstaller] ✅ Mod encontrado! ID: {actualId}, Título: {mod.title}");

                            // Verifica se o mod está habilitado
                            bool isEnabled = modManager.IsModEnabled(mod.label);
                            DebugConsole.Log($"[WorkshopInstaller] Status atual: {(isEnabled ? "✅ JÁ ATIVO" : "⚪ INATIVO")}");

                            if (!isEnabled)
                            {
                                try
                                {
                                    DebugConsole.Log($"[WorkshopInstaller] 🔧 Ativando mod {mod.title}...");

                                    // Ativa o mod usando o método correto do ONI
                                    modManager.EnableMod(mod.label, true, null);

                                    // Salva as mudanças imediatamente
                                    modManager.Save();

                                    // Confirma ativação
                                    bool nowEnabled = modManager.IsModEnabled(mod.label);
                                    if (nowEnabled)
                                    {
                                        DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} ({mod.title}) ATIVADO COM SUCESSO!");
                                        return true;
                                    }
                                    else
                                    {
                                        DebugConsole.LogWarning($"[WorkshopInstaller] ⚠️ Mod {modId} foi processado mas ainda não aparece como ativo");
                                        return false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Erro ao ativar mod {modId}: {ex.Message}");
                                    return false;
                                }
                            }
                            else
                            {
                                DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} ({mod.title}) já estava ativo!");
                                return true;
                            }
                        }
                    }
                }

                // Mod não encontrado - tenta estratégia mais agressiva
                DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Mod {modId} não encontrado na primeira busca");
                DebugConsole.Log("[WorkshopInstaller] 🔄 Tentando recarregamento mais agressivo...");

                // Tenta múltiplos recarregamentos
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        DebugConsole.Log($"[WorkshopInstaller] Tentativa {attempt}/3 de recarregamento...");

                        // Wait a bit before retry
                        if (attempt > 1)
                        {
                            System.Threading.Thread.Sleep(1000 * attempt); // Progressive delay
                        }

                        modManager.Report(null);

                        // Procura novamente
                        foreach (var mod in modManager.mods)
                        {
                            if (mod?.label != null && (mod.label.id == modId || mod.label.id.Contains(modId)))
                            {
                                DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} encontrado na tentativa {attempt}!");

                                if (!modManager.IsModEnabled(mod.label))
                                {
                                    modManager.EnableMod(mod.label, true, null);
                                    modManager.Save();
                                }

                                DebugConsole.Log($"[WorkshopInstaller] ✅ Mod {modId} ativado com sucesso na tentativa {attempt}!");
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.LogWarning($"[WorkshopInstaller] Erro na tentativa {attempt}: {ex.Message}");
                    }
                }

                DebugConsole.LogWarning($"[WorkshopInstaller] ⚠️ Mod {modId} foi instalado mas não conseguiu ser ativado automaticamente");
                DebugConsole.Log("[WorkshopInstaller] 🔄 Adicionando à fila de ativação pendente para tentar novamente em background");

                // Adiciona à fila de ativação pendente para tentar continuamente em background
                AddToPendingActivation(modId);

                return true; // Consideramos sucesso parcial - sistema continuará tentando
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[WorkshopInstaller] ❌ Erro crítico ao ativar mod {modId}: {ex.Message}");
                DebugConsole.LogWarning($"[WorkshopInstaller] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Instala e ativa um mod automaticamente
        /// </summary>
        public void InstallAndActivateMod(string modId, Action<bool> onComplete)
        {
            InstallWorkshopItem(modId,
                onReady: installedPath => {
                    // Pequena pausa para garantir que o sistema detectou o mod
                    StartCoroutine(DelayedActivation(modId, installedPath, onComplete));
                },
                onError: error => {
                    DebugConsole.LogWarning($"[WorkshopInstaller] Falha na instalação automática: {error}");
                    onComplete?.Invoke(false);
                }
            );
        }

        private IEnumerator DelayedActivation(string modId, string installedPath, Action<bool> onComplete)
        {
            yield return new WaitForSeconds(1f); // Pausa para sistema detectar o mod

            bool activated = ActivateInstalledMod(modId, installedPath);
            onComplete?.Invoke(activated);
        }
    }
}