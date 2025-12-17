using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;

namespace ONI_MP.Patches.World
{
	// Adds NetworkIdentity to buildings that need it for BuildingConfigPacket or other interactions
	[HarmonyPatch(typeof(BuildingComplete), "OnSpawn")]
	public static class BuildingSpawnPatch
	{
		public static void Postfix(BuildingComplete __instance)
		{
			if (!MultiplayerSession.InSession) return;

			var go = __instance.gameObject;
			bool needsIdentity = false;

			// Check for components that require NetID
			if (go.GetComponent<LogicSwitch>() != null) needsIdentity = true;
			else if (go.GetComponent<Valve>() != null) needsIdentity = true;
			else if (go.GetComponent<LogicTemperatureSensor>() != null) needsIdentity = true;
			else if (go.GetComponent<LogicPressureSensor>() != null) needsIdentity = true;
			else if (go.GetComponent<LogicWattageSensor>() != null) needsIdentity = true;
			else if (go.GetComponent<LogicTimeOfDaySensor>() != null) needsIdentity = true;
			else if (go.GetComponent<IThresholdSwitch>() != null) needsIdentity = true; // Smart Battery, etc
			else if (go.GetComponent<IActivationRangeTarget>() != null) needsIdentity = true;
			else if (go.GetComponent<ISliderControl>() != null) needsIdentity = true;

			// Should we add it to everything?
			// DeconstructablePatch uses Cell, so we don't need it there.
			// Wires/Pipes use Cell for build.
			// Only config sync needs NetID.

			if (needsIdentity)
			{
				var identity = go.AddOrGet<NetworkIdentity>();
				identity.RegisterIdentity();
			}
		}
	}
}
