using HarmonyLib;
using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets
{
	internal class API_Helper
	{
		public static Type CreateModApiPacketType(Type modPacketType)
		{
			var genericType = typeof(ModApiPacket<>);
			var constructedType = genericType.MakeGenericType(modPacketType);
			return constructedType;
		}
		public static bool WrapApiPacket(object packet, out IPacket wrap)
		{
			wrap = null;

			var type = packet.GetType();
			int id = type.Name.GetHashCode();
			if (!PacketRegistry.HasRegisteredPacket(type))
				return false;
			wrap = PacketRegistry.Create(id);
			if(wrap is IModApiPacket apiPacket)
			{
				apiPacket.SetWrappedInstance(packet);
				return true;
			}
			return false;
		}

		public static bool ValidAsModApiPacket(Type potentialPacketType)
		{
			///Ducktyping check if it has the required methods from IPacket interface
			var t = Traverse.Create(potentialPacketType);
			if (t.Method("Serialize", [typeof(BinaryWriter)]).MethodExists() &&
				t.Method("Deserialize", [typeof(BinaryReader)]).MethodExists() &&
				t.Method("OnDispatched").MethodExists())
				return true;
			return false;
		}

	}
}
