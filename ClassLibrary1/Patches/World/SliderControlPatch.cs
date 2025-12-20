using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Patches.World
{
	/// <summary>
	/// Patches for ISliderControl implementations to sync slider value changes.
	/// Instead of patching the interface (not possible in Harmony), we patch common implementations.
	/// </summary>

	// Patch Door's access control slider
	[HarmonyPatch(typeof(Door), "OnCopySettings")]
	public static class DoorSliderPatch
	{
		public static void Postfix(Door __instance, object data)
		{
			// This is called when copy-paste settings, but for direct slider changes
			// we need different hooks. Door doesn't use ISliderControl typically.
		}
	}

	// Generic approach: Patch the side screen that sets slider values
	// SingleSliderSideScreen.SetSliderValue is internal, so we target the slider release
	// The UI calls ISliderControl.SetSliderValue on the target component

	// TODO: Add specific ISliderControl implementation patches as needed
	// Examples: Certain machines with temperature setpoints, etc.
}

