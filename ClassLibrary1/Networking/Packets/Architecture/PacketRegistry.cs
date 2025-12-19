using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Cloud;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.Packets.DuplicantActions;
using ONI_MP.Networking.Packets.Events;
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

		public static void Register(PacketType type, Func<IPacket> constructor)
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
			try { Register(PacketType.ChoreAssignment, () => new ChoreAssignmentPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ChoreAssignmentPacket: {e}"); }
			try { Register(PacketType.EntityPosition, () => new EntityPositionPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register EntityPositionPacket: {e}"); }
			try { Register(PacketType.ChatMessage, () => new ChatMessagePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ChatMessagePacket: {e}"); }
			try { Register(PacketType.WorldData, () => new WorldDataPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register WorldDataPacket: {e}"); }
			try { Register(PacketType.WorldDataRequest, () => new WorldDataRequestPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register WorldDataRequestPacket: {e}"); }
			try { Register(PacketType.WorldUpdate, () => new WorldUpdatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register WorldUpdatePacket: {e}"); }
			try { Register(PacketType.NavigatorPath, () => new NavigatorPathPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register NavigatorPathPacket: {e}"); }
			try { Register(PacketType.SaveFileRequest, () => new SaveFileRequestPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register SaveFileRequestPacket: {e}"); }
			try { Register(PacketType.SaveFileChunk, () => new SaveFileChunkPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register SaveFileChunkPacket: {e}"); }
			try { Register(PacketType.Diggable, () => new DiggablePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DiggablePacket: {e}"); }
			try { Register(PacketType.DigComplete, () => new DigCompletePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DigCompletePacket: {e}"); }
			try { Register(PacketType.PlayAnim, () => new PlayAnimPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register PlayAnimPacket: {e}"); }
			try { Register(PacketType.Build, () => new BuildPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register BuildPacket: {e}"); }
			try { Register(PacketType.BuildComplete, () => new BuildCompletePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register BuildCompletePacket: {e}"); }
			try { Register(PacketType.WorldDamageSpawnResource, () => new WorldDamageSpawnResourcePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register WorldDamageSpawnResourcePacket: {e}"); }
			try { Register(PacketType.WorldCycle, () => new WorldCyclePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register WorldCyclePacket: {e}"); }
			try { Register(PacketType.Cancel, () => new CancelPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register CancelPacket: {e}"); }
			try { Register(PacketType.Deconstruct, () => new DeconstructPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DeconstructPacket: {e}"); }
			try { Register(PacketType.DeconstructComplete, () => new DeconstructCompletePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DeconstructCompletePacket: {e}"); }
			try { Register(PacketType.WireBuild, () => new UtilityBuildPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register UtilityBuildPacket (WireBuild): {e}"); }
			try { Register(PacketType.ToggleMinionEffect, () => new ToggleMinionEffectPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ToggleMinionEffectPacket: {e}"); }
			try { Register(PacketType.ToolEquip, () => new ToolEquipPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ToolEquipPacket: {e}"); }
			try { Register(PacketType.DuplicantCondition, () => new DuplicantConditionPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DuplicantConditionPacket: {e}"); }
			try { Register(PacketType.MoveToLocation, () => new MoveToLocationPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register MoveToLocationPacket: {e}"); }
			try { Register(PacketType.Prioritize, () => new PrioritizePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register PrioritizePacket: {e}"); }
			try { Register(PacketType.Clear, () => new ClearPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ClearPacket: {e}"); }
			try { Register(PacketType.ClientReadyStatus, () => new ClientReadyStatusPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ClientReadyStatusPacket: {e}"); }
			try { Register(PacketType.ClientReadyStatusUpdate, () => new ClientReadyStatusUpdatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ClientReadyStatusUpdatePacket: {e}"); }
			try { Register(PacketType.AllClientsReady, () => new AllClientsReadyPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register AllClientsReadyPacket: {e}"); }
			try { Register(PacketType.EventTriggered, () => new EventTriggeredPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register EventTriggeredPacket: {e}"); }
			try { Register(PacketType.HardSync, () => new HardSyncPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register HardSyncPacket: {e}"); }
			try { Register(PacketType.HardSyncComplete, () => new HardSyncCompletePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register HardSyncCompletePacket: {e}"); }
			try { Register(PacketType.Disinfect, () => new DisinfectPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DisinfectPacket: {e}"); }
			try { Register(PacketType.SpeedChange, () => new SpeedChangePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register SpeedChangePacket: {e}"); }
			try { Register(PacketType.PlayerCursor, () => new PlayerCursorPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register PlayerCursorPacket: {e}"); }
			try { Register(PacketType.GoogleDriveFileShare, () => new GoogleDriveFileSharePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register GoogleDriveFileSharePacket: {e}"); }
			try { Register(PacketType.BuildingState, () => new BuildingStatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register BuildingStatePacket: {e}"); }
			try { Register(PacketType.DiggingState, () => new DiggingStatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DiggingStatePacket: {e}"); }
			try { Register(PacketType.ChoreState, () => new ChoreStatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ChoreStatePacket: {e}"); }
			try { Register(PacketType.ResearchState, () => new ResearchStatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ResearchStatePacket: {e}"); }
			try { Register(PacketType.PrioritizeState, () => new PrioritizeStatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register PrioritizeStatePacket: {e}"); }
			try { Register(PacketType.DisinfectState, () => new DisinfectStatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DisinfectStatePacket: {e}"); }
			try { Register(PacketType.DuplicantState, () => new DuplicantStatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DuplicantStatePacket: {e}"); }
			try { Register(PacketType.StructureState, () => new StructureStatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register StructureStatePacket: {e}"); }
			try { Register(PacketType.ResearchRequest, () => new ResearchRequestPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ResearchRequestPacket: {e}"); }
			try { Register(PacketType.BuildingConfig, () => new BuildingConfigPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register BuildingConfigPacket: {e}"); }
			try { Register(PacketType.ImmigrantOptions, () => new ImmigrantOptionsPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ImmigrantOptionsPacket: {e}"); }
			try { Register(PacketType.ImmigrantSelection, () => new ImmigrantSelectionPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ImmigrantSelectionPacket: {e}"); }
			try { Register(PacketType.DuplicantPriority, () => new DuplicantPriorityPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register DuplicantPriorityPacket: {e}"); }
			try { Register(PacketType.SkillMastery, () => new SkillMasteryPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register SkillMasteryPacket: {e}"); }
			try { Register(PacketType.ScheduleUpdate, () => new ScheduleUpdatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ScheduleUpdatePacket: {e}"); }
			try { Register(PacketType.ScheduleAssignment, () => new ScheduleAssignmentPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ScheduleAssignmentPacket: {e}"); }
			try { Register(PacketType.FallingObject, () => new FallingObjectPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register FallingObjectPacket: {e}"); }
			try { Register(PacketType.ConsumablePermission, () => new ConsumablePermissionPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ConsumablePermissionPacket: {e}"); }
			try { Register(PacketType.VitalStats, () => new VitalStatsPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register VitalStatsPacket: {e}"); }
			try { Register(PacketType.ResourceCount, () => new ResourceCountPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ResourceCountPacket: {e}"); }
			try { Register(PacketType.Notification, () => new ONI_MP.Networking.Packets.Events.NotificationPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register NotificationPacket: {e}"); }
			try { Register(PacketType.ScheduleDelete, () => new ScheduleDeletePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ScheduleDeletePacket: {e}"); }
			try { Register(PacketType.ConsumableState, () => new ONI_MP.Networking.Packets.DuplicantActions.ConsumableStatePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ConsumableStatePacket: {e}"); }
			try { Register(PacketType.ResearchProgress, () => new ONI_MP.Networking.Packets.World.ResearchProgressPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ResearchProgressPacket: {e}"); }
			try { Register(PacketType.ResearchComplete, () => new ONI_MP.Networking.Packets.World.ResearchCompletePacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register ResearchCompletePacket: {e}"); }
			try { Register(PacketType.EntitySpawn, () => new ONI_MP.Networking.Packets.World.EntitySpawnPacket()); } catch (Exception e) { DebugConsole.LogError($"Failed to register EntitySpawnPacket: {e}"); }
		}
	}
}
