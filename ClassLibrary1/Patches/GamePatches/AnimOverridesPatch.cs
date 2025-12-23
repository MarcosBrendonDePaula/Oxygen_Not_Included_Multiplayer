using HarmonyLib;
using System;

namespace ONI_MP.Patches.GamePatches
{
	/// <summary>
	/// Prevents crash when AddAnimOverrides is called with a null kanim file.
	/// This commonly happens on clients when animation data isn't synced.
	/// </summary>
	[HarmonyPatch(typeof(KAnimControllerBase), nameof(KAnimControllerBase.AddAnimOverrides))]
	[HarmonyPatch(new Type[] { typeof(KAnimFile), typeof(float) })]
	public static class KAnimControllerBase_AddAnimOverrides_Patch
	{
		public static bool Prefix(KAnimFile kanim_file)
		{
			// If the kanim file is null, skip the original method entirely
			// This prevents the Debug.LogError crash
			if (kanim_file == null)
			{
				// Silently skip - this is common in multiplayer when animations aren't synced
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Prevents crash when RemoveAnimOverrides is called with a null kanim file.
	/// This commonly happens on clients when animation data isn't synced.
	/// </summary>
	[HarmonyPatch(typeof(KAnimControllerBase), nameof(KAnimControllerBase.RemoveAnimOverrides))]
	public static class KAnimControllerBase_RemoveAnimOverrides_Patch
	{
		public static bool Prefix(KAnimFile kanim_file)
		{
			// If the kanim file is null, skip the original method entirely
			// This prevents the Debug.LogError crash
			if (kanim_file == null)
			{
				// Silently skip - this is common in multiplayer when animations aren't synced
				return false;
			}
			return true;
		}
	}
}
