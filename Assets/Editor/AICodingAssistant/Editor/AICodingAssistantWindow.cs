using System;
using AICodingAssistant.AI;
using AICodingAssistant.Data;
using AICodingAssistant.Scripts;
using UnityEditor;
using UnityEngine;

namespace AICodingAssistant.Editor
{
    public class AICodingAssistantWindow : EditorWindow
    {
        // ... (keep all your existing variables)
        private int currentTabIndex = 0;
        private string[] tabOptions = { "Chat", "Scaffolder", "Settings" };
        private Vector2 scrollPosition;
        private AIBackend mainBackend;
        private PluginSettings settings;
        private EnhancedConsoleMonitor consoleMonitor;
        public AIChatController ChatController { get; private set; }
        private ScaffolderTab scaffolderTab;
        private ChatTab chatTab;
        private static AICodingAssistantWindow instance; // Keep a static reference to the window

        public AIBackend MainBackend => mainBackend;
        public PluginSettings Settings => settings;

        [MenuItem("Window/AI Coding Assistant")]
        public static void ShowWindow()
        {
            var window = GetWindow<AICodingAssistantWindow>("AI Coding Assistant");
            window.minSize = new Vector2(600, 800);
        }

        // ADD THIS NEW STATIC METHOD
        public static void AddSystemMessageToChat(string message)
        {
            if (instance != null && instance.ChatController != null)
            {
                // We use delayCall to make sure we're on the main thread for UI updates
                EditorApplication.delayCall += () =>
                {
                    instance.ChatController.AddMessage(message, false, true); // The new 'isSystemMessage' flag
                    instance.Repaint();
                };
            }
            else
            {
                Debug.Log($"[AICodingAssistantWindow] System Message (window not focused): {message}");
            }
        }

        private void OnEnable()
        {
            instance = this; // Set the static instance when the window is enabled
            LoadSettings();
            consoleMonitor = new EnhancedConsoleMonitor();
            consoleMonitor.StartCapturing();
            InitializeBackend();

            ChatController = new AIChatController(mainBackend, consoleMonitor, this);
            scaffolderTab = new ScaffolderTab(this);
            chatTab = new ChatTab(this, ChatController);
        }
        // ... (The rest of your AICodingAssistantWindow.cs file remains unchanged)
        private void OnDisable()
        {
            if (consoleMonitor != null) consoleMonitor.StopCapturing();
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
                case 0: chatTab.Draw(); break;
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
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Google Gemini API", EditorStyles.boldLabel);
            settings.GeminiApiKey = EditorGUILayout.TextField("Gemini API Key", settings.GeminiApiKey);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            settings.enablePromptLogging = EditorGUILayout.Toggle("Log Full AI Prompts", settings.enablePromptLogging);

            if (EditorGUI.EndChangeCheck())
            {
                // Re-initialize the backend if the API key changes
                InitializeBackend();
                ChatController = new AIChatController(mainBackend, consoleMonitor, this);
                chatTab = new ChatTab(this, ChatController);
                scaffolderTab = new ScaffolderTab(this);
                SaveSettings();
            }
        }
        
        private void InitializeBackend()
        {
            if (settings == null) return;
            // We now only create the main Gemini backend
            mainBackend = AIBackendFactory.CreateBackend(settings);
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
            if (settings != null) EditorUtility.SetDirty(settings);
        }
    }
}