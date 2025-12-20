// Keep this to only windows, Mac is not built with the Devtool framework so it doesn't have access to the DevTool class and just crashes
#if DEBUG //OS_WINDOWS || DEBUG

using System;
using System.Diagnostics;
using System.IO;
using ImGuiNET;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Components;
using UnityEngine;
using static STRINGS.UI;
using Steamworks;

namespace ONI_MP.DebugTools
{
    public class DevToolMultiplayer : DevTool
    {
        private Vector2 scrollPos = Vector2.zero;
        DebugConsole console = null;

        // Player color
        private bool useRandomColor = false;
        private Vector3 playerColor = new Vector3(1f, 1f, 1f);

        // Alert popup
        private bool showRestartPrompt = false;

        // Open player profile
        private CSteamID? selectedPlayer = null;

        private static readonly string ModDirectory = Path.Combine(
            Path.GetDirectoryName(typeof(DevToolMultiplayer).Assembly.Location),
            "oni_mp.dll"
        );

        public DevToolMultiplayer()
        {
            Name = "Multiplayer";
            RequiresGameRunning = false;
            console = DebugConsole.Init();

            ColorRGB loadedColor = Configuration.GetClientProperty<ColorRGB>("PlayerColor");
            playerColor = new Vector3(loadedColor.R / 255, loadedColor.G / 255, loadedColor.B / 255);
            useRandomColor = Configuration.GetClientProperty<bool>("UseRandomPlayerColor");

            OnInit += () => Init();
            OnUpdate += () => Update();
            OnUninit += () => UnInit();
        }

        void Init()
        {

        }

        void Update()
        {

        }

        void UnInit()
        {

        }

		public override void RenderTo(DevPanel panel)
        {
            // Begin scroll region
            ImGui.BeginChild("ScrollRegion", new Vector2(0, 0), true, ImGuiWindowFlags.HorizontalScrollbar);

            if (ImGui.Button("Open Mod Directory"))
            {
                string dir = Path.GetDirectoryName(ModDirectory);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
            ImGui.SameLine();
            if (ImGui.Button("Toggle Debug Console"))
            {
                console?.Toggle();
                Debug.Log("Toggled Debug Console");
            }
            console?.ShowWindow();

            ImGui.NewLine();
            ImGui.Separator();

            if (ImGui.CollapsingHeader("Player Color"))
            {
                if (ImGui.Checkbox("Use Random Color", ref useRandomColor))
                {
                    Configuration.SetClientProperty<bool>("UseRandomPlayerColor", useRandomColor);
                }

                if (ImGui.ColorPicker3("Player Color", ref playerColor))
                {
                    ColorRGB colorRGB = new ColorRGB();
                    colorRGB.R = (byte)(255 * playerColor.x);
                    colorRGB.G = (byte)(255 * playerColor.y);
                    colorRGB.B = (byte)(255 * playerColor.z);
                    Configuration.SetClientProperty<ColorRGB>("PlayerColor", colorRGB);
                }
            }

            // Multiplayer status section
            ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), "Multiplayer Active");

            if (ImGui.Button("Create Lobby"))
            {
                SteamLobby.CreateLobby(onSuccess: () =>
                {
                    SpeedControlScreen.Instance?.Unpause(false);
                });
            }

            ImGui.SameLine();
            if (ImGui.Button("Leave Lobby"))
            {
                SteamLobby.LeaveLobby();
            }

            ImGui.NewLine();
            if (ImGui.Button("Client Disconnect"))
            {
                GameClient.CacheCurrentServer();
                GameClient.Disconnect();
            }

            ImGui.SameLine();
            if (ImGui.Button("Reconnect"))
            {
                GameClient.ReconnectFromCache();
            }

            ImGui.NewLine();
            ImGui.Separator();

            ImGui.Text("Session details:");
            ImGui.Text($"Connected clients: {(MultiplayerSession.InSession ? (MultiplayerSession.PlayerCursors.Count + 1) : 0)}");
            ImGui.Text($"Is Host: {MultiplayerSession.IsHost}");
            ImGui.Text($"Is Client: {MultiplayerSession.IsClient}");
            ImGui.Text($"In Session: {MultiplayerSession.InSession}");
            ImGui.Text($"Local ID: {MultiplayerSession.LocalSteamID}");
            ImGui.Text($"Host ID: {MultiplayerSession.HostSteamID}");

            ImGui.Separator();

            try
            {
                if (MultiplayerSession.InSession)
                {
                    if (!MultiplayerSession.IsHost)
                    {
                        int? ping = GameClient.GetPingToHost();
                        string pingDisplay = ping >= 0 ? $"{ping} ms" : "Pending...";
                        ImGui.Text($"Ping to Host: {pingDisplay}");
                    }
                    else
                    {
                        ImGui.Text("Hosting multiplayer session.");
                        if (ImGui.Button("Test Hard Sync"))
                        {
                            GameServerHardSync.PerformHardSync();
                        }
                    }

                    ImGui.Separator();
                    
                    DrawPlayerList();
                }
                else
                {
                    ImGui.Text("Not in a multiplayer session.");
                }
            }
            catch (Exception e)
            {
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), $"Error: {e.Message}");
            }

            ImGui.EndChild();
        }

        private void DrawPlayerList()
        {
            var players = SteamLobby.GetAllLobbyMembers();

            ImGui.Separator();
            ImGui.Text("Players in Lobby:");

            string self = $"[YOU] {SteamFriends.GetPersonaName()} ({MultiplayerSession.LocalSteamID})";

            if (players.Count == 0)
            {
                ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), self);
                return;
            }

            if (MultiplayerSession.HostSteamID == MultiplayerSession.LocalSteamID)
                self = $"[YOU|HOST] {SteamFriends.GetPersonaName()} ({MultiplayerSession.LocalSteamID})";

            ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), self);

            foreach (var playerId in players)
            {
                string playerName = SteamFriends.GetFriendPersonaName(playerId);
                bool isHost = MultiplayerSession.HostSteamID == playerId;

                string label = isHost
                    ? $"[HOST] {playerName} ({playerId})"
                    : $"{playerName} ({playerId})";

                bool isSelected = selectedPlayer.HasValue && selectedPlayer.Value == playerId;

                if (ImGui.Selectable(label, isSelected))
                {
                    selectedPlayer = playerId;
                }

                // Right-click context menu
                if (ImGui.BeginPopupContextItem(playerId.ToString()))
                {
                    if (ImGui.MenuItem("Open Steam Profile"))
                    {
                        SteamFriends.ActivateGameOverlayToUser("steamid", playerId);
                    }

                    ImGui.EndPopup();
                }
            }
        }

        private void ShowAlertPrompt(
            ref bool isVisible,
            string title,
            string message,
            string confirmText,
            string cancelText,
            System.Action onConfirmAction)
        {
            if (ImGui.BeginPopupModal(title, ref isVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(message);
                ImGui.Separator();

                if (ImGui.Button(confirmText, new Vector2(150, 0)))
                {
                    isVisible = false;

                    onConfirmAction.Invoke();
                }

                ImGui.SameLine();
                if (ImGui.Button(cancelText, new Vector2(150, 0)))
                {
                    isVisible = false;
                }
                ImGui.EndPopup();
            }
        }
    }
}
#endif