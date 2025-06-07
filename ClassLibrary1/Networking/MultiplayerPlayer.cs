using Steamworks;

namespace ONI_MP.Networking
{
    public class MultiplayerPlayer
    {
        public CSteamID SteamID { get; private set; }
        public string SteamName { get; private set; }
        public bool IsLocal => SteamID == SteamUser.GetSteamID();

        // New: Ping (can be updated periodically from a ping system)
        public int Ping { get; set; } = -1; // -1 means unknown/uninitialized

        // New: Avatar image handle (can be used with SteamFriends.GetLargeFriendAvatar)
        public int AvatarImageId { get; private set; } = -1;

        public MultiplayerPlayer(CSteamID steamID)
        {
            SteamID = steamID;
            SteamName = SteamFriends.GetFriendPersonaName(steamID);
            AvatarImageId = SteamFriends.GetLargeFriendAvatar(steamID);
        }

        public override string ToString()
        {
            return $"{SteamName} ({SteamID})";
        }
    }
}
