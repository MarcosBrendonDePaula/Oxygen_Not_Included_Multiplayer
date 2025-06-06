using Steamworks;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking
{
    public static class SteamRichPresence
    {
        public static void SetStatus(string status)
        {
            if (!SteamManager.Initialized)
            {
                DebugConsole.LogWarning("SteamRichPresence: Not initialized.");
                return;
            }

            SteamFriends.SetRichPresence("status", status);
            DebugConsole.Log($"SteamRichPresence: Status set to \"{status}\"");
        }

        public static void Clear()
        {
            if (!SteamManager.Initialized)
            {
                DebugConsole.LogWarning("SteamRichPresence: Not initialized.");
                return;
            }

            SteamFriends.ClearRichPresence();
            DebugConsole.Log("SteamRichPresence: Cleared.");
        }

        public static void SetLobbyInfo(CSteamID lobbyID, string status = "In Lobby")
        {
            if (!SteamManager.Initialized)
            {
                DebugConsole.LogWarning("SteamRichPresence: Not initialized.");
                return;
            }

            SteamFriends.SetRichPresence("status", status);
            SteamFriends.SetRichPresence("steam_lobby", lobbyID.ToString());

            DebugConsole.Log($"SteamRichPresence: Lobby info set. Status: \"{status}\", Lobby ID: {lobbyID}");
        }
    }
}
