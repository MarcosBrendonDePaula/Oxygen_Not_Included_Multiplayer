using HarmonyLib;
using ONI_MP.UI;
using static STRINGS.UI.FRONTEND;

namespace ONI_MP.Patches
{
    [HarmonyPatch(typeof(GameScreenManager), "OnSpawn")]
    public static class GameScreenPatch
    {
        static void Postfix()
        {
            ChatScreen.Show();
        }
    }

}
