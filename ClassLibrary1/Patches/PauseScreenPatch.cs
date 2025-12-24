using HarmonyLib;
using JetBrains.Annotations;
using ONI_MP.Networking;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace ONI_MP.Patches
{
	[HarmonyPatch]
	public static class PauseScreenPatch
	{
		// This method is called when "Quit" is confirmed in the pause menu
		[HarmonyPatch(typeof(PauseScreen), "OnQuitConfirm")]
		[HarmonyPrefix]
		[UsedImplicitly]
		public static void OnQuitConfirm_Prefix(bool saveFirst)
		{
			if (MultiplayerSession.InSession)
			{
				SteamLobby.LeaveLobby();
				MultiplayerSession.Clear();
			}
		}

		// This prevents the game from pausing when the PauseScreen opens in multiplayer
		[HarmonyPatch(typeof(SpeedControlScreen), nameof(SpeedControlScreen.Pause))]
		[HarmonyPrefix]
		[UsedImplicitly]
		public static bool PreventPauseInMultiplayer(bool playSound = true, bool isCrashed = false)
		{
			// Restore pause functionality
			/*if (MultiplayerSession.InSession && !isCrashed)
			{
					return false;
			}*/

			return true;
		}

		[HarmonyPatch(typeof(PauseScreen), "ConfigureButtonInfos")]
		public static class PauseScreen_AddInviteButton
		{
			static void Postfix(PauseScreen __instance)
			{
                var buttonsField = AccessTools.Field(typeof(KModalButtonMenu), "buttons");
                var buttonInfos = ((KModalButtonMenu.ButtonInfo[])buttonsField.GetValue(__instance))?.ToList()
                                                    ?? new List<KModalButtonMenu.ButtonInfo>();

                // Only in multiplayer
                if (!MultiplayerSession.InSession)
				{
					AddButton(__instance, "Host Game", () =>
					{
                        SteamLobby.CreateLobby(onSuccess: () =>
                        {
                            PauseScreen.Instance.Show(false); // Hide pause screen
                            SpeedControlScreen.Instance?.Unpause(false);
                        });
                    });
                    return;
				}

				if (MultiplayerSession.IsHost)
				{
                    AddButton(__instance, "Invite", () =>
                    {
                        SteamFriends.ActivateGameOverlayInviteDialog(SteamLobby.CurrentLobby); // Whilst the menu opens, sending an invite this way doesn't work
                    });

					if (!GameServerHardSync.hardSyncDoneThisCycle)
					{
                        AddButton(__instance, "Perform Hard Sync", () =>
                        {
							if (MultiplayerSession.ConnectedPlayers.Count > 0)
							{
								PauseScreen.Instance.Show(false); // Hide pause screen
								SpeedControlScreen.Instance?.Unpause(false);
								GameServerHardSync.PerformHardSync(); // Manually trigger the hard sync
							} else
							{
                                PauseScreen.Instance.Show(false); // Hide pause screen
                                SpeedControlScreen.Instance?.Unpause(false);
								GameServerHardSync.hardSyncDoneThisCycle = true; // No one is here, skip hard sync
                            }
                        });
                    } else
					{
                        AddButton(__instance, "Already hard synced this cycle!", () =>
                        {
                            
                        });
                    }

					AddButton(__instance, "End Multiplayer Session", () =>
					{
						SteamLobby.LeaveLobby();
						PauseScreen.Instance.Show(false); // Hide pause screen
						SpeedControlScreen.Instance?.Unpause(false);
					}, "Invite");
				}

            }
		}

		private static void AddButton(PauseScreen __instance, string label, System.Action onClicked, string placeAfter = "Resume")
		{
            var buttonsField = AccessTools.Field(typeof(KModalButtonMenu), "buttons");
            var buttonInfos = ((KModalButtonMenu.ButtonInfo[])buttonsField.GetValue(__instance))?.ToList()
                                                ?? new List<KModalButtonMenu.ButtonInfo>();

            if (buttonInfos.Any(b => b.text == label))
                return; // Ignore duplicates

            int id_x = buttonInfos.FindIndex(b => b.text == placeAfter) + 1;
            if (id_x <= 0) id_x = 1;

            buttonInfos.Insert(id_x, new KModalButtonMenu.ButtonInfo(
                    label,
                    new UnityAction(() =>
                    {
						onClicked.Invoke();
                    })
            ));

            buttonsField.SetValue(__instance, buttonInfos.ToArray());
        }
	}
}
