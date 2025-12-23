using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;

namespace ONI_MP.DebugTools
{
    public class PacketTracker
    {
        private static PacketTracker _instance;
        private bool showWindow = false;

        private string filter = string.Empty;

        // Used for imgui packet tracking
        public struct PacketTrackData
        {
            public IPacket packet;
            public int size;
        }

        private List<PacketTrackData> tracked = new List<PacketTrackData>();
        private const int MAX_TRACKED_LIMIT = 100;

        public static PacketTracker Init()
        {
            if (_instance != null)
                return _instance;

            _instance = new PacketTracker();
            return _instance;
        }

        public static void TrackSent(PacketTrackData data)
        {
            _instance.tracked.Add(data);

            if (_instance.tracked.Count > MAX_TRACKED_LIMIT)
            {
                int overflow = _instance.tracked.Count - MAX_TRACKED_LIMIT;
                _instance.tracked.RemoveRange(0, overflow);
            }
        }

        public void Clear()
        {
            _instance.tracked.Clear();
        }

        public void Toggle()
        {
            showWindow = !showWindow;
        }

        public void ShowWindow()
        {
            if (!showWindow)
                return;

            if (ImGui.Begin("Packet Tracker", ref showWindow))
            {
                if (!MultiplayerSession.InSession)
                {
                    if (tracked.Count > 0)
                        Clear();

                    ImGui.TextDisabled("Not in a session!");
                }
                else
                {
                    ImGui.InputText("Filter", ref filter, 64);
                    ImGui.Separator();

                    if (ImGui.BeginTable("packet_tracker_table", 3,
                        ImGuiTableFlags.Borders |
                        ImGuiTableFlags.RowBg |
                        ImGuiTableFlags.ScrollY))
                    {
                        ImGui.TableSetupColumn("Packet Type");
                        ImGui.TableSetupColumn("Packet ID");
                        ImGui.TableSetupColumn("Size (bytes)");

                        ImGui.TableHeadersRow();

                        for (int i = tracked.Count - 1; i >= 0; i--)
                        {
                            var entry = tracked[i];

                            string typeName = entry.packet.GetType().Name;
                            string idString = entry.packet.GetType().GetHashCode().ToString();

                            if (!string.IsNullOrEmpty(filter))
                            {
                                bool matchesType =
                                    typeName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                                bool matchesId =
                                    idString.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                                if (!matchesType && !matchesId)
                                    continue;
                            }

                            ImGui.TableNextRow();

                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text(typeName);

                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(idString);

                            ImGui.TableSetColumnIndex(2);
                            ImGui.Text(Utils.FormatBytes(entry.size));
                        }

                        ImGui.EndTable();
                    }
                }
            }

            ImGui.End();
        }

    }
}
