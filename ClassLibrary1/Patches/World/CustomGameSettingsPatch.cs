using HarmonyLib;
using ONI_MP.DebugTools;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch]
    public static class CustomGameSettingsPatch
    {
        // Patch to prevent NullReferenceException in GetCurrentClusterLayout
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomGameSettings), "GetCurrentClusterLayout")]
        public static bool Prefix_GetCurrentClusterLayout(ref object __result)
        {
            try
            {
                // Check if the game settings are properly initialized
                if (CustomGameSettings.Instance == null)
                {
                    DebugConsole.LogWarning("[CustomGameSettingsPatch] CustomGameSettings.Instance is null, returning null cluster layout");
                    __result = null;
                    return false; // Skip original method
                }
                
                // Let the original method run if everything is OK
                return true;
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[CustomGameSettingsPatch] Error in GetCurrentClusterLayout prefix: {ex}");
                __result = null;
                return false; // Skip original method to prevent crash
            }
        }

        // Patch to prevent NullReferenceException in GetCurrentQualitySetting
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomGameSettings), "GetCurrentQualitySetting", new[] { typeof(string) })]
        public static bool Prefix_GetCurrentQualitySetting_String(string setting_id, ref object __result)
        {
            try
            {
                if (CustomGameSettings.Instance == null || string.IsNullOrEmpty(setting_id))
                {
                    DebugConsole.LogWarning($"[CustomGameSettingsPatch] Invalid state for GetCurrentQualitySetting({setting_id})");
                    __result = null;
                    return false;
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError($"[CustomGameSettingsPatch] Error in GetCurrentQualitySetting prefix: {ex}");
                __result = null;
                return false;
            }
        }
    }
}