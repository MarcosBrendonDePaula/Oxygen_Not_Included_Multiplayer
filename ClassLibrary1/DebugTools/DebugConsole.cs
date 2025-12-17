using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.DebugTools
{
	public class DebugConsole : MonoBehaviour
	{
		private static DebugConsole _instance;
		private static readonly List<LogEntry> logEntries = new List<LogEntry>();
		private Vector2 scrollPos;
		private bool showConsole = false;
		private const int MaxLines = 300;

		private GUIStyle logStyle;
		private GUIStyle warnStyle;
		private GUIStyle errorStyle;

		private class LogEntry
		{
			public string message;
			public string stack;
			public LogType type;
			public bool expanded;
		}

		private static string logPath;

		public static void Init()
		{
			if (_instance != null) return;

			logPath = System.IO.Path.Combine(Application.dataPath, "../ONI_MP_Log.txt");
			System.IO.File.WriteAllText(logPath, $"ONI Multiplayer Log - {System.DateTime.Now}\n");

			GameObject go = new GameObject("ONI_MP_DebugConsole");
			DontDestroyOnLoad(go);
			_instance = go.AddComponent<DebugConsole>();

			Application.logMessageReceived += _instance.HandleLog;
		}

		public void Toggle()
		{
			showConsole = !showConsole;
		}

		private void Awake()
		{
			//Application.logMessageReceived += HandleLog;
		}

		private void OnDestroy()
		{
			Application.logMessageReceived -= HandleLog;
		}

		// ... OnGUI ...

		private void OnGUI()
		{
			if (!showConsole) return;

			if (logStyle == null)
			{
				logStyle = new GUIStyle(GUI.skin.label);
				warnStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } };
				errorStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
			}

			GUILayout.BeginArea(new Rect(Screen.width - 610, 10, 600, Screen.height - 20), GUI.skin.box);
			GUILayout.Label("<b>Console Output</b>", new GUIStyle(GUI.skin.label) { richText = true });

			scrollPos = GUILayout.BeginScrollView(scrollPos);
			foreach (var entry in logEntries)
			{
				GUIStyle style;
				switch (entry.type)
				{
					case LogType.Warning:
						style = warnStyle;
						break;
					case LogType.Error:
					case LogType.Exception:
					case LogType.Assert:
						style = errorStyle;
						break;
					default:
						style = logStyle;
						break;
				}


				if (GUILayout.Button(entry.message, style))
				{
					entry.expanded = !entry.expanded;
				}

				if (entry.expanded && !string.IsNullOrEmpty(entry.stack))
				{
					GUILayout.Label(entry.stack, GUI.skin.box);
				}
			}
			GUILayout.EndScrollView();

			if (GUILayout.Button("Clear")) logEntries.Clear();

			GUILayout.EndArea();
		}

		private void HandleLog(string logString, string stackTrace, LogType type)
		{
			string message = $"[{type}] {logString}";
			if (type == LogType.Log)
			{
				message = $"{logString}";
			}

			// Write to file
			try
			{
				System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss}] {message}\n{stackTrace}\n");
			}
			catch { }

			logEntries.Add(new LogEntry
			{
				message = message,
				stack = stackTrace,
				type = type,
				expanded = false
			});

			if (logEntries.Count > MaxLines)
				logEntries.RemoveAt(0);
		}

		public static void Log(string message)
		{
			Debug.Log($"[ONI_MP] {message}");
			// HandleLog is hooked to Application.logMessageReceived, so we don't need to call it manually if we use Debug.Log
			// But for explicit calls we ensure instance exists.
			EnsureInstance();
		}

		public static void LogWarning(string message)
		{
			Debug.LogWarning($"[ONI_MP] {message}");
			EnsureInstance();
		}

		public static void LogError(string message, bool trigger_error_screen = true)
		{
			if (trigger_error_screen)
			{
				Debug.LogError($"[ONI_MP] {message}");
			}
			else
			{
				// If suppressing screen, still log to our console/file
				EnsureInstance();
				_instance.HandleLog($"[ONI_MP] {message}", "", LogType.Error);
			}
		}

		public static void LogException(System.Exception ex)
		{
			Debug.LogException(ex);
			EnsureInstance();
		}

		public static void LogAssert(string message)
		{
			Debug.Log($"[ONI_MP/Assert] {message}");
			EnsureInstance();
		}

		private static void EnsureInstance()
		{
			if (_instance == null)
				Init();
		}
	}
}
