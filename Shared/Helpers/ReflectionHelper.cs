using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Helpers
{
	public static class ReflectionHelper
	{
		public static bool TryGetType(string typeName, out Type type)
		{
			type = Type.GetType(typeName);
			if (type == null)
				Debug.LogWarning($"[ReflectionHelper] Type '{typeName}' not found.");
			return type != null;
		}
		public static bool TryGetMethod(string typeName, string methodName, Type[] parameters, out System.Reflection.MethodInfo methodInfo)
		{
			methodInfo = null;
			if (!TryGetType(typeName, out Type type))
				return false;
			methodInfo = AccessTools.Method(type, methodName, parameters);

			if (methodInfo == null)
				Debug.LogWarning($"[ReflectionHelper] method '{methodName}' not found on type {type}");

			return methodInfo != null;
		}
		public static bool TryCreateDelegate<T>(string typeName, string methodName, Type[] parameters, out T del) where T : Delegate
		{
			del = null;
			if (!TryGetMethod(typeName, methodName, parameters, out var methodInfo))
				return false;
			del = (T)Delegate.CreateDelegate(typeof(T), methodInfo);
			return del != null;

		}
	}
}
