using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ONI_MP.Patches.World
{
	/// <summary>
	/// Generic sync helpers for Side Screen UI components.
	/// </summary>
	public static class SideScreenSyncHelper
	{
		public static void SyncSliderChange(Component target, float value, int sliderIndex = 0)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (target == null) return;

			var identity = target.GetComponent<NetworkIdentity>();
			if (identity == null)
			{
				identity = target.gameObject.AddOrGet<NetworkIdentity>();
				identity.RegisterIdentity();
			}

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target.gameObject),
				ConfigHash = "Slider".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.SliderIndex,
				SliderIndex = sliderIndex
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncThresholdChange(GameObject target, float value)
		{
			DebugConsole.Log($"[SideScreenSyncHelper.SyncThresholdChange] Called for {target?.name ?? "null"}, value={value}");
			
			if (BuildingConfigPacket.IsApplyingPacket)
			{
				DebugConsole.Log("[SideScreenSyncHelper.SyncThresholdChange] IsApplyingPacket=true, skipping");
				return;
			}
			if (target == null) return;
			
			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "Threshold".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.Float
			};

			DebugConsole.Log($"[SideScreenSyncHelper.SyncThresholdChange] Sending packet: ConfigHash={packet.ConfigHash}, Value={value}");

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncThresholdDirection(GameObject target, bool activateAbove)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (target == null) return;

			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "ThresholdDir".GetHashCode(),
				Value = activateAbove ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncCheckboxChange(GameObject target, bool value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (target == null) return;

			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "Checkbox".GetHashCode(),
				Value = value ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncCapacityChange(GameObject target, float value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (target == null) return;

			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "Capacity".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}

		public static void SyncDoorState(GameObject target, Door.ControlState state)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (target == null) return;

			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(target),
				ConfigHash = "DoorState".GetHashCode(),
				Value = (float)(int)state,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	[HarmonyPatch(typeof(SingleSliderSideScreen), "SetTarget")]
	public static class SingleSliderSideScreen_SetTarget_Patch
	{
		public static void Postfix(SingleSliderSideScreen __instance, GameObject new_target)
		{
			if (new_target == null) return;
			
			var identity = new_target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var sliderSets = Traverse.Create(__instance).Field("sliderSets").GetValue() as IList;
			if (sliderSets != null)
			{
				for (int i = 0; i < sliderSets.Count; i++)
				{
					var sliderSet = sliderSets[i];
					var slider = Traverse.Create(sliderSet).Field("valueSlider").GetValue<KSlider>();
					var numberInput = Traverse.Create(sliderSet).Field("numberInput").GetValue<KNumberInputField>();
					
					int index = i;
					if (slider != null)
					{
						slider.onReleaseHandle -= () => OnSliderReleased(new_target, slider, index);
						slider.onReleaseHandle += () => OnSliderReleased(new_target, slider, index);
					}
					if (numberInput != null)
					{
						numberInput.onEndEdit -= () => OnInputEndEdit(new_target, numberInput, index);
						numberInput.onEndEdit += () => OnInputEndEdit(new_target, numberInput, index);
					}
				}
			}
		}

		private static void OnSliderReleased(GameObject target, KSlider slider, int index)
		{
			float value = slider.value;
			// Rounding for generators that use integer percentages
			if (ShouldRoundValue(target)) value = Mathf.Round(value);
			Send(target, value, index);
		}

		private static void OnInputEndEdit(GameObject target, KNumberInputField input, int index)
		{
			float value = input.currentValue;
			if (ShouldRoundValue(target)) value = Mathf.Round(value);
			Send(target, value, index);
		}

		private static bool ShouldRoundValue(GameObject target)
		{
			// ManualGenerator, EnergyGenerator (Coal), WoodGasGenerator all need rounding
			return target.GetComponent<ManualGenerator>() != null ||
			       target.GetComponent<EnergyGenerator>() != null;
		}

		private static void Send(GameObject target, float value, int index)
		{
			var comp = target.GetComponent<ISliderControl>() as Component;
			if (comp == null) comp = target.GetComponent<ISingleSliderControl>() as Component;
			if (comp != null) SideScreenSyncHelper.SyncSliderChange(comp, value, index);
		}
	}

	[HarmonyPatch(typeof(IntSliderSideScreen), "SetTarget")]
	public static class IntSliderSideScreen_SetTarget_Patch
	{
		public static void Postfix(IntSliderSideScreen __instance, GameObject new_target)
		{
			if (new_target == null) return;
			
			var identity = new_target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var sliderSets = Traverse.Create(__instance).Field("sliderSets").GetValue() as IList;
			if (sliderSets != null)
			{
				for (int i = 0; i < sliderSets.Count; i++)
				{
					var sliderSet = sliderSets[i];
					var slider = Traverse.Create(sliderSet).Field("valueSlider").GetValue<KSlider>();
					var numberInput = Traverse.Create(sliderSet).Field("numberInput").GetValue<KNumberInputField>();
					
					int index = i;
					if (slider != null)
					{
						slider.onReleaseHandle -= () => OnSliderReleased(new_target, slider, index);
						slider.onReleaseHandle += () => OnSliderReleased(new_target, slider, index);
					}
					if (numberInput != null)
					{
						numberInput.onEndEdit -= () => OnInputEndEdit(new_target, numberInput, index);
						numberInput.onEndEdit += () => OnInputEndEdit(new_target, numberInput, index);
					}
				}
			}
		}

		private static void OnSliderReleased(GameObject target, KSlider slider, int index)
		{
			Send(target, Mathf.Round(slider.value), index);
		}

		private static void OnInputEndEdit(GameObject target, KNumberInputField input, int index)
		{
			Send(target, Mathf.Round(input.currentValue), index);
		}

		private static void Send(GameObject target, float value, int index)
		{
			var comp = target.GetComponent<ISliderControl>() as Component;
			if (comp == null) comp = target.GetComponent<ISingleSliderControl>() as Component;
			if (comp != null) SideScreenSyncHelper.SyncSliderChange(comp, value, index);
		}
	}

  [HarmonyPatch(typeof(SingleCheckboxSideScreen), nameof(SingleCheckboxSideScreen.SetTarget))]
	public static class SingleCheckboxSideScreen_SetTarget_Patch
	{
		public static void Postfix(SingleCheckboxSideScreen __instance, GameObject target)
		{
			if (target == null) return;

			var identity = target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var checkboxToggle = __instance.toggle;
			if (checkboxToggle != null)
			{
				checkboxToggle.onValueChanged -= (action) => OnCheckboxClicked(target, action);
				checkboxToggle.onValueChanged += (action) => OnCheckboxClicked(target, action);
			}
		}

		private static void OnCheckboxClicked(GameObject target, bool value)
		{
			SideScreenSyncHelper.SyncCheckboxChange(target, value);
		}
	}

	/// <summary>
	/// Sync threshold value changes (e.g., temperature/pressure sensors)
	/// </summary>
	[HarmonyPatch(typeof(ThresholdSwitchSideScreen), nameof(ThresholdSwitchSideScreen.UpdateThresholdValue))]
	public static class ThresholdSwitchSideScreen_UpdateThresholdValue_Patch
	{
		public static void Postfix(ThresholdSwitchSideScreen __instance, float newValue)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (__instance.target == null) return;

			SideScreenSyncHelper.SyncThresholdChange(__instance.target, newValue);
		}
	}

	/// <summary>
	/// Sync threshold direction changes (above/below)
	/// </summary>
	[HarmonyPatch(typeof(ThresholdSwitchSideScreen), nameof(ThresholdSwitchSideScreen.OnConditionButtonClicked))]
	public static class ThresholdSwitchSideScreen_OnConditionButtonClicked_Patch
	{
		public static void Postfix(ThresholdSwitchSideScreen __instance, bool activate_above_threshold)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (__instance.target == null) return;

			SideScreenSyncHelper.SyncThresholdDirection(__instance.target, activate_above_threshold);
		}
	}

	/// <summary>
	/// Register NetworkIdentity when the side screen is opened
	/// </summary>
	[HarmonyPatch(typeof(ThresholdSwitchSideScreen), nameof(ThresholdSwitchSideScreen.SetTarget))]
	public static class ThresholdSwitchSideScreen_SetTarget_Patch
	{
		public static void Postfix(ThresholdSwitchSideScreen __instance, GameObject new_target)
		{
			if (new_target == null) return;
			var identity = new_target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();
		}
	}

	/// <summary>
	/// Sync door state changes (Open/Close/Auto)
	/// </summary>
	[HarmonyPatch(typeof(Door), "QueueStateChange")]
	public static class Door_QueueStateChange_Patch
	{
		public static void Postfix(Door __instance, Door.ControlState nextState)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			SideScreenSyncHelper.SyncDoorState(__instance.gameObject, nextState);
		}
	}

	/// <summary>
	/// Sync capacity changes from CapacityControlSideScreen
	/// </summary>
	[HarmonyPatch(typeof(CapacityControlSideScreen), nameof(CapacityControlSideScreen.UpdateMaxCapacity))]
	public static class CapacityControlSideScreen_UpdateMaxCapacity_Patch
	{
		public static void Postfix(CapacityControlSideScreen __instance, float newValue)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (__instance.target == null) return;

			// Get the GameObject from the IUserControlledCapacity target
			var targetComponent = __instance.target as Component;
			if (targetComponent != null)
			{
				SideScreenSyncHelper.SyncCapacityChange(targetComponent.gameObject, newValue);
			}
		}
	}

	/// <summary>
	/// Register NetworkIdentity when capacity side screen is opened
	/// </summary>
	[HarmonyPatch(typeof(CapacityControlSideScreen), nameof(CapacityControlSideScreen.SetTarget))]
	public static class CapacityControlSideScreen_SetTarget_Patch
	{
		public static void Postfix(CapacityControlSideScreen __instance, GameObject new_target)
		{
			if (new_target == null) return;
			var identity = new_target.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();
		}
	}

	/// <summary>
	/// Sync critter sensor checkbox toggles
	/// </summary>
	[HarmonyPatch(typeof(CritterSensorSideScreen), nameof(CritterSensorSideScreen.ToggleCritters))]
	public static class CritterSensorSideScreen_ToggleCritters_Patch
	{
		public static void Postfix(CritterSensorSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (__instance.targetSensor == null) return;

			var identity = __instance.targetSensor.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetSensor.gameObject),
				ConfigHash = "CritterCountCritters".GetHashCode(),
				Value = __instance.targetSensor.countCritters ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	[HarmonyPatch(typeof(CritterSensorSideScreen), nameof(CritterSensorSideScreen.ToggleEggs))]
	public static class CritterSensorSideScreen_ToggleEggs_Patch
	{
		public static void Postfix(CritterSensorSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (__instance.targetSensor == null) return;

			var identity = __instance.targetSensor.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetSensor.gameObject),
				ConfigHash = "CritterCountEggs".GetHashCode(),
				Value = __instance.targetSensor.countEggs ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync timer sensor settings via copy/paste
	/// </summary>
	[HarmonyPatch(typeof(LogicTimerSensor), nameof(LogicTimerSensor.OnCopySettings))]
	public static class LogicTimerSensor_OnCopySettings_Patch
	{
		public static void Postfix(LogicTimerSensor __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Sync both on and off durations
			var packetOn = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "TimerOnDuration".GetHashCode(),
				Value = __instance.onDuration,
				ConfigType = BuildingConfigType.Float
			};
			var packetOff = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "TimerOffDuration".GetHashCode(),
				Value = __instance.offDuration,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(packetOn);
				PacketSender.SendToAllClients(packetOff);
			}
			else
			{
				PacketSender.SendToHost(packetOn);
				PacketSender.SendToHost(packetOff);
			}
		}
	}

	// NOTE: These property setter patches may fail if the target is a field
	// Commented out until we can verify the class definitions
	// [HarmonyPatch(typeof(LogicTimeOfDaySensor), "startTime", MethodType.Setter)]
	// public static class LogicTimeOfDaySensor_StartTime_Patch
	// {
	// 	public static void Postfix(LogicTimeOfDaySensor __instance, float value)
	// 	{
	// 		if (BuildingConfigPacket.IsApplyingPacket) return;
	// 		if (!MultiplayerSession.InSession) return;
	// 
	// 		var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
	// 		identity.RegisterIdentity();
	// 
	// 		var packet = new BuildingConfigPacket
	// 		{
	// 			NetId = identity.NetId,
	// 			Cell = Grid.PosToCell(__instance.gameObject),
	// 			ConfigHash = "StartTime".GetHashCode(),
	// 			Value = value,
	// 			ConfigType = BuildingConfigType.Float
	// 		};
	// 
	// 		if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
	// 		else PacketSender.SendToHost(packet);
	// 	}
	// }

	// [HarmonyPatch(typeof(LogicTimeOfDaySensor), "duration", MethodType.Setter)]
	// public static class LogicTimeOfDaySensor_Duration_Patch
	// {
	// 	public static void Postfix(LogicTimeOfDaySensor __instance, float value)
	// 	{
	// 		if (BuildingConfigPacket.IsApplyingPacket) return;
	// 		if (!MultiplayerSession.InSession) return;
	// 
	// 		var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
	// 		identity.RegisterIdentity();
	// 
	// 		var packet = new BuildingConfigPacket
	// 		{
	// 			NetId = identity.NetId,
	// 			Cell = Grid.PosToCell(__instance.gameObject),
	// 			ConfigHash = "Duration".GetHashCode(),
	// 			Value = value,
	// 			ConfigType = BuildingConfigType.Float
	// 		};
	// 
	// 		if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
	// 		else PacketSender.SendToHost(packet);
	// 	}
	// }

	// [HarmonyPatch(typeof(LimitValve), "Limit", MethodType.Setter)]
	// public static class LimitValve_Limit_Patch
	// {
	// 	public static void Postfix(LimitValve __instance, float value)
	// 	{
	// 		if (BuildingConfigPacket.IsApplyingPacket) return;
	// 		if (!MultiplayerSession.InSession) return;
	// 
	// 		var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
	// 		identity.RegisterIdentity();
	// 
	// 		var packet = new BuildingConfigPacket
	// 		{
	// 			NetId = identity.NetId,
	// 			Cell = Grid.PosToCell(__instance.gameObject),
	// 			ConfigHash = "LimitValve".GetHashCode(),
	// 			Value = value,
	// 			ConfigType = BuildingConfigType.Float
	// 		};
	// 
	// 		if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
	// 		else PacketSender.SendToHost(packet);
	// 	}
	// }

	/// <summary>
	/// Sync timer sensor settings via direct slider changes
	/// </summary>
	[HarmonyPatch(typeof(TimerSideScreen), nameof(TimerSideScreen.ChangeSetting))]
	public static class TimerSideScreen_ChangeSetting_Patch
	{
		public static void Postfix(TimerSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetTimedSwitch == null) return;

			var identity = __instance.targetTimedSwitch.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Sync both on and off durations
			var packetOn = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetTimedSwitch.gameObject),
				ConfigHash = "TimerOnDuration".GetHashCode(),
				Value = __instance.targetTimedSwitch.onDuration,
				ConfigType = BuildingConfigType.Float
			};
			var packetOff = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetTimedSwitch.gameObject),
				ConfigHash = "TimerOffDuration".GetHashCode(),
				Value = __instance.targetTimedSwitch.offDuration,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(packetOn);
				PacketSender.SendToAllClients(packetOff);
			}
			else
			{
				PacketSender.SendToHost(packetOn);
				PacketSender.SendToHost(packetOff);
			}
		}
	}

	/// <summary>
	/// Sync filter element selection
	/// </summary>
	[HarmonyPatch(typeof(FilterSideScreen), nameof(FilterSideScreen.SetFilterTag))]
	public static class FilterSideScreen_SetFilterTag_Patch
	{
		public static void Postfix(FilterSideScreen __instance, Tag tag)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetFilterable == null) return;

			var targetGO = (__instance.targetFilterable as Component)?.gameObject;
			if (targetGO == null) return;

			var identity = targetGO.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Use string-based tag name instead of hash for proper reconstruction
			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(targetGO),
				ConfigHash = "FilterTagString".GetHashCode(),
				Value = 0,
				ConfigType = BuildingConfigType.String,
				StringValue = tag.Name
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync signal counter max count
	/// </summary>
	[HarmonyPatch(typeof(CounterSideScreen), nameof(CounterSideScreen.SetMaxCount))]
	public static class CounterSideScreen_SetMaxCount_Patch
	{
		public static void Postfix(CounterSideScreen __instance, int newValue)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetLogicCounter == null) return;

			var identity = __instance.targetLogicCounter.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetLogicCounter.gameObject),
				ConfigHash = "CounterMaxCount".GetHashCode(),
				Value = newValue,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync signal counter advanced mode toggle
	/// </summary>
	[HarmonyPatch(typeof(CounterSideScreen), nameof(CounterSideScreen.ToggleAdvanced))]
	public static class CounterSideScreen_ToggleAdvanced_Patch
	{
		public static void Postfix(CounterSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetLogicCounter == null) return;

			var identity = __instance.targetLogicCounter.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetLogicCounter.gameObject),
				ConfigHash = "CounterAdvanced".GetHashCode(),
				Value = __instance.targetLogicCounter.advancedMode ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync signal counter reset
	/// </summary>
	[HarmonyPatch(typeof(CounterSideScreen), nameof(CounterSideScreen.ResetCounter))]
	public static class CounterSideScreen_ResetCounter_Patch
	{
		public static void Postfix(CounterSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetLogicCounter == null) return;

			var identity = __instance.targetLogicCounter.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetLogicCounter.gameObject),
				ConfigHash = "CounterReset".GetHashCode(),
				Value = 1f,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync storage filter add tag
	/// </summary>
	[HarmonyPatch(typeof(TreeFilterable), nameof(TreeFilterable.AddTagToFilter))]
	public static class TreeFilterable_AddTagToFilter_Patch
	{
		public static void Postfix(TreeFilterable __instance, Tag t)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "StorageFilterAdd".GetHashCode(),
				Value = 0,
				ConfigType = BuildingConfigType.String,
				StringValue = t.Name
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync storage filter remove tag
	/// </summary>
	[HarmonyPatch(typeof(TreeFilterable), nameof(TreeFilterable.RemoveTagFromFilter))]
	public static class TreeFilterable_RemoveTagFromFilter_Patch
	{
		public static void Postfix(TreeFilterable __instance, Tag t)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "StorageFilterRemove".GetHashCode(),
				Value = 0,
				ConfigType = BuildingConfigType.String,
				StringValue = t.Name
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}
  
  /// <summary>
  /// Sync storage sweep-only toggle
  /// </summary>
  [HarmonyPatch(typeof(Storage), nameof(Storage.SetOnlyFetchMarkedItems))]
	public static class Storage_SetOnlyFetchMarkedItems_Patch
	{
    public static void Postfix(Storage __instance, bool is_set)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "StorageSweepOnly".GetHashCode(),
				Value = is_set ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync fabricator recipe queue changes
	/// </summary>
	[HarmonyPatch(typeof(ComplexFabricator), nameof(ComplexFabricator.SetRecipeQueueCount))]
	public static class ComplexFabricator_SetRecipeQueueCount_Patch
	{
		public static void Postfix(ComplexFabricator __instance, ComplexRecipe recipe, int count)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Use recipe ID hash and pack count into Value
			// We'll send recipe hash in ConfigHash and count in Value
			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = recipe.id.GetHashCode(),
				Value = count,
				ConfigType = BuildingConfigType.RecipeQueue
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	// NOTE: ComplexFabricator.ForbidMutantSeeds may not be patchable via setter
	// [HarmonyPatch(typeof(ComplexFabricator), "ForbidMutantSeeds", MethodType.Setter)]
	// public static class ComplexFabricator_ForbidMutantSeeds_Patch
	// {
	// 	public static void Postfix(ComplexFabricator __instance, bool value)
	// 	{
	// 		if (BuildingConfigPacket.IsApplyingPacket) return;
	// 		if (!MultiplayerSession.InSession) return;
	// 
	// 		var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
	// 		identity.RegisterIdentity();
	// 
	// 		var packet = new BuildingConfigPacket
	// 		{
	// 			NetId = identity.NetId,
	// 			Cell = Grid.PosToCell(__instance.gameObject),
	// 			ConfigHash = "FabricatorMutantSeeds".GetHashCode(),
	// 			Value = value ? 1f : 0f,
	// 			ConfigType = BuildingConfigType.Boolean
	// 		};
	// 
	// 		if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
	// 		else PacketSender.SendToHost(packet);
	// 	}
	// }

	/// <summary>
	/// Sync LogicAlarm (Automated Notifier) settings including text fields
	/// </summary>
	[HarmonyPatch(typeof(LogicAlarm), nameof(LogicAlarm.OnCopySettings))]
	public static class LogicAlarm_OnCopySettings_Patch
	{
		public static void Postfix(LogicAlarm __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Sync notification type
			var packetType = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "AlarmNotificationType".GetHashCode(),
				Value = (int)__instance.notificationType,
				ConfigType = BuildingConfigType.Float
			};

			// Sync pause on notify
			var packetPause = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "AlarmPauseOnNotify".GetHashCode(),
				Value = __instance.pauseOnNotify ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			// Sync zoom on notify
			var packetZoom = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "AlarmZoomOnNotify".GetHashCode(),
				Value = __instance.zoomOnNotify ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			// Sync notification name (text)
			var packetName = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "AlarmNotificationName".GetHashCode(),
				Value = 0,
				ConfigType = BuildingConfigType.String,
				StringValue = __instance.notificationName ?? ""
			};

			// Sync notification tooltip (text)
			var packetTooltip = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "AlarmNotificationTooltip".GetHashCode(),
				Value = 0,
				ConfigType = BuildingConfigType.String,
				StringValue = __instance.notificationTooltip ?? ""
			};

			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(packetType);
				PacketSender.SendToAllClients(packetPause);
				PacketSender.SendToAllClients(packetZoom);
				PacketSender.SendToAllClients(packetName);
				PacketSender.SendToAllClients(packetTooltip);
			}
			else
			{
				PacketSender.SendToHost(packetType);
				PacketSender.SendToHost(packetPause);
				PacketSender.SendToHost(packetZoom);
				PacketSender.SendToHost(packetName);
				PacketSender.SendToHost(packetTooltip);
			}
		}
	}

	/// <summary>
	/// Sync AlarmSideScreen.OnEndEditName (when user edits notification name)
	/// </summary>
	[HarmonyPatch(typeof(AlarmSideScreen), nameof(AlarmSideScreen.OnEndEditName))]
	public static class AlarmSideScreen_OnEndEditName_Patch
	{
		public static void Postfix(AlarmSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetAlarm == null) return;

			var identity = __instance.targetAlarm.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetAlarm.gameObject),
				ConfigHash = "AlarmName".GetHashCode(),
				ConfigType = BuildingConfigType.String,
				StringValue = __instance.targetAlarm.notificationName ?? ""
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync AlarmSideScreen.OnEndEditTooltip (when user edits notification tooltip)
	/// </summary>
	[HarmonyPatch(typeof(AlarmSideScreen), nameof(AlarmSideScreen.OnEndEditTooltip))]
	public static class AlarmSideScreen_OnEndEditTooltip_Patch
	{
		public static void Postfix(AlarmSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetAlarm == null) return;

			var identity = __instance.targetAlarm.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetAlarm.gameObject),
				ConfigHash = "AlarmTooltip".GetHashCode(),
				ConfigType = BuildingConfigType.String,
				StringValue = __instance.targetAlarm.notificationTooltip ?? ""
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync AlarmSideScreen.TogglePause
	/// </summary>
	[HarmonyPatch(typeof(AlarmSideScreen), nameof(AlarmSideScreen.TogglePause))]
	public static class AlarmSideScreen_TogglePause_Patch
	{
		public static void Postfix(AlarmSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetAlarm == null) return;

			var identity = __instance.targetAlarm.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetAlarm.gameObject),
				ConfigHash = "AlarmPause".GetHashCode(),
				Value = __instance.targetAlarm.pauseOnNotify ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync AlarmSideScreen.ToggleZoom
	/// </summary>
	[HarmonyPatch(typeof(AlarmSideScreen), nameof(AlarmSideScreen.ToggleZoom))]
	public static class AlarmSideScreen_ToggleZoom_Patch
	{
		public static void Postfix(AlarmSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetAlarm == null) return;

			var identity = __instance.targetAlarm.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetAlarm.gameObject),
				ConfigHash = "AlarmZoom".GetHashCode(),
				Value = __instance.targetAlarm.zoomOnNotify ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync AlarmSideScreen.SelectType (notification type selection)
	/// </summary>
	[HarmonyPatch(typeof(AlarmSideScreen), nameof(AlarmSideScreen.SelectType))]
	public static class AlarmSideScreen_SelectType_Patch
	{
		public static void Postfix(AlarmSideScreen __instance, NotificationType type)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetAlarm == null) return;

			var identity = __instance.targetAlarm.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetAlarm.gameObject),
				ConfigHash = "AlarmType".GetHashCode(),
				Value = (int)type,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Force AlarmSideScreen to refresh from component values when SetTarget is called.
	/// </summary>
	[HarmonyPatch(typeof(AlarmSideScreen), nameof(AlarmSideScreen.SetTarget))]
	public static class AlarmSideScreen_SetTarget_Patch
	{
		public static void Postfix(AlarmSideScreen __instance, GameObject target)
		{
			if (__instance.targetAlarm == null) return;
			
			// Force update visuals from current component values
			__instance.UpdateVisuals();
			__instance.RefreshToggles();
		}
	}


	/// <summary>
	/// Sync LogicTimeOfDaySensor via OnCopySettings (cycle sensor)
	/// </summary>
	[HarmonyPatch(typeof(LogicTimeOfDaySensor), nameof(LogicTimeOfDaySensor.OnCopySettings))]
	public static class LogicTimeOfDaySensor_OnCopySettings_Patch
	{
		public static void Postfix(LogicTimeOfDaySensor __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packetStart = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "StartTime".GetHashCode(),
				Value = __instance.startTime,
				ConfigType = BuildingConfigType.Float
			};
			var packetDuration = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "Duration".GetHashCode(),
				Value = __instance.duration,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(packetStart);
				PacketSender.SendToAllClients(packetDuration);
			}
			else
			{
				PacketSender.SendToHost(packetStart);
				PacketSender.SendToHost(packetDuration);
			}
		}
	}

	/// <summary>
	/// Sync TimeRangeSideScreen.ChangeSetting (cycle sensor slider changes)
	/// </summary>
	[HarmonyPatch(typeof(TimeRangeSideScreen), nameof(TimeRangeSideScreen.ChangeSetting))]
	public static class TimeRangeSideScreen_ChangeSetting_Patch
	{
		public static void Postfix(TimeRangeSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetTimedSwitch == null) return;

			var identity = __instance.targetTimedSwitch.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packetStart = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetTimedSwitch.gameObject),
				ConfigHash = "StartTime".GetHashCode(),
				Value = __instance.startTime.value,
				ConfigType = BuildingConfigType.Float
			};
			var packetDuration = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetTimedSwitch.gameObject),
				ConfigHash = "Duration".GetHashCode(),
				Value = __instance.duration.value,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(packetStart);
				PacketSender.SendToAllClients(packetDuration);
			}
			else
			{
				PacketSender.SendToHost(packetStart);
				PacketSender.SendToHost(packetDuration);
			}
		}
	}

	/// <summary>
	/// Force TimeRangeSideScreen to refresh from component values when SetTarget is called.
	/// </summary>
	[HarmonyPatch(typeof(TimeRangeSideScreen), nameof(TimeRangeSideScreen.SetTarget))]
	public static class TimeRangeSideScreen_SetTarget_Patch
	{
		public static void Postfix(TimeRangeSideScreen __instance, GameObject target)
		{
			if (__instance.targetTimedSwitch == null) return;
			
			// Force update sliders from current component values
			__instance.startTime.value = __instance.targetTimedSwitch.startTime;
			__instance.duration.value = __instance.targetTimedSwitch.duration;
			__instance.ChangeSetting();
		}
	}

	/// <summary>
	/// Sync LimitValve via OnCopySettings (meter valve)
	/// </summary>
	[HarmonyPatch(typeof(LimitValve), nameof(LimitValve.OnCopySettings))]
	public static class LimitValve_OnCopySettings_Patch
	{
		public static void Postfix(LimitValve __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "LimitValve".GetHashCode(),
				Value = __instance.Limit,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	// NOTE: Receptacle sync disabled - causes planter crash when client opens UI
	// The issue is that creating a Tag from hash doesn't produce a valid prefab lookup
	// [HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.CreateOrder))]
	// public static class SingleEntityReceptacle_CreateOrder_Patch
	// {
	// 	public static void Postfix(SingleEntityReceptacle __instance, Tag entityTag, Tag additionalFilterTag)
	// 	{
	// 		if (BuildingConfigPacket.IsApplyingPacket) return;
	// 		if (!MultiplayerSession.InSession) return;
	// 
	// 		var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
	// 		identity.RegisterIdentity();
	// 
	// 		var packetTag = new BuildingConfigPacket
	// 		{
	// 			NetId = identity.NetId,
	// 			Cell = Grid.PosToCell(__instance.gameObject),
	// 			ConfigHash = "ReceptacleEntityTag".GetHashCode(),
	// 			Value = entityTag.GetHash(),
	// 			ConfigType = BuildingConfigType.Float
	// 		};
	// 
	// 		var packetFilter = new BuildingConfigPacket
	// 		{
	// 			NetId = identity.NetId,
	// 			Cell = Grid.PosToCell(__instance.gameObject),
	// 			ConfigHash = "ReceptacleFilterTag".GetHashCode(),
	// 			Value = additionalFilterTag.IsValid ? additionalFilterTag.GetHash() : 0,
	// 			ConfigType = BuildingConfigType.Float
	// 		};
	// 
	// 		if (MultiplayerSession.IsHost)
	// 		{
	// 			PacketSender.SendToAllClients(packetTag);
	// 			PacketSender.SendToAllClients(packetFilter);
	// 		}
	// 		else
	// 		{
	// 			PacketSender.SendToHost(packetTag);
	// 			PacketSender.SendToHost(packetFilter);
	// 		}
	// 	}
	// }

	// [HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.CancelActiveRequest))]
	// public static class SingleEntityReceptacle_CancelActiveRequest_Patch
	// {
	// 	public static void Postfix(SingleEntityReceptacle __instance)
	// 	{
	// 		if (BuildingConfigPacket.IsApplyingPacket) return;
	// 		if (!MultiplayerSession.InSession) return;
	// 
	// 		var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
	// 		identity.RegisterIdentity();
	// 
	// 		var packet = new BuildingConfigPacket
	// 		{
	// 			NetId = identity.NetId,
	// 			Cell = Grid.PosToCell(__instance.gameObject),
	// 			ConfigHash = "ReceptacleCancelRequest".GetHashCode(),
	// 			Value = 1f,
	// 			ConfigType = BuildingConfigType.Float
	// 		};
	// 
	// 		if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
	// 		else PacketSender.SendToHost(packet);
	// 	}
	// }

	// NOTE: EggIncubator.autoReplaceEntity is a field, not a property with a setter
	// Synced via OnCopySettings instead
	// [HarmonyPatch(typeof(EggIncubator), "autoReplaceEntity", MethodType.Setter)]
	// public static class EggIncubator_AutoReplaceEntity_Patch
	// {
	// 	public static void Postfix(EggIncubator __instance, bool value)
	// 	{
	// 		if (BuildingConfigPacket.IsApplyingPacket) return;
	// 		if (!MultiplayerSession.InSession) return;
	// 
	// 		var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
	// 		identity.RegisterIdentity();
	// 
	// 		var packet = new BuildingConfigPacket
	// 		{
	// 			NetId = identity.NetId,
	// 			Cell = Grid.PosToCell(__instance.gameObject),
	// 			ConfigHash = "IncubatorAutoReplace".GetHashCode(),
	// 			Value = value ? 1f : 0f,
	// 			ConfigType = BuildingConfigType.Boolean
	// 		};
	// 
	// 		if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
	// 		else PacketSender.SendToHost(packet);
	// 	}
	// }

	/// <summary>
	/// Sync BottleEmptier manual pumping station fetching toggle
	/// </summary>
	[HarmonyPatch(typeof(BottleEmptier), nameof(BottleEmptier.OnChangeAllowManualPumpingStationFetching))]
	public static class BottleEmptier_ManualPump_Patch
	{
		public static void Postfix(BottleEmptier __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "BottleEmptierManualPump".GetHashCode(),
				Value = __instance.allowManualPumpingStationFetching ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	// NOTE: Door_QueueStateChange_Patch already defined above using SideScreenSyncHelper
	// NOTE: CapacityControlSideScreen_UpdateMaxCapacity_Patch already defined above using SideScreenSyncHelper

	/// <summary>
	/// Sync SmartReservoir activation thresholds via OnCopySettings
	/// </summary>
	[HarmonyPatch(typeof(SmartReservoir), nameof(SmartReservoir.OnCopySettings))]
	public static class SmartReservoir_OnCopySettings_Patch
	{
		public static void Postfix(SmartReservoir __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packetActivate = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "SmartReservoirActivate".GetHashCode(),
				Value = __instance.activateValue,
				ConfigType = BuildingConfigType.Float
			};
			var packetDeactivate = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "SmartReservoirDeactivate".GetHashCode(),
				Value = __instance.deactivateValue,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(packetActivate);
				PacketSender.SendToAllClients(packetDeactivate);
			}
			else
			{
				PacketSender.SendToHost(packetActivate);
				PacketSender.SendToHost(packetDeactivate);
			}
		}
	}

	/// <summary>
	/// Force ActiveRangeSideScreen to always refresh from component values when SetTarget is called.
	/// This fixes threshold display sync for SmartReservoir, MassageTable, and other IActivationRangeTarget buildings.
	/// </summary>
	[HarmonyPatch(typeof(ActiveRangeSideScreen), nameof(ActiveRangeSideScreen.SetTarget))]
	public static class ActiveRangeSideScreen_SetTarget_Patch
	{
		public static void Postfix(ActiveRangeSideScreen __instance, GameObject new_target)
		{
			DebugConsole.Log($"[ActiveRangeSideScreen_SetTarget] Called for {new_target?.name ?? "null"}");
			
			if (__instance.target == null)
			{
				DebugConsole.Log("[ActiveRangeSideScreen_SetTarget] Target is null, returning");
				return;
			}
			
			// Force update sliders and labels from current component values
			float activateVal = __instance.target.ActivateValue;
			float deactivateVal = __instance.target.DeactivateValue;
			
			DebugConsole.Log($"[ActiveRangeSideScreen_SetTarget] Reading values: activate={activateVal}, deactivate={deactivateVal}");
			
			__instance.activateValueSlider.value = activateVal;
			__instance.deactivateValueSlider.value = deactivateVal;
			__instance.activateValueLabel.SetDisplayValue(activateVal.ToString());
			__instance.deactivateValueLabel.SetDisplayValue(deactivateVal.ToString());
			__instance.RefreshTooltips();
			
			DebugConsole.Log("[ActiveRangeSideScreen_SetTarget] Updated sliders and labels");
		}
	}

	/// <summary>
	/// Sync FoodStorage SpicedFoodOnly toggle (seasoned food only option on Refrigerator)
	/// </summary>
	[HarmonyPatch(typeof(FoodStorage), nameof(FoodStorage.OnCopySettings))]
	public static class FoodStorage_OnCopySettings_Patch
	{
		public static void Postfix(FoodStorage __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "FoodStorageSpicedFoodOnly".GetHashCode(),
				Value = __instance.SpicedFoodOnly ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync FoodStorage SpicedFoodOnly property changes (when toggled via sidescreen)
	/// </summary>
	[HarmonyPatch(typeof(FoodStorage), "SpicedFoodOnly", MethodType.Setter)]
	public static class FoodStorage_SpicedFoodOnly_Patch
	{
		public static void Postfix(FoodStorage __instance, bool value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "FoodStorageSpicedFoodOnly".GetHashCode(),
				Value = value ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	// NOTE: Using side screen patches instead of property setter for more reliable sync
	// The setter patch may fire multiple times during UI updates, causing issues
	// [HarmonyPatch(typeof(LimitValve), "Limit", MethodType.Setter)]
	// public static class LimitValve_Limit_Patch
	// {
	// 	public static void Postfix(LimitValve __instance, float value)
	// 	{
	// 		if (BuildingConfigPacket.IsApplyingPacket) return;
	// 		if (!MultiplayerSession.InSession) return;
	// 
	// 		var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
	// 		identity.RegisterIdentity();
	// 
	// 		var packet = new BuildingConfigPacket
	// 		{
	// 			NetId = identity.NetId,
	// 			Cell = Grid.PosToCell(__instance.gameObject),
	// 			ConfigHash = "LimitValve".GetHashCode(),
	// 			Value = value,
	// 			ConfigType = BuildingConfigType.Float
	// 		};
	// 
	// 		if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
	// 		else PacketSender.SendToHost(packet);
	// 	}
	// }

	/// <summary>
	/// Sync LimitValveSideScreen.OnReleaseHandle (when user releases slider)
	/// </summary>
	[HarmonyPatch(typeof(LimitValveSideScreen), nameof(LimitValveSideScreen.OnReleaseHandle))]
	public static class LimitValveSideScreen_OnReleaseHandle_Patch
	{
		public static void Postfix(LimitValveSideScreen __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetLimitValve == null) return;

			var identity = __instance.targetLimitValve.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetLimitValve.gameObject),
				ConfigHash = "LimitValve".GetHashCode(),
				Value = __instance.targetLimit,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);

			DebugConsole.Log($"[LimitValveSideScreen_OnReleaseHandle] Synced Limit={__instance.targetLimit}");
		}
	}

	/// <summary>
	/// Sync LimitValveSideScreen.ReceiveValueFromInput (when user enters number)
	/// </summary>
	[HarmonyPatch(typeof(LimitValveSideScreen), nameof(LimitValveSideScreen.ReceiveValueFromInput))]
	public static class LimitValveSideScreen_ReceiveValueFromInput_Patch
	{
		public static void Postfix(LimitValveSideScreen __instance, float input)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;
			if (__instance.targetLimitValve == null) return;

			var identity = __instance.targetLimitValve.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.targetLimitValve.gameObject),
				ConfigHash = "LimitValve".GetHashCode(),
				Value = input,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);

			DebugConsole.Log($"[LimitValveSideScreen_ReceiveValueFromInput] Synced Limit={input}");
		}
	}

	/// <summary>
	/// Force LimitValveSideScreen to always refresh from component value when SetTarget is called.
	/// The base game skips updating if same target is selected (flag=false), but in multiplayer
	/// the value may have changed remotely.
	/// </summary>
	[HarmonyPatch(typeof(LimitValveSideScreen), nameof(LimitValveSideScreen.SetTarget))]
	public static class LimitValveSideScreen_SetTarget_Patch
	{
		public static void Postfix(LimitValveSideScreen __instance, GameObject target)
		{
			if (__instance.targetLimitValve == null) return;
			
			// Always update the slider and number input from the actual component value
			float currentLimit = __instance.targetLimitValve.Limit;
			__instance.limitSlider.value = __instance.limitSlider.GetPercentageFromValue(currentLimit);
			__instance.targetLimit = currentLimit;
			
			// Update the display (the base game may skip this if flag=false)
			if (__instance.targetLimitValve.displayUnitsInsteadOfMass)
			{
				__instance.numberInput.SetDisplayValue(GameUtil.GetFormattedUnits(
					Mathf.Max(0f, currentLimit), 
					GameUtil.TimeSlice.None, 
					displaySuffix: false, 
					LimitValveSideScreen.FLOAT_FORMAT));
			}
			else
			{
				__instance.numberInput.SetDisplayValue(GameUtil.GetFormattedMass(
					Mathf.Max(0f, currentLimit), 
					GameUtil.TimeSlice.None, 
					GameUtil.MetricMassFormat.Kilogram, 
					includeSuffix: false, 
					LimitValveSideScreen.FLOAT_FORMAT));
			}
		}
	}

	// NOTE: EggIncubator.autoReplaceEntity is a FIELD (not a property) inherited from
	// SingleEntityReceptacle, so it cannot be patched with MethodType.Setter.
	// This feature syncs via OnCopySettings or the SingleEntityReceptacle patches instead.


	/// <summary>
	/// Sync AccessControl default permission changes (door access control)
	/// </summary>
	[HarmonyPatch(typeof(AccessControl), nameof(AccessControl.SetDefaultPermission))]
	public static class AccessControl_SetDefaultPermission_Patch
	{
		public static void Postfix(AccessControl __instance, Tag groupTag, AccessControl.Permission permission)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "AccessControlDefault".GetHashCode(),
				Value = (float)(int)permission,
				ConfigType = BuildingConfigType.String,
				StringValue = groupTag.Name
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync AccessControl individual minion permissions (door access for specific duplicants)
	/// Uses NetIDs for consistent duplicant identification across host and clients.
	/// </summary>
	[HarmonyPatch(typeof(AccessControl), nameof(AccessControl.SetPermission), typeof(MinionAssignablesProxy), typeof(AccessControl.Permission))]
	public static class AccessControl_SetPermission_Patch
	{
		public static void Postfix(AccessControl __instance, MinionAssignablesProxy key, AccessControl.Permission permission)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Get the duplicant's NetID
			int minionNetId = -1;
			var targetGO = key.GetTargetGameObject();
			if (targetGO != null)
			{
				var minionIdentity = targetGO.GetComponent<NetworkIdentity>();
				if (minionIdentity != null)
				{
					minionNetId = minionIdentity.NetId;
				}
			}

			if (minionNetId == -1) return; // Can't sync without NetID

			// Send as: ConfigHash = "AccessControlMinion", Value = permission, SliderIndex = minionNetId
			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "AccessControlMinion".GetHashCode(),
				Value = (int)permission,
				SliderIndex = minionNetId,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);

			DebugConsole.Log($"[AccessControl_SetPermission_Patch] Sent minion permission: door={__instance.name}, minionNetId={minionNetId}, permission={permission}");
		}
	}

	/// <summary>
	/// Sync AccessControl.ClearPermission (removing custom minion permissions from doors)
	/// Uses NetIDs for consistent duplicant identification.
	/// </summary>
	[HarmonyPatch(typeof(AccessControl), nameof(AccessControl.ClearPermission), typeof(MinionAssignablesProxy))]
	public static class AccessControl_ClearPermission_Patch
	{
		public static void Postfix(AccessControl __instance, MinionAssignablesProxy key)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Get the duplicant's NetID
			int minionNetId = -1;
			var targetGO = key.GetTargetGameObject();
			if (targetGO != null)
			{
				var minionIdentity = targetGO.GetComponent<NetworkIdentity>();
				if (minionIdentity != null)
				{
					minionNetId = minionIdentity.NetId;
				}
			}

			if (minionNetId == -1) return;

			// Send clear command: ConfigHash = "AccessControlClear", SliderIndex = minionNetId
			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "AccessControlClear".GetHashCode(),
				Value = 0f,
				SliderIndex = minionNetId,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);

			DebugConsole.Log($"[AccessControl_ClearPermission_Patch] Sent clear permission for minionNetId={minionNetId} on {__instance.name}");
		}
	}

	/// <summary>
	/// Sync EggIncubator autoReplaceEntity (Continuous toggle) via OnCopySettings
	/// </summary>
	[HarmonyPatch(typeof(EggIncubator), nameof(EggIncubator.OnCopySettings))]
	public static class EggIncubator_OnCopySettings_Patch
	{
		public static void Postfix(EggIncubator __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "IncubatorAutoReplace".GetHashCode(),
				Value = __instance.autoReplaceEntity ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync EggIncubator autoReplaceEntity when clicking the UI toggle directly
	/// Wraps the continuousToggle.onClick to also send a sync packet
	/// </summary>
	[HarmonyPatch(typeof(IncubatorSideScreen), nameof(IncubatorSideScreen.SetTarget))]
	public static class IncubatorSideScreen_SetTarget_Patch
	{
		public static void Postfix(IncubatorSideScreen __instance, GameObject target)
		{
			if (target == null) return;

			var incubator = target.GetComponent<EggIncubator>();
			if (incubator == null) return;

			// Get the original onClick action (if any) and wrap it
			var originalOnClick = __instance.continuousToggle.onClick;

			__instance.continuousToggle.onClick = delegate
			{
				// Run the original click action (toggles the value and updates UI)
				originalOnClick?.Invoke();

				// Now send the sync packet
				if (BuildingConfigPacket.IsApplyingPacket) return;
				if (!MultiplayerSession.InSession) return;

				var identity = incubator.gameObject.AddOrGet<NetworkIdentity>();
				identity.RegisterIdentity();

				var packet = new BuildingConfigPacket
				{
					NetId = identity.NetId,
					Cell = Grid.PosToCell(incubator.gameObject),
					ConfigHash = "IncubatorAutoReplace".GetHashCode(),
					Value = incubator.autoReplaceEntity ? 1f : 0f,
					ConfigType = BuildingConfigType.Boolean
				};

				if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
				else PacketSender.SendToHost(packet);

				DebugConsole.Log($"[IncubatorSideScreen] Synced autoReplaceEntity={incubator.autoReplaceEntity} for {incubator.name}");
			};
		}
	}

	/// <summary>
	/// Sync Automatable.SetAutomationOnly (Allow Manual Use toggle on conveyor buildings and others)
	/// </summary>
	[HarmonyPatch(typeof(Automatable), nameof(Automatable.SetAutomationOnly))]
	public static class Automatable_SetAutomationOnly_Patch
	{
		public static void Postfix(Automatable __instance, bool only)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "AutomationOnly".GetHashCode(),
				Value = only ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);

			DebugConsole.Log($"[Automatable_SetAutomationOnly_Patch] Synced AutomationOnly={only} for {__instance.name}");
		}
	}

	/// <summary>
	/// Sync DirectionControl (Sink, Wash Basin, Hand Sanitizer direction setting)
	/// </summary>
	[HarmonyPatch(typeof(DirectionControl), nameof(DirectionControl.SetAllowedDirection))]
	public static class DirectionControl_SetAllowedDirection_Patch
	{
		public static void Postfix(DirectionControl __instance, WorkableReactable.AllowedDirection new_direction)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "DirectionControl".GetHashCode(),
				Value = (int)new_direction,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);

			DebugConsole.Log($"[DirectionControl_SetAllowedDirection_Patch] Synced direction={new_direction} for {__instance.name}");
		}
	}

	/// <summary>
	/// Sync MassageTable threshold via OnCopySettings (implements IActivationRangeTarget)
	/// </summary>
	[HarmonyPatch(typeof(MassageTable), nameof(MassageTable.OnCopySettings))]
	public static class MassageTable_OnCopySettings_Patch
	{
		public static void Postfix(MassageTable __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Send activate value
			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "MassageTableActivate".GetHashCode(),
				Value = __instance.ActivateValue,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);

			// Send deactivate value
			var packet2 = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "MassageTableDeactivate".GetHashCode(),
				Value = __instance.DeactivateValue,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet2);
			else PacketSender.SendToHost(packet2);
		}
	}

	/// <summary>
	/// Sync MassageTable ActivateValue property changes (threshold slider)
	/// </summary>
	[HarmonyPatch(typeof(MassageTable), "ActivateValue", MethodType.Setter)]
	public static class MassageTable_ActivateValue_Patch
	{
		public static void Postfix(MassageTable __instance, float value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "MassageTableActivate".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync MassageTable DeactivateValue property changes (threshold slider)
	/// </summary>
	[HarmonyPatch(typeof(MassageTable), "DeactivateValue", MethodType.Setter)]
	public static class MassageTable_DeactivateValue_Patch
	{
		public static void Postfix(MassageTable __instance, float value)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "MassageTableDeactivate".GetHashCode(),
				Value = value,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync IceMachine target production element
	/// </summary>
	[HarmonyPatch(typeof(IceMachine), nameof(IceMachine.OnOptionSelected))]
	public static class IceMachine_OnOptionSelected_Patch
	{
		public static void Postfix(IceMachine __instance, FewOptionSideScreen.IFewOptionSideScreen.Option option)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "IceMachineElement".GetHashCode(),
				StringValue = option.tag.Name,
				ConfigType = BuildingConfigType.String
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync Gantry (Launch Pad Dock) toggle state
	/// </summary>
	[HarmonyPatch(typeof(Gantry), nameof(Gantry.Toggle))]
	public static class Gantry_Toggle_Patch
	{
		public static void Postfix(Gantry __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "GantryToggle".GetHashCode(),
				Value = __instance.IsSwitchedOn ? 1f : 0f,
				ConfigType = BuildingConfigType.Boolean
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync SingleEntityReceptacle orders (planters, incubators selecting items)
	/// </summary>
	[HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.CreateOrder))]
	public static class SingleEntityReceptacle_CreateOrder_Patch
	{
		public static void Postfix(SingleEntityReceptacle __instance, Tag entityTag, Tag additionalFilterTag)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Send entity tag as string
			var packetEntity = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "ReceptacleEntityTag".GetHashCode(),
				Value = 0,
				ConfigType = BuildingConfigType.String,
				StringValue = entityTag.IsValid ? entityTag.Name : ""
			};

			// Send additional filter tag as string (for mutations)
			var packetFilter = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "ReceptacleFilterTag".GetHashCode(),
				Value = 0,
				ConfigType = BuildingConfigType.String,
				StringValue = additionalFilterTag.IsValid ? additionalFilterTag.Name : ""
			};

			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(packetEntity);
				PacketSender.SendToAllClients(packetFilter);
			}
			else
			{
				PacketSender.SendToHost(packetEntity);
				PacketSender.SendToHost(packetFilter);
			}
		}
	}

	/// <summary>
	/// Sync SingleEntityReceptacle cancel requests
	/// </summary>
	[HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.CancelActiveRequest))]
	public static class SingleEntityReceptacle_CancelActiveRequest_Patch
	{
		public static void Postfix(SingleEntityReceptacle __instance)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "ReceptacleCancelRequest".GetHashCode(),
				Value = 1f,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
		}
	}

	/// <summary>
	/// Sync Assignable.Assign (building assignments for Outhouse, Lavatory, Triage Cot, etc.)
	/// Uses duplicant NetIDs for consistent assignment across host and clients.
	/// </summary>
	[HarmonyPatch(typeof(Assignable), nameof(Assignable.Assign), typeof(IAssignableIdentity))]
	public static class Assignable_Assign_Patch
	{
		public static void Postfix(Assignable __instance, IAssignableIdentity new_assignee)
		{
			if (AssignmentPacket.IsApplying) return;
			if (!MultiplayerSession.InSession) return;

			var buildingIdentity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			buildingIdentity.RegisterIdentity();

			int assigneeNetId = -1;
			string groupId = "";

			if (new_assignee == null)
			{
				// Unassign
				assigneeNetId = -1;
			}
			else if (new_assignee is AssignmentGroup group)
			{
				// Assignment group (e.g., "public")
				groupId = group.id;
			}
			else if (new_assignee is MinionAssignablesProxy proxy)
			{
				// Get the actual minion from the proxy
				var targetGO = proxy.GetTargetGameObject();
				if (targetGO != null)
				{
					var minionNetId = targetGO.GetComponent<NetworkIdentity>();
					if (minionNetId != null)
					{
						assigneeNetId = minionNetId.NetId;
					}
				}
			}
			else if (new_assignee is KMonoBehaviour mb)
			{
				// Direct minion reference
				var minionNetId = mb.gameObject.GetComponent<NetworkIdentity>();
				if (minionNetId != null)
				{
					minionNetId.RegisterIdentity();
					assigneeNetId = minionNetId.NetId;
				}
			}

			var packet = new AssignmentPacket
			{
				BuildingNetId = buildingIdentity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				AssigneeNetId = assigneeNetId,
				GroupId = groupId
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);

			DebugConsole.Log($"[Assignable_Assign_Patch] Sent assignment: building={__instance.name}, assignee={new_assignee}, netId={assigneeNetId}, group={groupId}");
		}
	}

	/// <summary>
	/// Sync Assignable.Unassign
	/// </summary>
	[HarmonyPatch(typeof(Assignable), nameof(Assignable.Unassign))]
	public static class Assignable_Unassign_Patch
	{
		public static void Postfix(Assignable __instance)
		{
			if (AssignmentPacket.IsApplying) return;
			if (!MultiplayerSession.InSession) return;

			var buildingIdentity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			buildingIdentity.RegisterIdentity();

			var packet = new AssignmentPacket
			{
				BuildingNetId = buildingIdentity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				AssigneeNetId = -1,
				GroupId = ""
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);

			DebugConsole.Log($"[Assignable_Unassign_Patch] Sent unassign for {__instance.name}");
		}
	}

	/// <summary>
	/// Sync GeoTuner geyser selection when AssignFutureGeyser is called
	/// </summary>
	[HarmonyPatch(typeof(GeoTuner.Instance), nameof(GeoTuner.Instance.AssignFutureGeyser))]
	public static class GeoTuner_Instance_AssignFutureGeyser_Patch
	{
		public static void Postfix(GeoTuner.Instance __instance, Geyser newFutureGeyser)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Send geyser's cell position (-1 means no geyser selected)
			int geyserCell = (newFutureGeyser != null) ? Grid.PosToCell(newFutureGeyser.gameObject) : -1;

			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "GeoTunerGeyser".GetHashCode(),
				Value = geyserCell,
				ConfigType = BuildingConfigType.Float
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
			
			DebugConsole.Log($"[GeoTuner_AssignFutureGeyser] Sent geyser assignment: cell={geyserCell}");
		}
	}

	/// <summary>
	/// Force GeoTunerSideScreen to refresh from component values when SetTarget is called.
	/// </summary>
	[HarmonyPatch(typeof(GeoTunerSideScreen), nameof(GeoTunerSideScreen.SetTarget))]
	public static class GeoTunerSideScreen_SetTarget_Patch
	{
		public static void Postfix(GeoTunerSideScreen __instance, GameObject target)
		{
			if (__instance.targetGeotuner == null) return;
			
		// Force refresh the options list from current component state
			__instance.RefreshOptions();
		}
	}

	/// <summary>
	/// Sync MissileLauncher ammunition selection when ChangeAmmunition is called
	/// </summary>
	[HarmonyPatch(typeof(MissileLauncher.Instance), nameof(MissileLauncher.Instance.ChangeAmmunition))]
	public static class MissileLauncher_Instance_ChangeAmmunition_Patch
	{
		public static void Postfix(MissileLauncher.Instance __instance, Tag tag, bool allowed)
		{
			if (BuildingConfigPacket.IsApplyingPacket) return;
			if (!MultiplayerSession.InSession) return;

			var identity = __instance.gameObject.AddOrGet<NetworkIdentity>();
			identity.RegisterIdentity();

			// Send tag as string and allowed as value (0 or 1)
			var packet = new BuildingConfigPacket
			{
				NetId = identity.NetId,
				Cell = Grid.PosToCell(__instance.gameObject),
				ConfigHash = "MissileLauncherAmmo".GetHashCode(),
				Value = allowed ? 1f : 0f,
				ConfigType = BuildingConfigType.String,
				StringValue = tag.Name
			};

			if (MultiplayerSession.IsHost) PacketSender.SendToAllClients(packet);
			else PacketSender.SendToHost(packet);
			
			DebugConsole.Log($"[MissileLauncher_ChangeAmmunition] Sent ammo change: tag={tag.Name}, allowed={allowed}");
		}
	}

	/// <summary>
	/// Force MissileSelectionSideScreen to refresh from component values when SetTarget is called.
	/// </summary>
	[HarmonyPatch(typeof(MissileSelectionSideScreen), nameof(MissileSelectionSideScreen.SetTarget))]
	public static class MissileSelectionSideScreen_SetTarget_Patch
	{
		public static void Postfix(MissileSelectionSideScreen __instance, GameObject target)
		{
			if (__instance.targetMissileLauncher == null) return;
			
			// Force refresh the UI from current component state
			__instance.Refresh();
		}
	}
}


