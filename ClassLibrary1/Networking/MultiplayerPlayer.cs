﻿using ONI_MP.Networking;
using ONI_MP.Networking.States;
using Steamworks;

public class MultiplayerPlayer
{
    public CSteamID SteamID { get; private set; }
    public string SteamName { get; private set; }
    public bool IsLocal => SteamID == SteamUser.GetSteamID();

    public int Ping { get; set; } = -1;
    public int AvatarImageId { get; private set; } = -1;
    public HSteamNetConnection? Connection { get; set; } = null;
    public bool IsConnected => Connection != null;

    public ClientReadyState readyState { get; set; } = ClientReadyState.Unready;
    public bool ModSyncCompleted { get; set; } = false;
    public bool ModSyncCompatible { get; set; } = false;
    public bool SaveFileSent { get; set; } = false;
    
    public MultiplayerPlayer(CSteamID steamID)
    {
        SteamID = steamID;
        SteamName = SteamFriends.GetFriendPersonaName(steamID);
        AvatarImageId = SteamFriends.GetLargeFriendAvatar(steamID);
        readyState = (SteamID == MultiplayerSession.HostSteamID) ? ClientReadyState.Ready : ClientReadyState.Unready;
    }

    public override string ToString()
    {
        return $"{SteamName} ({SteamID})";
    }
}
