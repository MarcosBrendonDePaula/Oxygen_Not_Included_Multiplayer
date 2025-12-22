using JetBrains.Annotations;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP_API.Networking
{
	public static class PacketRegistryAPI
	{
		static bool Init()
		{
			if (typesInitialized)
				return true;
						
			if (!ReflectionHelper.TryCreateDelegate<TryRegisterPacketDelegate>("ONI_MP.Networking.Packets.Architecture.PacketRegistry, ONI_MP", "TryRegister", [typeof(Type), typeof(string)], out _TryRegister))
				return false;
			typesInitialized = true;
			return true;
		}

		static bool typesInitialized = false;
		static TryRegisterPacketDelegate? _TryRegister = null;
		delegate void TryRegisterPacketDelegate(Type packetType, string nameOverride);


		/// <summary>
		/// Registers a packet type with the packet registry.
		/// Do not call earlier than "OnAllModsLoaded" Harmony event or the main mod type might not exist yet.
		/// </summary>
		/// <param name="packetType"></param>
		public static void TryRegister(Type packetType, string nameOverride = null)
		{
			if (!Init())
				return;
			_TryRegister(packetType, nameOverride);
		}
	}
}
