using System;
using System.Threading.Tasks;
using AICodingAssistant.AI;
using AICodingAssistant.Scripts;
using UnityEditor;
using UnityEngine;

namespace AICodingAssistant.Editor
{
    public class AICodingAssistantWindow : EditorWindow
    {
        private int currentTabIndex = 0;
        // Corrected tab options array
        private string[] tabOptions = { "Chat", "Scaffolder", "Settings" };
        private Vector2 scrollPosition;

        private AIBackendType selectedBackendType = AIBackendType.LocalLLM;
        private AIBackend currentBackend;

        private EnhancedConsoleMonitor consoleMonitor;
        private CodebaseContext codebaseContext;

        public AIChatController ChatController { get; private set; }
        private AISettingsController settingsController;
        
        // Tab handlers
        private ScaffolderTab scaffolderTab;
        private ChatTab chatTab;

        public AIBackend CurrentBackend => currentBackend;

        [MenuItem("Window/AI Coding Assistant")]
        public static void ShowWindow()
        {
            GetWindow<AICodingAssistantWindow>("AI Coding Assistant");
        }

        private void OnEnable()
        {
            consoleMonitor = new EnhancedConsoleMonitor();
            consoleMonitor.StartCapturing();
            
            codebaseContext = new CodebaseContext();
            settingsController = new AISettingsController();
            settingsController.LoadSettings();

            InitializeBackend();

            // Initialize controllers and tabs
            ChatController = new AIChatController(currentBackend, consoleMonitor, codebaseContext, this);
            scaffolderTab = new ScaffolderTab(this);
            chatTab = new ChatTab(this, ChatController);
        }

        private void OnDisable()
        {
            consoleMonitor.StopCapturing();
            if (settingsController != null)
            {
                settingsController.SaveSettings();
            }
        }

        private void OnGUI()
        {
            if (settingsController.Settings == null)
            {
                EditorGUILayout.HelpBox("AI Coding Assistant settings file not found. Please create one via Assets > Create > AI Coding Assistant > Settings", MessageType.Error);
                return;
            }

            currentTabIndex = GUILayout.Toolbar(currentTabIndex, tabOptions);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Updated switch to draw the correct tab
            switch (currentTabIndex)
            {
                case 0:
                    chatTab.Draw();
                    break;
                case 1:
                    scaffolderTab.Draw(); 
                    break;
                case 2:
                    DrawSettingsTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("AI Backend", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            selectedBackendType = (AIBackendType)EditorGUILayout.EnumPopup("Backend:", selectedBackendType);
            if (EditorGUI.EndChangeCheck())
            {
                InitializeBackend();
                // Re-initialize controllers that depend on the backend
                ChatController = new AIChatController(currentBackend, consoleMonitor, codebaseContext, this);
                chatTab = new ChatTab(this, ChatController);
                scaffolderTab = new ScaffolderTab(this);
            }
            
            var settings = settingsController.Settings;
            
            EditorGUI.BeginChangeCheck();
            settings.GrokApiKey = EditorGUILayout.TextField("Grok API Key", settings.GrokApiKey);
            settings.ClaudeApiKey = EditorGUILayout.TextField("Claude API Key", settings.ClaudeApiKey);
            settings.OllamaUrl = EditorGUILayout.TextField("Ollama URL", settings.OllamaUrl);
            settings.OllamaModel = EditorGUILayout.TextField("Ollama Model", settings.OllamaModel);
            settings.GeminiApiKey = EditorGUILayout.TextField("Gemini API Key", settings.GeminiApiKey);

            if (EditorGUI.EndChangeCheck())
            {
                settingsController.SaveSettings();
            }
        }
        
        private void InitializeBackend()
        {
            if (settingsController.Settings == null) return;
            currentBackend = AIBackendFactory.CreateBackend(selectedBackendType, settingsController.Settings);
        }
    }
}
