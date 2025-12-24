
using Steamworks;

namespace ONI_MP
{
	internal class MP_STRINGS
	{
		public class UI
		{
			public class MAINMENU
			{
				public static LocString JOINGAME = "JOIN GAME";
				public static LocString DISCORD_INFO = "Join ONI Together\non Discord";
			}
			public class PAUSESCREEN
			{
				//maybe add tooltips to these later in some way?
				public class HOSTGAME
				{
					public static LocString LABEL = "Host Game";
					//e.g:
					//public static LocString TOOLTIP = "Host your current game as a multiplayer session";
				}
				public class INVITE
				{
					public static LocString LABEL = "Invite";
				}
				public class DOHARDSYNC
				{
					public static LocString LABEL = "Perform Hard Sync";
				}
				public class HARDSYNCNOTAVAILABLE
				{
					public static LocString LABEL = "Already hard synced this cycle!";
				}
				public class ENDSESSION
				{
					public static LocString LABEL = "End Multiplayer Session";
				}
			}
			public class MP_CHATWINDOW
			{
				public static LocString CHAT_INITIALIZED = "<color=yellow>System:</color> Chat initialized.";
				public class RESIZE
				{
					public static LocString EXPAND = "Chat (+)";
					public static LocString RETRACT = "Chat (-)";
				}
			}
			public class MP_OVERLAY
			{
				public class HOST
				{
					public static LocString STARTINGHOSTING = "Hosting game...";
				}
				public class CLIENT
				{
					public static LocString DOWNLOADING_GAME = "Downloading world: {0}%";
					public static LocString LOST_CONNECTION = "Connection to the host was lost!";
					public static LocString MISSING_SAVE_FILE = "Downloaded save file not found.";
					public static LocString CONNECTING_TO_HOST = "Connecting to {0}!";
					public static LocString WAITING_FOR_PLAYER = "Waiting for {0}...";
				}
				public class SYNC
				{
					public static LocString HARDSYNC_INPROGRESS = "Hard sync in progress!";
					public static LocString FINALIZING_SYNC = "All players are ready!\nPlease wait...";
					public static LocString WAITING_FOR_PLAYERS_SYNC = "Waiting for players ({0}/{1} ready)...\n";
					public class READYSTATE
					{
						public static LocString READY = "Ready";
						public static LocString UNREADY = "Loading";
						public static LocString UNKNOWN = "Unknown";
					}
				}
			}
		}
	}
}
