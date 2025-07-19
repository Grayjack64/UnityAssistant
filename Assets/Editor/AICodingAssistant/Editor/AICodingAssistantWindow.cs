using System;
using System.Threading.Tasks;
using AICodingAssistant.AI;
using AICodingAssistant.Data;
using AICodingAssistant.Scripts;
using UnityEditor;
using UnityEngine;

namespace AICodingAssistant.Editor
{
    public class AICodingAssistantWindow : EditorWindow
    {
        private int currentTabIndex = 0;
        private string[] tabOptions = { "Chat", "Scaffolder", "Settings" };
        private Vector2 scrollPosition;

        // We now manage two backends
        private AIBackend localBackend;
        private AIBackend mainBackend;
        private AIBackendType selectedMainBackendType = AIBackendType.Gemini; // Default main AI
        private PluginSettings settings;

        public AIChatController ChatController { get; private set; }
        
        private ScaffolderTab scaffolderTab;
        private ChatTab chatTab;

        // Expose both backends
        public AIBackend LocalBackend => localBackend;
        public AIBackend MainBackend => mainBackend;
        public PluginSettings Settings => settings;

        [MenuItem("Window/AI Coding Assistant")]
        public static void ShowWindow()
        {
            var window = GetWindow<AICodingAssistantWindow>("AI Coding Assistant");
            window.minSize = new Vector2(600, 800); 
        }

        private void OnEnable()
        {
            LoadSettings(); 
            InitializeBackends();

            // The ChatController now takes the local and main backends
            ChatController = new AIChatController(localBackend, mainBackend, this);
            scaffolderTab = new ScaffolderTab(this);
            chatTab = new ChatTab(this, ChatController);
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void OnGUI()
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("AI Coding Assistant settings file not found.", MessageType.Error);
                return;
            }

            currentTabIndex = GUILayout.Toolbar(currentTabIndex, tabOptions);
            
            switch (currentTabIndex)
            {
                case 0:
                    chatTab.Draw();
                    break;
                case 1:
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                    scaffolderTab.Draw();
                    EditorGUILayout.EndScrollView();
                    break;
                case 2:
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                    DrawSettingsTab();
                    EditorGUILayout.EndScrollView();
                    break;
            }
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Main AI Backend (for execution)", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            selectedMainBackendType = (AIBackendType)EditorGUILayout.EnumPopup("Backend:", selectedMainBackendType);
            if (EditorGUI.EndChangeCheck())
            {
                InitializeBackends();
                // Re-initialize controllers that depend on the backends
                ChatController = new AIChatController(localBackend, mainBackend, this);
                chatTab = new ChatTab(this, ChatController);
                scaffolderTab = new ScaffolderTab(this);
            }
            
            EditorGUI.BeginChangeCheck();
            settings.GrokApiKey = EditorGUILayout.TextField("Grok API Key", settings.GrokApiKey);
            settings.ClaudeApiKey = EditorGUILayout.TextField("Claude API Key", settings.ClaudeApiKey);
            settings.GeminiApiKey = EditorGUILayout.TextField("Gemini API Key", settings.GeminiApiKey);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local AI Backend (for planning)", EditorStyles.boldLabel);
            settings.OllamaUrl = EditorGUILayout.TextField("Ollama URL", settings.OllamaUrl);
            settings.OllamaModel = EditorGUILayout.TextField("Ollama Model", settings.OllamaModel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            settings.enablePromptLogging = EditorGUILayout.Toggle("Log Full AI Prompts", settings.enablePromptLogging);

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }
        }
        
        private void InitializeBackends()
        {
            if (settings == null) return;
            // Always create the local backend from Ollama settings
            localBackend = AIBackendFactory.CreateBackend(AIBackendType.LocalLLM, settings);
            // Create the main backend based on user selection
            mainBackend = AIBackendFactory.CreateBackend(selectedMainBackendType, settings);
        }

        private void LoadSettings()
        {
            var guids = AssetDatabase.FindAssets("t:PluginSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<PluginSettings>(path);
            }
            else
            {
                settings = ScriptableObject.CreateInstance<PluginSettings>();
                if (!AssetDatabase.IsValidFolder("Assets/Editor/AICodingAssistant/Data"))
                {
                    AssetDatabase.CreateFolder("Assets/Editor/AICodingAssistant", "Data");
                }
                AssetDatabase.CreateAsset(settings, "Assets/Editor/AICodingAssistant/Data/AICompanion_Settings.asset");
                AssetDatabase.SaveAssets();
            }
        }

        private void SaveSettings()
        {
            if (settings != null)
            {
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
