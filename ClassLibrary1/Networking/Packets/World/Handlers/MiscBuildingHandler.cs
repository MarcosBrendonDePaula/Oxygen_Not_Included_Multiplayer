using UnityEngine;
using HarmonyLib;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Packets.World.Handlers
{
	/// <summary>
	/// Handles miscellaneous buildings that don't fit into other categories.
	/// Includes: LogicSwitch, LogicCounter, LimitValve, ManualGenerator, BottleEmptier,
	/// Checkbox controls, and other one-off handlers.
	/// </summary>
	public class MiscBuildingHandler : IBuildingConfigHandler
	{
		private static readonly int[] _hashes = new int[]
		{
			// LogicSwitch
			"LogicSwitchState".GetHashCode(),
			// LogicCounter
			"CounterMaxCount".GetHashCode(),
			"CounterAdvancedMode".GetHashCode(),
			"CounterResetAtMax".GetHashCode(),
			"CounterReset".GetHashCode(),
			// CritterSensor
			"CritterSensorCountCritters".GetHashCode(),
			"CritterSensorCountEggs".GetHashCode(),
			// LimitValve
			"LimitValveLimit".GetHashCode(),
			// ManualGenerator
			"ManualGeneratorThreshold".GetHashCode(),
			// BottleEmptier
			"BottleEmptierAllowManualPump".GetHashCode(),
			// Checkbox control
			"Checkbox".GetHashCode(),
			// Automatable
			"AutomatableAutomationOnly".GetHashCode(),
			// DirectionControl
			"LoopConveyorDirection".GetHashCode(),
		};

		public int[] SupportedConfigHashes => _hashes;

		public bool TryApplyConfig(GameObject go, BuildingConfigPacket packet)
		{
			int hash = packet.ConfigHash;

			// LogicSwitch
			var logicSwitch = go.GetComponent<LogicSwitch>();
			if (logicSwitch != null && hash == "LogicSwitchState".GetHashCode())
			{
				logicSwitch.SetState(packet.Value > 0.5f);
				DebugConsole.Log($"[MiscBuildingHandler] Set LogicSwitch state={packet.Value > 0.5f} on {go.name}");
				return true;
			}

			// LogicCounter
			var counter = go.GetComponent<LogicCounter>();
			if (counter != null)
			{
				if (hash == "CounterMaxCount".GetHashCode())
				{
					counter.maxCount = (int)packet.Value;
					counter.SetCounterState();
					DebugConsole.Log($"[MiscBuildingHandler] Set counter maxCount={counter.maxCount} on {go.name}");
					return true;
				}
				if (hash == "CounterAdvancedMode".GetHashCode())
				{
					counter.advancedMode = packet.Value > 0.5f;
					counter.SetCounterState();
					DebugConsole.Log($"[MiscBuildingHandler] Set counter advancedMode={counter.advancedMode} on {go.name}");
					return true;
				}
				if (hash == "CounterResetAtMax".GetHashCode())
				{
					counter.resetCountAtMax = packet.Value > 0.5f;
					counter.SetCounterState();
					DebugConsole.Log($"[MiscBuildingHandler] Set counter resetCountAtMax={counter.resetCountAtMax} on {go.name}");
					return true;
				}
				if (hash == "CounterReset".GetHashCode())
				{
					counter.ResetCounter();
					DebugConsole.Log($"[MiscBuildingHandler] Reset counter on {go.name}");
					return true;
				}
			}

			// CritterSensor
			var critterSensor = go.GetComponent<LogicCritterCountSensor>();
			if (critterSensor != null)
			{
				if (hash == "CritterSensorCountCritters".GetHashCode())
				{
					critterSensor.countCritters = packet.Value > 0.5f;
					DebugConsole.Log($"[MiscBuildingHandler] Set countCritters={critterSensor.countCritters} on {go.name}");
					return true;
				}
				if (hash == "CritterSensorCountEggs".GetHashCode())
				{
					critterSensor.countEggs = packet.Value > 0.5f;
					DebugConsole.Log($"[MiscBuildingHandler] Set countEggs={critterSensor.countEggs} on {go.name}");
					return true;
				}
			}

			// LimitValve
			var limitValve = go.GetComponent<LimitValve>();
			if (limitValve != null && hash == "LimitValveLimit".GetHashCode())
			{
				limitValve.Limit = packet.Value;
				DebugConsole.Log($"[MiscBuildingHandler] Set LimitValve Limit={packet.Value} on {go.name}");
				return true;
			}

			// ManualGenerator
			var manualGenerator = go.GetComponent<ManualGenerator>();
			if (manualGenerator != null && hash == "ManualGeneratorThreshold".GetHashCode())
			{
				Traverse.Create(manualGenerator).Field("refillPercent").SetValue(packet.Value);
				DebugConsole.Log($"[MiscBuildingHandler] Set ManualGenerator refillPercent={packet.Value} on {go.name}");
				return true;
			}

			// BottleEmptier
			var bottleEmptier = go.GetComponent<BottleEmptier>();
			if (bottleEmptier != null && hash == "BottleEmptierAllowManualPump".GetHashCode())
			{
				bottleEmptier.allowManualPumpingStationFetching = packet.Value > 0.5f;
				DebugConsole.Log($"[MiscBuildingHandler] Set BottleEmptier allowManualPump={packet.Value > 0.5f} on {go.name}");
				return true;
			}

			// ICheckboxControl
			if (hash == "Checkbox".GetHashCode())
			{
				var checkbox = go.GetComponent<ICheckboxControl>();
				if (checkbox != null)
				{
					checkbox.SetCheckboxValue(packet.Value > 0.5f);
					DebugConsole.Log($"[MiscBuildingHandler] Set Checkbox={packet.Value > 0.5f} on {go.name}");
					return true;
				}
			}

			// Automatable
			var automatable = go.GetComponent<Automatable>();
			if (automatable != null && hash == "AutomatableAutomationOnly".GetHashCode())
			{
				automatable.SetAutomationOnly(packet.Value > 0.5f);
				DebugConsole.Log($"[MiscBuildingHandler] Set AutomationOnly={packet.Value > 0.5f} on {go.name}");
				return true;
			}

			// DirectionControl (Loop Conveyor)
			var directionControl = go.GetComponent<DirectionControl>();
			if (directionControl != null && hash == "LoopConveyorDirection".GetHashCode())
			{
				directionControl.SetAllowedDirection((WorkableReactable.AllowedDirection)(int)packet.Value);
				DebugConsole.Log($"[MiscBuildingHandler] Set Direction={(WorkableReactable.AllowedDirection)(int)packet.Value} on {go.name}");
				return true;
			}

			return false;
		}
	}
}
