using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Cloud;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.Packets.DuplicantActions;
using ONI_MP.Networking.Packets.Events;
using ONI_MP.Networking.Packets.Handshake;
using ONI_MP.Networking.Packets.Social;
using ONI_MP.Networking.Packets.Tools.Build;
using ONI_MP.Networking.Packets.Tools.Cancel;
using ONI_MP.Networking.Packets.Tools.Clear;
using ONI_MP.Networking.Packets.Tools.Deconstruct;
using ONI_MP.Networking.Packets.Tools.Dig;
using ONI_MP.Networking.Packets.Tools.Disinfect;
using ONI_MP.Networking.Packets.Tools.Move;
using ONI_MP.Networking.Packets.Tools.Prioritize;
using ONI_MP.Networking.Packets.World;
using System;
using System.Collections.Generic;

namespace ONI_MP.Networking.Packets.Architecture
{
	public static class PacketRegistry
	{
		private static readonly Dictionary<PacketType, Func<IPacket>> _constructors = new Dictionary<PacketType, Func<IPacket>>();

		private static void Register(PacketType type, Func<IPacket> constructor)
		{
			_constructors[type] = constructor;
		}

		public static IPacket Create(PacketType type)
		{
			return _constructors.TryGetValue(type, out var ctor)
					? ctor()
					: throw new InvalidOperationException($"No packet registered for type {type}");
		}

		public static void RegisterDefaults()
		{
            TryRegister(PacketType.ChoreAssignment, () => new ChoreAssignmentPacket());
            TryRegister(PacketType.EntityPosition, () => new EntityPositionPacket());
            TryRegister(PacketType.ChatMessage, () => new ChatMessagePacket());
            TryRegister(PacketType.WorldData, () => new WorldDataPacket());
            TryRegister(PacketType.WorldDataRequest, () => new WorldDataRequestPacket());
            TryRegister(PacketType.WorldUpdate, () => new WorldUpdatePacket());
            TryRegister(PacketType.NavigatorPath, () => new NavigatorPathPacket());
            TryRegister(PacketType.SaveFileRequest, () => new SaveFileRequestPacket());
            TryRegister(PacketType.SaveFileChunk, () => new SaveFileChunkPacket());
            TryRegister(PacketType.Diggable, () => new DiggablePacket());
            TryRegister(PacketType.DigComplete, () => new DigCompletePacket());
            TryRegister(PacketType.PlayAnim, () => new PlayAnimPacket());
            TryRegister(PacketType.Build, () => new BuildPacket());
            TryRegister(PacketType.BuildComplete, () => new BuildCompletePacket());
            TryRegister(PacketType.WorldDamageSpawnResource, () => new WorldDamageSpawnResourcePacket());
            TryRegister(PacketType.WorldCycle, () => new WorldCyclePacket());
            TryRegister(PacketType.Cancel, () => new CancelPacket());
            TryRegister(PacketType.Deconstruct, () => new DeconstructPacket());
            TryRegister(PacketType.DeconstructComplete, () => new DeconstructCompletePacket());
            TryRegister(PacketType.WireBuild, () => new UtilityBuildPacket(), "UtilityBuildPacket (WireBuild)");
            TryRegister(PacketType.ToggleMinionEffect, () => new ToggleMinionEffectPacket());
            TryRegister(PacketType.ToolEquip, () => new ToolEquipPacket());
            TryRegister(PacketType.DuplicantCondition, () => new DuplicantConditionPacket());
            TryRegister(PacketType.MoveToLocation, () => new MoveToLocationPacket());
            TryRegister(PacketType.Prioritize, () => new PrioritizePacket());
            TryRegister(PacketType.Clear, () => new ClearPacket());
            TryRegister(PacketType.ClientReadyStatus, () => new ClientReadyStatusPacket());
            TryRegister(PacketType.ClientReadyStatusUpdate, () => new ClientReadyStatusUpdatePacket());
            TryRegister(PacketType.AllClientsReady, () => new AllClientsReadyPacket());
            TryRegister(PacketType.EventTriggered, () => new EventTriggeredPacket());
            TryRegister(PacketType.HardSync, () => new HardSyncPacket());
            TryRegister(PacketType.HardSyncComplete, () => new HardSyncCompletePacket());
            TryRegister(PacketType.Disinfect, () => new DisinfectPacket());
            TryRegister(PacketType.SpeedChange, () => new SpeedChangePacket());
            TryRegister(PacketType.PlayerCursor, () => new PlayerCursorPacket());
            TryRegister(PacketType.GoogleDriveFileShare, () => new GoogleDriveFileSharePacket());
            TryRegister(PacketType.BuildingState, () => new BuildingStatePacket());
            TryRegister(PacketType.DiggingState, () => new DiggingStatePacket());
            TryRegister(PacketType.ChoreState, () => new ChoreStatePacket());
            TryRegister(PacketType.ResearchState, () => new ResearchStatePacket());
            TryRegister(PacketType.PrioritizeState, () => new PrioritizeStatePacket());
            TryRegister(PacketType.DisinfectState, () => new DisinfectStatePacket());
            TryRegister(PacketType.DuplicantState, () => new DuplicantStatePacket());
            TryRegister(PacketType.StructureState, () => new StructureStatePacket());
            TryRegister(PacketType.ResearchRequest, () => new ResearchRequestPacket());
            TryRegister(PacketType.BuildingConfig, () => new BuildingConfigPacket());
            TryRegister(PacketType.ImmigrantOptions, () => new ImmigrantOptionsPacket());
            TryRegister(PacketType.ImmigrantSelection, () => new ImmigrantSelectionPacket());
            TryRegister(PacketType.DuplicantPriority, () => new DuplicantPriorityPacket());
            TryRegister(PacketType.SkillMastery, () => new SkillMasteryPacket());
            TryRegister(PacketType.ScheduleUpdate, () => new ScheduleUpdatePacket());
            TryRegister(PacketType.ScheduleAssignment, () => new ScheduleAssignmentPacket());
            TryRegister(PacketType.FallingObject, () => new FallingObjectPacket());
            TryRegister(PacketType.ConsumablePermission, () => new ConsumablePermissionPacket());
            TryRegister(PacketType.VitalStats, () => new VitalStatsPacket());
            TryRegister(PacketType.ResourceCount, () => new ResourceCountPacket());
            TryRegister(PacketType.Notification, () => new NotificationPacket());
            TryRegister(PacketType.ScheduleDelete, () => new ScheduleDeletePacket());
            TryRegister(PacketType.ConsumableState, () => new ConsumableStatePacket());
            TryRegister(PacketType.ResearchProgress, () => new ResearchProgressPacket());
            TryRegister(PacketType.ResearchComplete, () => new ResearchCompletePacket());
            TryRegister(PacketType.EntitySpawn, () => new EntitySpawnPacket());

            // Mod compatibility verification packets
            TryRegister(PacketType.ModVerification, () => new ModVerificationPacket());
            TryRegister(PacketType.ModVerificationResponse, () => new ModVerificationResponsePacket());
            TryRegister(PacketType.ModListRequest, () => new ModListRequestPacket());
		}

		public static void TryRegister(PacketType type, Func<IPacket> constructor, string nameOverride = "")
		{
			try
			{
				Register(type, constructor);
			}
			catch (Exception e)
			{
				string name = string.IsNullOrEmpty(nameOverride) ? constructor.GetType().Name : nameOverride;
				DebugConsole.LogError($"Failed to register {name}: {e}");
			}
		}
	}
}
