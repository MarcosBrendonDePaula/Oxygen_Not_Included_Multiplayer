using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using System.IO;
using UnityEngine;
using HarmonyLib;

namespace ONI_MP.Networking.Packets.World
{
	public enum BuildingConfigType : byte
	{
		Float = 0,      // Standard float value (valve flow, thresholds)
		Boolean = 1,    // Checkbox values
		SliderIndex = 2 // Slider with index (for multi-slider controls)
	}

	public class BuildingConfigPacket : IPacket
	{
		public PacketType Type => PacketType.BuildingConfig;

		public int NetId;
		public int Cell; // Deterministic location-based identification
		public int ConfigHash; // Hash of the property name (e.g. "Threshold", "Logic")
		public float Value;
		public BuildingConfigType ConfigType = BuildingConfigType.Float;
		public int SliderIndex = 0; // For ISliderControl multi-sliders

		public static bool IsApplyingPacket = false;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(Cell);
			writer.Write(ConfigHash);
			writer.Write(Value);
			writer.Write((byte)ConfigType);
			writer.Write(SliderIndex);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			Cell = reader.ReadInt32();
			ConfigHash = reader.ReadInt32();
			Value = reader.ReadSingle();
			ConfigType = (BuildingConfigType)reader.ReadByte();
			SliderIndex = reader.ReadInt32();
		}

		public void OnDispatched()
		{
			DebugConsole.Log($"[BuildingConfigPacket] Received a config update packet. NetId={NetId}, Cell={Cell}");

			if (!NetworkIdentityRegistry.TryGet(NetId, out var identity) || identity == null)
			{
				// Attempt to find building by cell
				if (Grid.IsValidCell(Cell))
				{
					// For multi-layered buildings, we might need a more specific search, but usually 
					// we just look for BuildingComplete components.
					GameObject buildingGO = Grid.Objects[Cell, (int)ObjectLayer.Building];
					if (buildingGO != null)
					{
						identity = buildingGO.AddOrGet<NetworkIdentity>();
						identity.NetId = NetId; // Client forces the NetId from Host
						identity.RegisterIdentity();
						DebugConsole.Log($"[BuildingConfigPacket] Resolved missing identity for {buildingGO.name} at cell {Cell}. Assigned NetId: {NetId}");
					}
				}
			}

			if (identity != null)
			{
				try
				{
					IsApplyingPacket = true;
					ApplyConfig(identity.gameObject);
				}
				finally
				{
					IsApplyingPacket = false;
				}
			}
			else
			{
				DebugConsole.LogWarning($"[BuildingConfigPacket] FAILED to resolve entity for NetId {NetId} at Cell {Cell}");
			}
		}

    private void ApplyConfig(GameObject go)
		{
			if (go == null) return;

			// Handle Logic Switch (Signal Switch, etc.)
			var logicSwitch = go.GetComponent<LogicSwitch>();
			if (logicSwitch != null && ConfigHash == "LogicState".GetHashCode())
			{
				bool isOn = Value > 0.5f;
				if (logicSwitch.IsSwitchedOn != isOn)
				{
					HarmonyLib.Traverse.Create(logicSwitch).Method("HandleToggle").GetValue();
				}
				return;
			}

			// Handle Smart Battery (IActivationRangeTarget)
			var activationRange = go.GetComponent<IActivationRangeTarget>();
			if (activationRange != null)
			{
				if (ConfigHash == "Activate".GetHashCode()) activationRange.ActivateValue = Value;
				if (ConfigHash == "Deactivate".GetHashCode()) activationRange.DeactivateValue = Value;
				return;
			}

			// Handle Logic Sensors (IThresholdSwitch)
			var thresholdSwitch = go.GetComponent<IThresholdSwitch>();
			if (thresholdSwitch != null)
			{
				if (ConfigHash == "Threshold".GetHashCode())
				{
					thresholdSwitch.Threshold = Value;
					return;
				}
				if (ConfigHash == "ThresholdDir".GetHashCode())
				{
					thresholdSwitch.ActivateAboveThreshold = Value > 0.5f;
					return;
				}
			}

			// Handle Valves
			var valve = go.GetComponent<Valve>();
			if (valve != null && ConfigHash == "Rate".GetHashCode())
			{
				HarmonyLib.Traverse.Create(valve).Method("ChangeFlow", Value).GetValue();
				return;
			}

			// Handle ISliderControl (generic sliders)
			var sliderControl = go.GetComponent<ISliderControl>();
			if (sliderControl != null && ConfigHash == "Slider".GetHashCode())
			{
        sliderControl.SetSliderValue(Value, SliderIndex);
				return;
			}

			// Handle ISingleSliderControl (alternative generic slider)
			var singleSliderControl = go.GetComponent<ISingleSliderControl>();
			if (singleSliderControl == null) singleSliderControl = go.GetSMI<ISingleSliderControl>();
			if (singleSliderControl != null && ConfigHash == "Slider".GetHashCode())
			{
        DebugConsole.Log($"[BuildingConfigPacket] Slider changed: value={Value}, index={SliderIndex}");
        singleSliderControl.SetSliderValue(Value, SliderIndex);
				return;
			}

			// Handle ICheckboxControl
			var checkboxControl = go.GetComponent<ICheckboxControl>();
			if (checkboxControl != null && ConfigHash == "Checkbox".GetHashCode())
			{
				checkboxControl.SetCheckboxValue(Value > 0.5f);
				return;
			}

			// Handle IUserControlledCapacity (storage capacity)
			var capacityControl = go.GetComponent<IUserControlledCapacity>();
			if (capacityControl != null && ConfigHash == "Capacity".GetHashCode())
			{
				capacityControl.UserMaxCapacity = Value;
				return;
			}

			// Handle ISidescreenButtonControl (button presses)
			var buttonControl = go.GetComponent<ISidescreenButtonControl>();
			if (buttonControl != null && ConfigHash == "ButtonPress".GetHashCode())
			{
				buttonControl.OnSidescreenButtonPressed();
				return;
			}

			// Handle Door state (Open, Close, Auto)
			var door = go.GetComponent<Door>();
			if (door != null && ConfigHash == "DoorState".GetHashCode())
			{
				var state = (Door.ControlState)(int)Value;
				HarmonyLib.Traverse.Create(door).Method("SetRequestedState", new System.Type[] { typeof(Door.ControlState) }).GetValue(state);
				return;
			}

			// Handle LimitValve
			var limitValve = go.GetComponent<LimitValve>();
			if (limitValve != null && ConfigHash == "LimitValve".GetHashCode())
			{
				limitValve.Limit = Value;
				return;
			}

			// Handle LogicTimeOfDaySensor
			var timeOfDaySensor = go.GetComponent<LogicTimeOfDaySensor>();
			if (timeOfDaySensor != null)
			{
				if (ConfigHash == "StartTime".GetHashCode())
				{
					timeOfDaySensor.startTime = Value;
					return;
				}
				if (ConfigHash == "Duration".GetHashCode())
				{
					timeOfDaySensor.duration = Value;
					return;
				}
			}

			// Handle ManualGenerator
			var manualGenerator = go.GetComponent<ManualGenerator>();
			if (manualGenerator != null && ConfigHash == "ManualGeneratorThreshold".GetHashCode())
			{
				Traverse.Create(manualGenerator).Field("refillPercent").SetValue(Value);
				return;
			}

			// TODO: CritterCount sensor sync - needs investigation of the correct API
		}
	}
}
