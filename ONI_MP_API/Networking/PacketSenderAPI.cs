using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using Shared.Helpers;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP_API.Networking
{
	public static class PacketSenderAPI
	{
		static bool Init()
		{
			if (typesInitialized)
				return true;

			if (!ReflectionHelper.TryCreateDelegate<SendToAllDelegate>("ONI_MP.Networking.PacketSender, ONI_MP", "SendToAll_API", [typeof(object), typeof(CSteamID?), typeof(int)], out _SendToAll))
				return false;

			typesInitialized = true;
			return true;
		}

		static bool typesInitialized = false;

		static SendToAllDelegate? _SendToAll = null;
		delegate void SendToAllDelegate(object packet, CSteamID? exclude = null, int sendType = (int)SteamNetworkingSend.Reliable);

		/// Original single-exclude overload
		public static void SendToAll(IPacket packet, CSteamID? exclude = null, SteamNetworkingSend sendType = SteamNetworkingSend.Reliable)
		{
			Init();
			if (_SendToAll == null)
				return;
			_SendToAll(packet, exclude, (int)sendType);
		}
	}
}
