using HarmonyLib;
using JetBrains.Annotations;
using ONI_MP.Networking;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
			public static void Postfix(PauseScreen __instance)
			{
				var buttonInfos = __instance.buttons;

                // Only in multiplayer
                if (!MultiplayerSession.InSession)
				{
					AddButton(__instance, MP_STRINGS.UI.PAUSESCREEN.HOSTGAME.LABEL, () =>
					{
						PauseScreen.Instance.Show(false); // Hide pause screen
						// Show lobby config screen - it will handle lobby creation
						var canvas = Object.FindObjectOfType<Canvas>();
						if (canvas != null)
						{
							ONI_MP.Menus.HostLobbyConfigScreen.Show(canvas.transform, () =>
							{
								// Config closed - create lobby with settings
								SteamLobby.CreateLobby(onSuccess: () =>
								{
									SpeedControlScreen.Instance?.Unpause(false);
								});
							});
						}
                    });
                    return;
				}

				// In multiplayer session - show single Multiplayer button
				AddButton(__instance, MP_STRINGS.UI.PAUSESCREEN.MULTIPLAYER.LABEL, () =>
				{
					PauseScreen.Instance.Show(false); // Hide pause screen
					// Show multiplayer info screen
					var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
					if (canvas != null)
					{
						ONI_MP.Menus.MultiplayerInfoScreen.Show(canvas.transform);
					}
				});
				
				/*
                    AddButton(__instance, MP_STRINGS.UI.PAUSESCREEN.INVITE.LABEL, () =>
                    {
                        SteamFriends.ActivateGameOverlayInviteDialog(SteamLobby.CurrentLobby); // Whilst the menu opens, sending an invite this way doesn't work
                    });

					if (!GameServerHardSync.hardSyncDoneThisCycle)
					{
                        AddButton(__instance, MP_STRINGS.UI.PAUSESCREEN.DOHARDSYNC.LABEL, () =>
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
                        AddButton(__instance, MP_STRINGS.UI.PAUSESCREEN.HARDSYNCNOTAVAILABLE.LABEL, () =>
                        {
                            
                        });
                    }

					AddButton(__instance, MP_STRINGS.UI.PAUSESCREEN.ENDSESSION.LABEL, () =>
					{
						SteamLobby.LeaveLobby();
						PauseScreen.Instance.Show(false); // Hide pause screen
						SpeedControlScreen.Instance?.Unpause(false);
					}, MP_STRINGS.UI.PAUSESCREEN.INVITE.LABEL);
				}*/

            }
		}

		private static void AddButton(PauseScreen __instance, string label, System.Action onClicked, string placeAfter = "Resume")
		{
			var buttonInfos = __instance.buttons.ToList();
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

			__instance.buttons = buttonInfos.ToArray();
        }
	}
}
