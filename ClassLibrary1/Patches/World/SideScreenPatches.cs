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
			if (BuildingConfigPacket.IsApplyingPacket) return;
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
			// Specific rounding for ManualGenerator
			if (target.GetComponent<ManualGenerator>() != null) value = Mathf.Round(value);
			Send(target, value, index);
		}

		private static void OnInputEndEdit(GameObject target, KNumberInputField input, int index)
		{
			float value = input.currentValue;
			if (target.GetComponent<ManualGenerator>() != null) value = Mathf.Round(value);
			Send(target, value, index);
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


}
