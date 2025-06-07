using Steamworks;

namespace ONI_MP.Networking
{
    public class MultiplayerPlayer
    {
        public CSteamID SteamID { get; private set; }
        public string SteamName { get; private set; }
        public bool IsLocal => SteamID == SteamUser.GetSteamID();

        public MultiplayerPlayer(CSteamID steamID)
        {
            SteamID = steamID;
            SteamName = SteamFriends.GetFriendPersonaName(steamID);
        }

        public override string ToString()
        {
            return $"{SteamName} ({SteamID})";
        }
    }
}
