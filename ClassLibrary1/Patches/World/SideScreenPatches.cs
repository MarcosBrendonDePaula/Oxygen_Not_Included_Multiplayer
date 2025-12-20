using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
}
