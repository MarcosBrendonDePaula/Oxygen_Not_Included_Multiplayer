using ONI_MP.Networking.States;
using Steamworks;

public class MultiplayerPlayer
{
	public CSteamID SteamID { get; private set; }
	public string SteamName { get; private set; }
	public bool IsLocal => SteamID == SteamUser.GetSteamID();

	public int AvatarImageId { get; private set; } = -1;
	public HSteamNetConnection? Connection { get; set; } = null;
	public bool IsConnected => Connection != null;

	public ClientReadyState readyState = ClientReadyState.Ready;

    public MultiplayerPlayer(CSteamID steamID)
	{
		SteamID = steamID;
		SteamName = TrucatedName(SteamFriends.GetFriendPersonaName(steamID));
		AvatarImageId = SteamFriends.GetLargeFriendAvatar(steamID);
	}

	private string TrucatedName(string steamName)
	{
		if (steamName.Length > 24)
		{
			return steamName.Substring(0, 24) + "...";
		} else
		{
			return steamName;
		}
	}

	public override string ToString()
	{
		return $"{SteamName} ({SteamID})";
	}
}
