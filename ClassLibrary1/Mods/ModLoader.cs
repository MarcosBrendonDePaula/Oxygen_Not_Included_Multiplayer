using System.Collections.Generic;
using Steamworks; // Lembre-se de adicionar o pacote Steamworks.NET ao seu projeto

namespace ONI_MP.Mods
{
    public static class ModLoader
    {
        private static readonly KMod.Manager ModsManager = Global.Instance.modManager;

        // Retorna todos os mods conhecidos pelo sistema
        public static List<KMod.Mod> GetInstalledMods()
        {
            return ModsManager.mods;
        }

        // Retorna apenas os mods instalados E habilitados para o DLC ativo
        public static List<KMod.Mod> GetActiveInstalledMods()
        {
            return ModsManager.mods
                .FindAll(m => m.status == KMod.Mod.Status.Installed && m.IsEnabledForActiveDlc());
        }

        // Verifica se existe um mod com o ID informado
        public static bool ModExists(string modId)
        {
            return ModsManager.mods.Exists(m => m.label.id == modId);
        }

        // Verifica se o mod está habilitado para o DLC ativo
        public static bool IsModEnabled(string modId)
        {
            var mod = ModsManager.mods.Find(m => m.label.id == modId);
            return mod != null && mod.IsEnabledForActiveDlc();
        }

        // Ativa ou desativa um mod pelo ID
        public static bool SetModEnabled(string modId, bool enabled)
        {
            var mod = ModsManager.mods.Find(m => m.label.id == modId);
            if (mod == null)
                return false; // Mod não existe

            // Habilita ou desabilita usando a API do ONI
            return ModsManager.EnableMod(mod.label, enabled, null);
        }

        // Retorna o link da Steam Workshop para o mod
        public static string GetSteamWorkshopLink(string modId)
        {
            if (string.IsNullOrEmpty(modId))
                return null;

            // Remove o prefixo "workshop-" se existir
            const string steamPrefix = "workshop-";
            string steamId = modId.StartsWith(steamPrefix) ? modId.Substring(steamPrefix.Length) : modId;

            // Só gera link se for um número (opcional: deixa mais seguro)
            if (ulong.TryParse(steamId, out _))
                return $"https://steamcommunity.com/sharedfiles/filedetails/?id={steamId}";

            return null;
        }

        // Tenta subscrever automaticamente ao mod na Steam Workshop (Steamworks.NET)
        public static bool SubscribeToWorkshopMod(string modId)
        {
            const string steamPrefix = "workshop-";
            string steamId = modId.StartsWith(steamPrefix) ? modId.Substring(steamPrefix.Length) : modId;
            if (ulong.TryParse(steamId, out ulong workshopId))
            {
                // Verifica se o Steamworks está inicializado no contexto do seu jogo/projeto
                if (SteamManager.Initialized) // ou SteamAPI.Init(), dependendo do seu setup
                {
                    SteamUGC.SubscribeItem(new PublishedFileId_t(workshopId));
                    return true;
                }
            }
            return false;
        }

    }
}
