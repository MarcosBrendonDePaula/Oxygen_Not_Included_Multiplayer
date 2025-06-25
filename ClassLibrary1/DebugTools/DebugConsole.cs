using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ONI_MP.DebugTools
{
    public class DebugConsole : MonoBehaviour
    {
        private static DebugConsole _instance;
        private static readonly List<LogEntry> logEntries = new List<LogEntry>();
        private static string logFilePath => Path.Combine(Application.persistentDataPath, "oni_mp_debug.log");
        
        private Vector2 scrollPos;
        private bool showConsole = false;
        private const int MaxLines = 1000;

        // Filtros
        private bool showLogs = true;
        private bool showWarnings = true;
        private bool showErrors = true;
        private bool showExceptions = true;
        private bool showAsserts = true;
        private string searchFilter = "";
        private bool autoScroll = true;
        private bool timestamps = true;
        
        // Cores e estilos
        private GUIStyle logStyle;
        private GUIStyle warnStyle;
        private GUIStyle errorStyle;
        private GUIStyle exceptionStyle;
        private GUIStyle assertStyle;
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle toggleStyle;
        private GUIStyle searchStyle;
        private GUIStyle backgroundStyle;
        
        // Controle de interface
        private float consoleWidth = 700f;
        private float consoleHeight = 500f;
        private bool isDragging = false;
        private Vector2 dragOffset;
        private Rect consoleRect;
        private bool isResizing = false;
        private Vector2 resizeStart;

        private class LogEntry
        {
            public string message;
            public string stack;
            public LogType type;
            public bool expanded;
            public System.DateTime timestamp;
            public int count = 1; // Para agrupar mensagens repetidas
        }

        public static void Init()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("ONI_MP_DebugConsole");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DebugConsole>();
        }

        public void Toggle()
        {
            showConsole = !showConsole;
        }

        private void Awake()
        {
            consoleRect = new Rect(Screen.width - consoleWidth - 20, 20, consoleWidth, consoleHeight);
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            // Inicialização dos estilos será feita no OnGUI quando necessário
        }

        private void OnGUI()
        {
            if (!showConsole) return;

            // Inicializar estilos se necessário
            if (logStyle == null)
            {
                SetupStyles();
            }

            // Desenhar console
            DrawConsole();
            
            // Bloquear apenas cliques que não são da UI do console
            if (Event.current.type == EventType.MouseDown && 
                consoleRect.Contains(Event.current.mousePosition) && 
                !isDragging && !isResizing)
            {
                // Permite que a UI do console funcione, mas bloqueia cliques que passariam através
                if (Event.current.mousePosition.y > consoleRect.y + 30) // Evita bloquear o header
                {
                    Event.current.Use();
                }
            }
        }

        private void SetupStyles()
        {
            // Estilos para diferentes tipos de log
            logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white },
                wordWrap = true,
                padding = new RectOffset(5, 5, 2, 2)
            };

            warnStyle = new GUIStyle(logStyle)
            {
                normal = { textColor = Color.yellow }
            };

            errorStyle = new GUIStyle(logStyle)
            {
                normal = { textColor = Color.red }
            };

            exceptionStyle = new GUIStyle(logStyle)
            {
                normal = { textColor = new Color(1f, 0.4f, 0.4f) }
            };

            assertStyle = new GUIStyle(logStyle)
            {
                normal = { textColor = Color.cyan }
            };

            // Estilo do cabeçalho
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            // Estilo dos botões
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                margin = new RectOffset(2, 2, 2, 2)
            };

            // Estilo dos toggles
            toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 10
            };

            // Estilo da busca
            searchStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 11
            };

            // Fundo semi-transparente mais opaco
            backgroundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.95f)) },
                border = new RectOffset(4, 4, 4, 4)
            };
        }

        private void DrawConsole()
        {
            // Fundo opaco para bloquear elementos atrás
            GUI.Box(consoleRect, "", backgroundStyle);
            
            // Área com padding interno para melhor aparência
            Rect innerRect = new Rect(consoleRect.x + 5, consoleRect.y + 5, 
                                    consoleRect.width - 10, consoleRect.height - 10);
            
            GUILayout.BeginArea(innerRect);
            
            // Cabeçalho
            DrawHeader();
            
            // Filtros e controles
            DrawFilters();
            
            // Área de logs
            DrawLogArea();
            
            // Rodapé com botões
            DrawFooter();
            
            GUILayout.EndArea();
            
            // Controle de redimensionamento
            HandleResize();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            
            GUILayout.Label("ONI MP Debug Console", headerStyle);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                showConsole = false;
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
        }

        private void DrawFilters()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            
            // Filtros por tipo
            var filteredEntries = GetFilteredEntries();
            int logCount = filteredEntries.Count(e => e.type == LogType.Log);
            int warnCount = filteredEntries.Count(e => e.type == LogType.Warning);
            int errorCount = filteredEntries.Count(e => e.type == LogType.Error);
            int exceptionCount = filteredEntries.Count(e => e.type == LogType.Exception);
            int assertCount = filteredEntries.Count(e => e.type == LogType.Assert);
            
            showLogs = GUILayout.Toggle(showLogs, $"Logs ({logCount})", toggleStyle);
            showWarnings = GUILayout.Toggle(showWarnings, $"Warnings ({warnCount})", toggleStyle);
            showErrors = GUILayout.Toggle(showErrors, $"Errors ({errorCount})", toggleStyle);
            showExceptions = GUILayout.Toggle(showExceptions, $"Exceptions ({exceptionCount})", toggleStyle);
            showAsserts = GUILayout.Toggle(showAsserts, $"Asserts ({assertCount})", toggleStyle);
            
            GUILayout.EndHorizontal();
            
            // Segunda linha de filtros
            GUILayout.BeginHorizontal();
            
            GUILayout.Label("Search:", GUILayout.Width(50));
            searchFilter = GUILayout.TextField(searchFilter, searchStyle);
            
            if (GUILayout.Button("Clear", buttonStyle, GUILayout.Width(50)))
            {
                searchFilter = "";
            }
            
            GUILayout.Space(10);
            
            autoScroll = GUILayout.Toggle(autoScroll, "Auto Scroll", toggleStyle);
            timestamps = GUILayout.Toggle(timestamps, "Timestamps", toggleStyle);
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
        }

        private void DrawLogArea()
        {
            var filteredEntries = GetFilteredEntries();
            
            // Container com fundo para a área de logs
            GUILayout.BeginVertical(GUI.skin.box);
            
            // Área de scroll
            float scrollViewHeight = consoleRect.height - 140; // Deixa espaço para header e footer
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollViewHeight));
            
            foreach (var entry in filteredEntries)
            {
                DrawLogEntry(entry);
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            
            // Auto scroll para o final
            if (autoScroll && Event.current.type == EventType.Repaint)
            {
                scrollPos.y = Mathf.Infinity;
            }
        }

        private void DrawLogEntry(LogEntry entry)
        {
            GUIStyle style = GetStyleForLogType(entry.type);
            
            string prefix = GetPrefixForLogType(entry.type);
            string timestamp = timestamps ? $"[{entry.timestamp:HH:mm:ss}] " : "";
            string countSuffix = entry.count > 1 ? $" ({entry.count})" : "";
            string displayMessage = $"{timestamp}{prefix}{entry.message}{countSuffix}";
            
            // Botão para expandir/colapsar
            if (GUILayout.Button(displayMessage, style))
            {
                entry.expanded = !entry.expanded;
            }
            
            // Stack trace expandido
            if (entry.expanded && !string.IsNullOrEmpty(entry.stack))
            {
                GUILayout.Label(entry.stack, GUI.skin.box);
            }
        }

        private void DrawFooter()
        {
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear All", buttonStyle))
            {
                logEntries.Clear();
            }
            
            if (GUILayout.Button("Save to File", buttonStyle))
            {
                SaveLogsToFile();
            }
            
            if (GUILayout.Button("Copy Last", buttonStyle))
            {
                CopyLastLogToClipboard();
            }
            
            GUILayout.FlexibleSpace();
            
            GUILayout.Label($"Total: {logEntries.Count}/{MaxLines}", GUI.skin.label);
            
            GUILayout.EndHorizontal();
        }

        private void HandleResize()
        {
            // Área de redimensionamento (canto inferior direito)
            Rect resizeArea = new Rect(consoleRect.x + consoleRect.width - 20, 
                                     consoleRect.y + consoleRect.height - 20, 20, 20);
            
            if (Event.current.type == EventType.MouseDown && resizeArea.Contains(Event.current.mousePosition))
            {
                isResizing = true;
                resizeStart = Event.current.mousePosition;
                Event.current.Use();
            }
            
            if (isResizing)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    Vector2 delta = Event.current.mousePosition - resizeStart;
                    consoleWidth = Mathf.Max(400f, consoleWidth + delta.x);
                    consoleHeight = Mathf.Max(300f, consoleHeight + delta.y);
                    
                    consoleRect.width = consoleWidth;
                    consoleRect.height = consoleHeight;
                    
                    resizeStart = Event.current.mousePosition;
                    Event.current.Use();
                }
                
                if (Event.current.type == EventType.MouseUp)
                {
                    isResizing = false;
                    Event.current.Use();
                }
            }
            
            // Desenhar indicador de redimensionamento
            if (resizeArea.Contains(Event.current.mousePosition))
            {
                GUI.Label(new Rect(resizeArea.x - 5, resizeArea.y - 5, 15, 15), "↘", 
                    new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.gray } });
            }
        }

        private List<LogEntry> GetFilteredEntries()
        {
            return logEntries.Where(entry =>
            {
                // Filtro por tipo
                bool typeMatch = (entry.type == LogType.Log && showLogs) ||
                               (entry.type == LogType.Warning && showWarnings) ||
                               (entry.type == LogType.Error && showErrors) ||
                               (entry.type == LogType.Exception && showExceptions) ||
                               (entry.type == LogType.Assert && showAsserts);
                
                if (!typeMatch) return false;
                
                // Filtro de busca
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    return entry.message.ToLower().Contains(searchFilter.ToLower()) ||
                           (!string.IsNullOrEmpty(entry.stack) && entry.stack.ToLower().Contains(searchFilter.ToLower()));
                }
                
                return true;
            }).ToList();
        }

        private GUIStyle GetStyleForLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning: return warnStyle;
                case LogType.Error: return errorStyle;
                case LogType.Exception: return exceptionStyle;
                case LogType.Assert: return assertStyle;
                default: return logStyle;
            }
        }

        private string GetPrefixForLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning: return "[WARN] ";
                case LogType.Error: return "[ERROR] ";
                case LogType.Exception: return "[EXCEPTION] ";
                case LogType.Assert: return "[ASSERT] ";
                default: return "";
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Verificar se é uma mensagem duplicada recente
            var lastEntry = logEntries.LastOrDefault();
            if (lastEntry != null && lastEntry.message == logString && lastEntry.type == type)
            {
                lastEntry.count++;
                lastEntry.timestamp = System.DateTime.Now;
                return;
            }

            logEntries.Add(new LogEntry
            {
                message = logString,
                stack = stackTrace,
                type = type,
                expanded = false,
                timestamp = System.DateTime.Now
            });

            WriteLogToFile(logString, stackTrace, type);

            if (logEntries.Count > MaxLines)
                logEntries.RemoveAt(0);
        }

        private static void WriteLogToFile(string message, string stackTrace, LogType type)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    string prefix = type == LogType.Log ? "" : $"[{type}] ";
                    writer.WriteLine($"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} - {prefix}{message}");
                    if (!string.IsNullOrEmpty(stackTrace))
                        writer.WriteLine(stackTrace);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ONI_MP] Falha ao escrever no arquivo de log: {e.Message}");
            }
        }

        private void SaveLogsToFile()
        {
            try
            {
                string exportPath = Path.Combine(Application.persistentDataPath, $"oni_mp_export_{System.DateTime.Now:yyyyMMdd_HHmmss}.log");
                using (StreamWriter writer = new StreamWriter(exportPath))
                {
                    foreach (var entry in logEntries)
                    {
                        string prefix = entry.type == LogType.Log ? "" : $"[{entry.type}] ";
                        writer.WriteLine($"{entry.timestamp:yyyy-MM-dd HH:mm:ss} - {prefix}{entry.message}");
                        if (!string.IsNullOrEmpty(entry.stack))
                            writer.WriteLine(entry.stack);
                    }
                }
                Log($"Logs exportados para: {exportPath}");
            }
            catch (System.Exception e)
            {
                LogError($"Falha ao exportar logs: {e.Message}");
            }
        }

        private void CopyLastLogToClipboard()
        {
            if (logEntries.Count > 0)
            {
                var lastEntry = logEntries.Last();
                string textToCopy = $"{lastEntry.message}";
                if (!string.IsNullOrEmpty(lastEntry.stack))
                    textToCopy += $"\n{lastEntry.stack}";
                
                GUIUtility.systemCopyBuffer = textToCopy;
                Log("Último log copiado para clipboard");
            }
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // Métodos públicos para logging (mantidos iguais)
        public static void Log(string message)
        {
            Debug.Log($"[ONI_MP] {message}");
            EnsureInstance();
            _instance.HandleLog($"{message}", "", LogType.Log);
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[ONI_MP] {message}");
            EnsureInstance();
            _instance.HandleLog($"{message}", "", LogType.Warning);
        }

        public static void LogError(string message, bool trigger_error_screen = true)
        {
            if (trigger_error_screen)
            {
                Debug.LogError($"[ONI_MP] {message}");
            }
            EnsureInstance();
            _instance.HandleLog($"[ONI_MP] {message}", "", LogType.Error);
        }

        public static void LogException(System.Exception ex)
        {
            Debug.LogException(ex);
            EnsureInstance();
            _instance.HandleLog($"{ex.Message}", ex.StackTrace, LogType.Exception);
        }

        public static void LogAssert(string message)
        {
            Debug.Log($"[ONI_MP/Assert] {message}");
            EnsureInstance();
            _instance.HandleLog($"{message}", "", LogType.Assert);
        }

        private static void EnsureInstance()
        {
            if (_instance == null)
                Init();
        }

        private void Update()
        {
            // Hotkey: Shift + F7 para abrir/fechar o console
            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.F7))
            {
                Toggle();
            }
            
            // Hotkey: Ctrl + Shift + C para limpar logs
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C))
            {
                logEntries.Clear();
            }
        }
    }
}