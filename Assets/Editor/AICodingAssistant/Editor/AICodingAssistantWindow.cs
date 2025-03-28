using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using AICodingAssistant.AI;
using AICodingAssistant.Scripts;
using AICodingAssistant.Editor;
using UnityEditor;
using UnityEngine;

namespace AICodingAssistant.Editor
{
    /// <summary>
    /// Main editor window for the AI Coding Assistant
    /// </summary>
    public class AICodingAssistantWindow : EditorWindow
    {
        // Window state
        private int currentTabIndex = 0;
        private string[] tabOptions = { "Chat", "Settings" };
        private Vector2 scrollPosition;
        
        // AI settings
        private AIBackendType selectedBackendType = AIBackendType.LocalLLM;
        private AIBackend currentBackend;
        
        // Unified chat interface
        private string userQuery = "";
        private List<ChatMessage> chatHistory = new List<ChatMessage>();
        private bool includeConsoleLogs = true;
        private bool includeCodeContext = true;
        private string selectedScriptPath = "";
        private bool isProcessing = false;
        private bool includeProjectSummary = true;
        private int maxHistoryMessages = 10;
        
        // Context management
        private string projectSummary = "";
        private DateTime lastProjectSummaryUpdate = DateTime.MinValue;
        private TimeSpan projectSummaryUpdateInterval = TimeSpan.FromMinutes(10);
        
        // Settings
        private string grokApiKey = "";
        private string claudeApiKey = "";
        private string claudeModel = "claude-3-opus-20240229";
        private string[] claudeModelOptions = new string[0];
        private int selectedClaudeModelIndex = 0;
        private bool isLoadingClaudeModels = false;
        private string ollamaUrl = "http://localhost:11434";
        private string ollamaModel = "llama2";
        private string geminiApiKey = "";
        private string geminiModel = "gemini-2.0-flash";
        private string[] geminiModelOptions = new string[0];
        private int selectedGeminiModelIndex = 0;
        private bool enableEnhancedConsole = true;
        private bool enableCodeIndexing = true;
        private int maxConsoleEntries = 500;
        
        // Utilities
        private EnhancedConsoleMonitor consoleMonitor;
        private CodebaseContext codebaseContext;
        private bool codebaseInitialized = false;
        private float codebaseInitProgress = 0f;
        private UnityEngine.Object scriptObject;
        
        // Scene operations data
        private List<string> sceneHierarchy = new List<string>();
        private List<string> materials = new List<string>();
        private List<string> prefabs = new List<string>();
        private DateTime lastSceneRefreshTime = DateTime.MinValue;
        private TimeSpan sceneRefreshInterval = TimeSpan.FromSeconds(5);
        
        [MenuItem("Window/AI Coding Assistant")]
        public static void ShowWindow()
        {
            var window = GetWindow<AICodingAssistantWindow>("AI Coding Assistant");
            window.minSize = new Vector2(450, 650);
            window.Show();
        }
        
        private async void OnEnable()
        {
            // Initialize backend
            currentBackend = AIBackend.CreateBackend(selectedBackendType);
            
            // Initialize console monitor
            consoleMonitor = new EnhancedConsoleMonitor(maxConsoleEntries);
            consoleMonitor.StartCapturing();
            
            // Initialize codebase context (async to avoid freezing the UI)
            codebaseContext = new CodebaseContext();
            
            // Initialize change tracker (singleton pattern, just need to access it)
            _ = ChangeTracker.Instance;
            
            // Subscribe to compilation events
            ChangeTracker.Instance.OnCompilationCompleted += HandleCompilationCompleted;
            
            // Load settings
            LoadSettings();
            
            // Initialize codebase indexing if enabled
            if (enableCodeIndexing)
            {
                EditorApplication.delayCall += async () => 
                {
                    await InitializeCodebaseContext();
                };
            }
            
            // Add welcome message to chat history
            chatHistory.Add(new ChatMessage
            {
                IsUser = false,
                Content = "Welcome to the Unity AI Coding Assistant! I can help with:\n\n" +
                          "• Answering questions about Unity development\n" +
                          "• Analyzing and improving your code\n" +
                          "• Generating new scripts based on your requirements\n" +
                          "• Searching your codebase for relevant information\n" +
                          "• Manipulating scene objects directly through chat commands\n\n" +
                          "Just describe what you need in natural language, and I'll help you out!",
                Timestamp = DateTime.Now
            });
            
            // Initial scene data refresh
            RefreshSceneData();
            
            // Subscribe to hierarchy changes
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }
        
        private void OnDisable()
        {
            // Stop capturing console logs
            consoleMonitor.StopCapturing();
            
            // Unsubscribe from compilation events
            ChangeTracker.Instance.OnCompilationCompleted -= HandleCompilationCompleted;
            
            // Unsubscribe from hierarchy changes
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            
            // Save settings
            SaveSettings();
        }
        
        private void OnHierarchyChanged()
        {
            // Mark scene data for refresh on next repaint
            lastSceneRefreshTime = DateTime.MinValue;
        }
        
        private void RefreshSceneData()
        {
            try
            {
                // Get scene hierarchy
                sceneHierarchy = AICodingAssistant.Scripts.SceneManipulationService.GetSceneHierarchy();
                
                // Get materials
                var materialsResult = AICodingAssistant.Scripts.SceneManipulationService.GetAllMaterials();
                materials = materialsResult.Success ? materialsResult.Value : new List<string>();
                
                // Get prefabs
                var prefabsResult = AICodingAssistant.Scripts.SceneManipulationService.GetAllPrefabs();
                prefabs = prefabsResult.Success ? prefabsResult.Value : new List<string>();
                
                // Update refresh time
                lastSceneRefreshTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error refreshing scene data: {ex.Message}");
            }
        }
        
        private async Task InitializeCodebaseContext()
        {
            codebaseInitialized = false;
            codebaseInitProgress = 0f;
            
            try
            {
                await codebaseContext.Initialize();
                codebaseInitialized = true;
                
                // Generate project summary after initialization
                GenerateProjectSummary();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing codebase context: {ex.Message}");
            }
            
            codebaseInitProgress = 1f;
            Repaint();
        }
        
        private void OnGUI()
        {
            // Check if scene data needs refreshing
            if (DateTime.Now - lastSceneRefreshTime > sceneRefreshInterval)
            {
                RefreshSceneData();
            }
            
            // Tab selection
            currentTabIndex = GUILayout.Toolbar(currentTabIndex, tabOptions);
            
            // Show codebase initialization message if needed
            if (!codebaseInitialized && enableCodeIndexing)
            {
                EditorGUILayout.HelpBox("Codebase indexing is in progress...", MessageType.Info);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), codebaseInitProgress, "Indexing Codebase");
                EditorGUILayout.Space(5);
            }
            
            // Display current tab without an outer scroll view for the Chat tab
            // to prevent nested scroll views
            if (currentTabIndex == 0) // Chat tab
            {
                DrawUnifiedChatTab();
            }
            else
            {
                // For other tabs, keep the scroll view
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                switch (currentTabIndex)
                {
                    case 1: // Settings
                        DrawSettingsTab();
                        break;
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        
        private void DrawUnifiedChatTab()
        {
            // Calculate appropriate height for the chat area based on window size
            // Chat area should take up approximately 50% of the window height
            float totalHeight = position.height;
            float chatAreaHeight = Math.Max(200, totalHeight * 0.5f);
            float controlsHeight = Math.Min(250, totalHeight - chatAreaHeight - 20); // 20 for padding
            
            // Top section - Message area - dedicated scroll view for chat messages
            EditorGUILayout.BeginVertical(GUILayout.Height(chatAreaHeight));
            
            // Dedicated scroll view for messages with a distinctive background
            var bgColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.9f, 0.9f, 0.9f);
            var oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            // Create a visual container for the scroll area
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(chatAreaHeight));
            GUI.backgroundColor = oldBgColor;
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(chatAreaHeight));
            
            // Draw chat history
            foreach (var message in chatHistory)
            {
                // Skip system messages in the UI display
                if (message.IsSystemMessage)
                    continue;
                    
                DrawChatMessage(message);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical(); // End of visual container
            EditorGUILayout.EndVertical();
            
            // Add a separator
            EditorGUILayout.Space(5);
            
            // Bottom section - Input area
            EditorGUILayout.BeginVertical(GUILayout.Height(controlsHeight));
            
            // Draw controls for code context and console logs
            EditorGUILayout.BeginHorizontal();
            
            includeCodeContext = EditorGUILayout.ToggleLeft("Include Code Context", includeCodeContext, GUILayout.Width(150));
            includeConsoleLogs = EditorGUILayout.ToggleLeft("Include Console Logs", includeConsoleLogs, GUILayout.Width(150));
            includeProjectSummary = EditorGUILayout.ToggleLeft("Include Project Summary", includeProjectSummary, GUILayout.Width(180));
            
            if (GUILayout.Button("Regenerate Project Summary", GUILayout.Width(180)))
            {
                GenerateProjectSummary();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Draw script selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected Script:", GUILayout.Width(100));
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(selectedScriptPath);
            EditorGUI.EndDisabledGroup();
            
            // Script object field
            UnityEngine.Object newScriptObject = EditorGUILayout.ObjectField(scriptObject, typeof(MonoScript), false, GUILayout.Width(100));
            
            if (newScriptObject != scriptObject)
            {
                scriptObject = newScriptObject;
                if (scriptObject != null)
                {
                    selectedScriptPath = AssetDatabase.GetAssetPath(scriptObject);
                }
            }
            
            if (GUILayout.Button("Analyze Script", GUILayout.Width(100)))
            {
                AnalyzeSelectedScript();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Scene Operations Section - First row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scene Operations:", EditorStyles.boldLabel, GUILayout.Width(120));
            
            if (GUILayout.Button("List Scene", GUILayout.Width(100)))
            {
                string result = ProcessSceneCommand("scene.list");
                AddSystemMessage($"Scene Hierarchy:\n```\n{result}\n```");
            }
            
            if (GUILayout.Button("List Materials", GUILayout.Width(120)))
            {
                string result = ProcessSceneCommand("scene.materials");
                AddSystemMessage($"Available Materials:\n```\n{result}\n```");
            }
            
            if (GUILayout.Button("List Prefabs", GUILayout.Width(100)))
            {
                string result = ProcessSceneCommand("scene.prefabs");
                AddSystemMessage($"Available Prefabs:\n```\n{result}\n```");
            }
            
            if (GUILayout.Button("Refresh Scene Data", GUILayout.Width(150)))
            {
                RefreshSceneData();
                AddSystemMessage("Scene data refreshed.");
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Scene Operations Section - Second row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Create:", GUILayout.Width(50));
            
            if (GUILayout.Button("Empty Object", GUILayout.Width(100)))
            {
                string objName = "NewObject_" + DateTime.Now.ToString("HHmmss");
                string result = ProcessSceneCommand($"scene.create {objName}");
                AddSystemMessage($"Create empty object result:\n```\n{result}\n```");
            }
            
            string[] primitiveOptions = { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };
            int primitiveIndex = EditorGUILayout.Popup(0, primitiveOptions, GUILayout.Width(80));
            
            if (GUILayout.Button("Create Primitive", GUILayout.Width(120)))
            {
                string objName = primitiveOptions[primitiveIndex] + "_" + DateTime.Now.ToString("HHmmss");
                string result = ProcessSceneCommand($"scene.primitive {primitiveOptions[primitiveIndex]} {objName}");
                AddSystemMessage($"Create primitive result:\n```\n{result}\n```");
            }
            
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            
            // Search the codebase
            if (GUILayout.Button("Search Codebase", GUILayout.Width(120)))
            {
                string searchTerm = ExtractSearchTerm(userQuery);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    PerformCodebaseSearch(searchTerm);
                }
                else
                {
                    // Add a system message about the missing search term
                    AddSystemMessage("I tried to search the codebase, but I couldn't determine what to search for.");
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // User input field
            EditorGUILayout.BeginHorizontal();
            
            // Handle pressing Enter in the text field
            GUI.SetNextControlName("UserQueryField");
            
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return 
                && GUI.GetNameOfFocusedControl() == "UserQueryField" && !Event.current.shift)
            {
                if (!string.IsNullOrEmpty(userQuery) && !isProcessing)
                {
                    SendChatMessage();
                    Event.current.Use(); // Consume the event
                }
            }
            
            // Multiline text area with word wrap
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            
            // Ensure there's space for the text area by calculating minimum height
            float textAreaHeight = Mathf.Max(60, controlsHeight - 170); // Reserve space for other UI elements
            userQuery = EditorGUILayout.TextArea(userQuery, textAreaStyle, GUILayout.Height(textAreaHeight));
            
            EditorGUILayout.EndHorizontal();
            
            // Send button and status - Use a more prominent style for the send button
            EditorGUILayout.BeginHorizontal(GUILayout.Height(40));
            
            // Always ensure the send button has enough height to be visible
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(userQuery) || isProcessing);
            
            // Create a more visible send button
            GUIStyle sendButtonStyle = new GUIStyle(GUI.skin.button);
            sendButtonStyle.fontStyle = FontStyle.Bold;
            sendButtonStyle.fontSize = 12;
            
            if (GUILayout.Button("SEND", sendButtonStyle, GUILayout.Width(80), GUILayout.Height(30)))
            {
                SendChatMessage();
            }
            EditorGUI.EndDisabledGroup();
            
            // Display processing status
            if (isProcessing)
            {
                EditorGUILayout.LabelField("Processing...", EditorStyles.boldLabel);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private async void SendChatMessage()
        {
            if (string.IsNullOrWhiteSpace(userQuery) || isProcessing)
            {
                return;
            }
            
            // Add user message to chat history
            chatHistory.Add(new ChatMessage
            {
                IsUser = true,
                Content = userQuery,
                Timestamp = DateTime.Now
            });
            
            // Clear the input field
            string query = userQuery;
            userQuery = "";
            
            isProcessing = true;
            Repaint();
            
            try
            {
                // Check if project summary needs refresh
                if (includeProjectSummary && (string.IsNullOrEmpty(projectSummary) || DateTime.Now - lastProjectSummaryUpdate > projectSummaryUpdateInterval))
                {
                    await Task.Run(() => GenerateProjectSummary());
                }
                
                // Refresh scene data if needed
                if (DateTime.Now - lastSceneRefreshTime > sceneRefreshInterval)
                {
                    RefreshSceneData();
                }
                
                // Process and prepare the prompt
                string prompt = BuildAIPrompt(query);
                
                // Add processing message
                var processingMessage = new ChatMessage
                {
                    IsUser = false,
                    Content = "Processing...",
                    Timestamp = DateTime.Now,
                    IsNew = true
                };
                chatHistory.Add(processingMessage);
                Repaint();
                
                // Send to AI backend
                string response = await currentBackend.SendRequest(prompt);
                
                // Remove the processing message
                chatHistory.Remove(processingMessage);
                
                // Add AI response to chat history
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = response,
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
                
                // Limit chat history size
                while (chatHistory.Count > maxHistoryMessages * 2) // *2 because each exchange is 2 messages
                {
                    chatHistory.RemoveAt(0);
                }
                
                // Parse the response for commands
                await ProcessAIResponse(query, response);
            }
            catch (Exception ex)
            {
                // Add error message to chat history
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = $"Error: {ex.Message}",
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
                Debug.LogError($"Error processing query: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }
        
        private string BuildAIPrompt(string userMessage)
        {
            StringBuilder prompt = new StringBuilder();

            // Create a header for the prompt with context
            prompt.AppendLine("You are an AI assistant for Unity development.");
            prompt.AppendLine("You can help with coding, debugging, and explaining Unity concepts.");
            prompt.AppendLine("You are knowledgeable about C#, Unity Engine APIs, and game development patterns.");
            prompt.AppendLine();

            // Include scene manipulation instructions
            prompt.AppendLine("You can manipulate the Unity scene using the following commands enclosed in backticks (`):");
            prompt.AppendLine("- `scene.list` - List all GameObjects in the scene hierarchy");
            prompt.AppendLine("- `scene.create NAME [PARENT_PATH]` - Create a new GameObject, optionally as a child (example: `scene.create Player` or `scene.create MainCamera /Player`)");
            prompt.AppendLine("- `scene.position PATH X Y Z [LOCAL]` - Set position (example: `scene.position Player 0 1 0 true` for local position)");
            prompt.AppendLine("- `scene.rotation PATH X Y Z [LOCAL]` - Set rotation in degrees (example: `scene.rotation Player 0 90 0`)");
            prompt.AppendLine("- `scene.scale PATH X Y Z` - Set scale (example: `scene.scale Player 2 2 2`)");
            prompt.AppendLine("- `scene.primitive TYPE NAME [PARENT_PATH]` - Create primitive (example: `scene.primitive Cube MyCube`)");
            prompt.AppendLine("- `scene.components PATH` - List components on a GameObject (example: `scene.components Player`)");
            prompt.AppendLine("- `scene.addcomponent PATH COMPONENT_NAME` - Add component (example: `scene.addcomponent Player Rigidbody`)");
            prompt.AppendLine("- `scene.setfield PATH COMPONENT FIELD VALUE` - Set component field (example: `scene.setfield Player Rigidbody mass 5`)");
            prompt.AppendLine("- `scene.material PATH MATERIAL_PATH [INDEX]` - Set material (example: `scene.material Player Materials/Red`)");
            prompt.AppendLine("- `scene.prefab PREFAB_PATH [PARENT_PATH]` - Instantiate prefab (example: `scene.prefab Prefabs/Enemy`)");
            prompt.AppendLine("- `scene.delete PATH` - Delete GameObject (example: `scene.delete Enemy`)");
            prompt.AppendLine("- `scene.materials` - List all materials in the project");
            prompt.AppendLine("- `scene.prefabs` - List all prefabs in the project");
            prompt.AppendLine();
            
            prompt.AppendLine("Follow these guidelines for scene manipulation:");
            prompt.AppendLine("1. Always check for failed commands in my responses and adapt your approach accordingly");
            prompt.AppendLine("2. For component operations, use the exact component name (e.g., 'Rigidbody', not 'RigidBody')");
            prompt.AppendLine("3. For path parameters, use the full hierarchy path (e.g., 'Parent/Child')");
            prompt.AppendLine("4. For positions and rotations, specify if they're local with an optional 'true' parameter");
            prompt.AppendLine("5. You can use scene commands directly in your responses; they will be automatically executed");
            prompt.AppendLine("6. After each scene command, you'll receive feedback on whether it succeeded or failed");
            prompt.AppendLine();
            
            // Add step-by-step verification workflow guidelines
            prompt.AppendLine("When adding components or creating scripts, follow this step-by-step approach:");
            prompt.AppendLine("1. Start by creating a clear plan of action with all the steps needed to accomplish the task");
            prompt.AppendLine("2. Execute only ONE step at a time (one scene command or one script addition)");
            prompt.AppendLine("3. After each step, verify it worked correctly by checking the command result");
            prompt.AppendLine("4. Only proceed to the next step after confirming the previous step was successful");
            prompt.AppendLine("5. If a step fails, troubleshoot that specific issue before attempting to continue");
            prompt.AppendLine("6. After component addition, use `scene.components` to verify the component was added correctly");
            prompt.AppendLine("7. For script additions, wait for feedback on whether the script compiled successfully");
            prompt.AppendLine();

            // Add project context information if requested
            if (includeProjectSummary)
            {
                prompt.AppendLine("Project Information:");
                prompt.AppendLine(projectSummary);
                prompt.AppendLine();
            }

            // Include console logs if requested
            if (includeConsoleLogs && consoleMonitor != null)
            {
                var recentLogs = consoleMonitor.GetRecentLogs(10);
                if (recentLogs.Count > 0)
                {
                    prompt.AppendLine("Recent Console Logs:");
                    foreach (var log in recentLogs)
                    {
                        string prefix = "";
                        if (log.Type == LogType.Error || log.Type == LogType.Exception)
                            prefix = "[ERROR] ";
                        else if (log.Type == LogType.Warning)
                            prefix = "[WARNING] ";
                        
                        prompt.AppendLine($"{prefix}{log.Message}");
                    }
                    prompt.AppendLine();
                }
            }
            
            // Include scene information to provide context for scene manipulation
            prompt.AppendLine("Current Scene Information:");
            prompt.AppendLine($"Total GameObjects: {sceneHierarchy.Count}");
            prompt.AppendLine($"Available Materials: {materials.Count}");
            prompt.AppendLine($"Available Prefabs: {prefabs.Count}");
            prompt.AppendLine();
            
            // Include a sample of GameObjects in the scene for better context
            if (sceneHierarchy.Count > 0)
            {
                prompt.AppendLine("Top-level GameObjects:");
                int count = 0;
                foreach (var path in sceneHierarchy)
                {
                    if (!path.Contains("/") && count < 10) // Only top-level objects, max 10
                    {
                        prompt.AppendLine($"- {path}");
                        count++;
                    }
                }
                prompt.AppendLine();
            }

            // Include selected script content if available
            if (includeCodeContext && !string.IsNullOrEmpty(selectedScriptPath))
            {
                prompt.AppendLine($"Selected Script ({selectedScriptPath}):");
                prompt.AppendLine("```csharp");
                prompt.AppendLine(File.ReadAllText(selectedScriptPath));
                prompt.AppendLine("```");
                prompt.AppendLine();
            }

            // Include recent compilation errors if any
            if (codebaseInitialized && includeCodeContext)
            {
                var changeTracker = ChangeTracker.Instance;
                if (changeTracker != null && changeTracker.HasCompilationErrors())
                {
                    prompt.AppendLine("Recent Compilation Errors:");
                    var errors = changeTracker.GetRecentCompilationErrors();
                    foreach (var error in errors)
                    {
                        prompt.AppendLine($"- {error.File}: Line {error.Line}: {error.Message} ({error.ErrorCode})");
                    }
                    prompt.AppendLine();
                }
            }

            // Add chat history with limited messages
            prompt.AppendLine("Chat History:");
            
            // Get a subset of chat history based on maxHistoryMessages
            int startIndex = Math.Max(0, chatHistory.Count - maxHistoryMessages);
            for (int i = startIndex; i < chatHistory.Count; i++)
            {
                var message = chatHistory[i];
                // Include all user messages and only the non-system AI messages in the displayed history
                if (message.IsUser || !message.IsSystemMessage)
                {
                    prompt.AppendLine(message.IsUser ? $"User: {message.Content}" : $"AI: {message.Content}");
                }
                // Include all system messages in the AI prompt
                else if (message.IsSystemMessage)
                {
                    prompt.AppendLine($"System: {message.Content}");
                }
            }
            
            // Add the current query
            prompt.AppendLine($"User: {userMessage}");
            prompt.AppendLine("AI:");

            return prompt.ToString();
        }
        
        private async Task ProcessAIResponse(string query, string response)
        {
            // Track any failed commands to inform the AI in the next prompt
            List<string> failedCommands = new List<string>();
            bool sceneWasModified = false;

            // Process any scene commands in the response
            if (response.Contains("scene."))
            {
                // Extract scene commands from the response using regex
                var sceneCommandMatches = System.Text.RegularExpressions.Regex.Matches(response, @"`(scene\.[^`]+)`");
                
                foreach (System.Text.RegularExpressions.Match match in sceneCommandMatches)
                {
                    string command = match.Groups[1].Value;
                    string result = ProcessSceneCommand(command);
                    
                    // Check if the command failed (result contains "Failed")
                    bool commandFailed = result.Contains("Failed");
                    if (commandFailed)
                    {
                        failedCommands.Add($"{command}: {result}");
                    }
                    
                    // Mark scene as modified if a command was successful
                    if (!commandFailed && command.StartsWith("scene.") && 
                        !command.StartsWith("scene.list") && 
                        !command.StartsWith("scene.materials") && 
                        !command.StartsWith("scene.prefabs"))
                    {
                        sceneWasModified = true;
                    }
                    
                    // Insert execution results below each command in the chat
                    chatHistory[chatHistory.Count - 1].Content = chatHistory[chatHistory.Count - 1].Content.Replace(
                        match.Value, 
                        $"{match.Value}\n\n**Command Result:**\n```\n{result}\n```");
                }
                
                // Refresh scene data if any commands modified the scene
                if (sceneWasModified)
                {
                    RefreshSceneData();
                    
                    // Add a system message to inform the AI about the step completion status
                    StringBuilder stepCompletionMessage = new StringBuilder();
                    stepCompletionMessage.AppendLine("Step execution status:");
                    
                    if (failedCommands.Count == 0)
                    {
                        stepCompletionMessage.AppendLine("✅ Last scene operation step completed successfully.");
                        
                        // Add current scene state for verification context
                        stepCompletionMessage.AppendLine("\nCurrent scene state after the operation:");
                        stepCompletionMessage.AppendLine($"- Total GameObjects: {sceneHierarchy.Count}");
                        
                        // Include recently modified objects if possible
                        var lastCommandPath = sceneCommandMatches.Cast<System.Text.RegularExpressions.Match>()
                            .LastOrDefault()?.Groups[1].Value;
                            
                        if (!string.IsNullOrEmpty(lastCommandPath))
                        {
                            // Extract the object path from the command
                            string objectPath = ExtractObjectPathFromCommand(lastCommandPath);
                            
                            if (!string.IsNullOrEmpty(objectPath) && sceneHierarchy.Contains(objectPath))
                            {
                                stepCompletionMessage.AppendLine($"- Modified object: {objectPath}");
                                
                                // Get components of the modified object
                                var componentsResult = AICodingAssistant.Scripts.SceneManipulationService.GetComponents(objectPath);
                                if (componentsResult.Success)
                                {
                                    stepCompletionMessage.AppendLine("- Components:");
                                    foreach (var component in componentsResult.Value)
                                    {
                                        stepCompletionMessage.AppendLine($"  - {component}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        stepCompletionMessage.AppendLine("❌ Last scene operation step failed. Please fix before continuing.");
                    }
                    
                    // Add this as a system message that's visible to the AI but not the user
                    AddSystemMessage(stepCompletionMessage.ToString());
                }
                
                // Repaint to show the updated response with command results
                Repaint();
                
                // If there were failed commands, send a follow-up message to the AI
                if (failedCommands.Count > 0)
                {
                    Debug.Log($"Detected {failedCommands.Count} failed scene commands");
                    
                    // Add a system message to inform about failed commands
                    string failureMessage = "The following scene commands failed:\n\n";
                    foreach (string failure in failedCommands)
                    {
                        failureMessage += $"- {failure}\n";
                    }
                    failureMessage += "\nPlease acknowledge these failures and adjust your approach accordingly in your next response.";
                    
                    // Add this as a system message that's visible to the AI but not the user
                    // This will be included in the next AI prompt but won't be shown in the chat UI
                    AddSystemMessage(failureMessage);
                }
            }
            
            // Check for code edit suggestions
            var codeEdits = CodeEditUtility.ExtractEdits(response);
            if (codeEdits.Count > 0)
            {
                // Show code edit dialog
                ShowCodeEditDialog(codeEdits);
            }
            
            // Extract possible action commands using regex
            if (query.ToLowerInvariant().Contains("search for") || query.ToLowerInvariant().Contains("find in codebase"))
            {
                // Extract search term
                string searchTerm = ExtractSearchTerm(query);
                if (!string.IsNullOrEmpty(searchTerm) && codebaseInitialized)
                {
                    await PerformCodebaseSearch(searchTerm);
                }
            }
            else if (query.ToLowerInvariant().Contains("analyze") || query.ToLowerInvariant().Contains("improve code"))
            {
                // Handle code analysis request
                if (!string.IsNullOrEmpty(selectedScriptPath))
                {
                    await AnalyzeSelectedScript();
                }
            }
            else if (query.ToLowerInvariant().Contains("generate") || query.ToLowerInvariant().Contains("create script"))
            {
                // Extract code blocks for potential script creation
                string codeBlock = ExtractCodeBlock(response);
                if (!string.IsNullOrEmpty(codeBlock))
                {
                    ShowCreateScriptDialog(codeBlock);
                }
            }
            
            // If the query is about scene manipulation, provide a tutorial message if no scene commands were used
            if ((query.ToLowerInvariant().Contains("scene") || 
                 query.ToLowerInvariant().Contains("object") || 
                 query.ToLowerInvariant().Contains("gameobject") ||
                 query.ToLowerInvariant().Contains("create") ||
                 query.ToLowerInvariant().Contains("position") ||
                 query.ToLowerInvariant().Contains("rotation")) && 
                !response.Contains("`scene."))
            {
                AddSystemMessage("Tip: You can ask me to manipulate the scene directly using commands like `scene.create`, `scene.position`, etc. I'll execute these commands and show you the results.");
            }
        }
        
        /// <summary>
        /// Adds a system message that's only visible to the AI, not to the user in the chat UI
        /// </summary>
        private void AddSystemMessage(string content)
        {
            // Create a special type of message that will be included in prompts to the AI
            // but won't be displayed in the chat UI
            ChatMessage systemMessage = new ChatMessage
            {
                Content = content,
                IsUser = false,
                Timestamp = DateTime.Now,
                IsNew = false,
                IsSystemMessage = true  // Mark as system message
            };
            
            chatHistory.Add(systemMessage);
        }
        
        /// <summary>
        /// Shows a dialog to review and apply code edits
        /// </summary>
        private void ShowCodeEditDialog(List<CodeEdit> edits)
        {
            if (edits == null || edits.Count == 0)
                return;
                
            // Group edits by file
            Dictionary<string, List<CodeEdit>> editsByFile = new Dictionary<string, List<CodeEdit>>();
            
            foreach (var edit in edits)
            {
                if (edit.Type == EditType.FullFileEdit && !string.IsNullOrEmpty(edit.FilePath))
                {
                    if (!editsByFile.ContainsKey(edit.FilePath))
                    {
                        editsByFile[edit.FilePath] = new List<CodeEdit>();
                    }
                    
                    editsByFile[edit.FilePath].Add(edit);
                }
                else if (edit.Type != EditType.FullFileEdit && !string.IsNullOrEmpty(selectedScriptPath))
                {
                    // For non-full-file edits, use the selected script
                    if (!editsByFile.ContainsKey(selectedScriptPath))
                    {
                        editsByFile[selectedScriptPath] = new List<CodeEdit>();
                    }
                    
                    editsByFile[selectedScriptPath].Add(edit);
                }
            }
            
            // If there are edits to apply
            if (editsByFile.Count > 0)
            {
                StringBuilder confirmMessage = new StringBuilder();
                confirmMessage.AppendLine("The AI has suggested the following code edits:");
                confirmMessage.AppendLine();
                
                bool containsNewFiles = false;
                
                foreach (var fileEntry in editsByFile)
                {
                    bool fileExists = File.Exists(fileEntry.Key);
                    if (!fileExists) 
                    {
                        containsNewFiles = true;
                    }
                    
                    confirmMessage.AppendLine($"File: {fileEntry.Key} {(fileExists ? "" : "(New file)")}");
                    confirmMessage.AppendLine($"- {fileEntry.Value.Count} edit(s)");
                }
                
                confirmMessage.AppendLine();
                if (containsNewFiles)
                {
                    confirmMessage.AppendLine("NOTE: Some files don't exist yet and will be created.");
                    confirmMessage.AppendLine();
                }
                
                confirmMessage.AppendLine("Would you like to apply these edits? (Backups will be created for existing files)");
                
                if (EditorUtility.DisplayDialog("Code Edits Available", confirmMessage.ToString(), "Apply Edits", "Cancel"))
                {
                    // Apply edits to each file
                    foreach (var fileEntry in editsByFile)
                    {
                        if (CodeEditUtility.ApplyEdits(fileEntry.Key, fileEntry.Value))
                        {
                            chatHistory.Add(new ChatMessage
                            {
                                IsUser = false,
                                Content = $"✅ Successfully {(File.Exists(fileEntry.Key) ? "applied edits to" : "created")} {fileEntry.Key}",
                                Timestamp = DateTime.Now,
                                IsNew = true
                            });
                            
                            // Add system message for step verification
                            AddSystemMessage($"Script step completed successfully: {(File.Exists(fileEntry.Key) ? "Modified" : "Created")} {fileEntry.Key}.\n\nPlease verify this step worked correctly before proceeding to the next step in your plan.");
                        }
                        else
                        {
                            chatHistory.Add(new ChatMessage
                            {
                                IsUser = false,
                                Content = $"❌ Failed to {(File.Exists(fileEntry.Key) ? "apply edits to" : "create")} {fileEntry.Key}",
                                Timestamp = DateTime.Now,
                                IsNew = true
                            });
                            
                            // Add system message for step failure
                            AddSystemMessage($"Script step failed: Could not {(File.Exists(fileEntry.Key) ? "modify" : "create")} {fileEntry.Key}.\n\nPlease troubleshoot this issue before continuing with your plan.");
                        }
                    }
                    
                    Repaint();
                }
            }
        }
        
        /// <summary>
        /// Shows a dialog to create a new script from AI-generated code
        /// </summary>
        private void ShowCreateScriptDialog(string codeContent)
        {
            if (string.IsNullOrEmpty(codeContent))
                return;
                
            // Detect class name for suggested file name
            string suggestedFileName = "NewScript.cs";
            Match classMatch = Regex.Match(codeContent, @"class\s+(\w+)");
            if (classMatch.Success && classMatch.Groups.Count > 1)
            {
                suggestedFileName = classMatch.Groups[1].Value + ".cs";
            }
            
            // Show warning if the file appears to already exist
            string suggestedPath = Path.Combine("Assets", suggestedFileName);
            if (File.Exists(suggestedPath))
            {
                suggestedPath = Path.Combine("Assets", "Generated_" + suggestedFileName);
            }
            
            // Prompt for file location
            string savePath = EditorUtility.SaveFilePanel(
                "Save Generated Script",
                Path.GetDirectoryName(suggestedPath),
                Path.GetFileName(suggestedPath),
                "cs");
                
            if (!string.IsNullOrEmpty(savePath))
            {
                // Convert absolute path to project-relative path if possible
                string projectPath = Path.GetDirectoryName(Application.dataPath);
                if (savePath.StartsWith(projectPath))
                {
                    savePath = savePath.Substring(projectPath.Length + 1);
                }
                
                if (CodeEditUtility.CreateScript(savePath, codeContent))
                {
                    chatHistory.Add(new ChatMessage
                    {
                        IsUser = false,
                        Content = $"✅ Successfully created script at {savePath}",
                        Timestamp = DateTime.Now,
                        IsNew = true
                    });
                    
                    // Add system message for step verification
                    AddSystemMessage($"Script creation step completed successfully: Created {savePath}.\n\nPlease verify this script functions correctly before proceeding to the next step in your plan. Check for any compilation errors or runtime issues.");
                    
                    // Open the file in the editor
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(savePath);
                    if (asset != null)
                    {
                        AssetDatabase.OpenAsset(asset);
                    }
                }
                else
                {
                    chatHistory.Add(new ChatMessage
                    {
                        IsUser = false,
                        Content = $"❌ Failed to create script at {savePath}",
                        Timestamp = DateTime.Now,
                        IsNew = true
                    });
                    
                    // Add system message for step failure
                    AddSystemMessage($"Script creation step failed: Could not create {savePath}.\n\nPlease troubleshoot this issue before continuing with your plan.");
                }
                
                Repaint();
            }
        }
        
        private string ExtractSearchTerm(string query)
        {
            // Simple extraction - more sophisticated parsing could be implemented
            string[] searchPhrases = { "search for", "find in codebase", "look for" };
            foreach (var phrase in searchPhrases)
            {
                int index = query.ToLowerInvariant().IndexOf(phrase);
                if (index >= 0)
                {
                    return query.Substring(index + phrase.Length).Trim();
                }
            }
            return "";
        }
        
        private async Task PerformCodebaseSearch(string searchTerm)
        {
            if (!codebaseInitialized || string.IsNullOrWhiteSpace(searchTerm))
            {
                return;
            }
            
            try
            {
                var results = codebaseContext.Search(searchTerm);
                
                if (results.Count == 0)
                {
                    chatHistory.Add(new ChatMessage
                    {
                        IsUser = false,
                        Content = $"I searched for '{searchTerm}' but didn't find any results in the codebase.",
                        Timestamp = DateTime.Now,
                        IsNew = true
                    });
                    return;
                }
                
                StringBuilder resultMessage = new StringBuilder();
                resultMessage.AppendLine($"Here are the search results for '{searchTerm}':");
                resultMessage.AppendLine();
                
                for (int i = 0; i < Math.Min(results.Count, 5); i++)
                {
                    var result = results[i];
                    resultMessage.AppendLine($"**{result.FilePath}:{result.LineNumber}**");
                    resultMessage.AppendLine($"```csharp\n{result.Line}\n```");
                    
                    // Get a bit of context around the match
                    string context = codebaseContext.GetFileContent(result.FilePath, 
                        Math.Max(1, result.LineNumber - 3), 
                        result.LineNumber + 3);
                    
                    if (!string.IsNullOrEmpty(context))
                    {
                        resultMessage.AppendLine("Context:");
                        resultMessage.AppendLine($"```csharp\n{context}\n```");
                    }
                    
                    resultMessage.AppendLine();
                }
                
                if (results.Count > 5)
                {
                    resultMessage.AppendLine($"... and {results.Count - 5} more results.");
                }
                
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = resultMessage.ToString(),
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
            }
            catch (Exception ex)
            {
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = $"Error searching codebase: {ex.Message}",
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
                Debug.LogError($"Error searching codebase: {ex.Message}");
            }
        }
        
        private async Task AnalyzeSelectedScript()
        {
            if (string.IsNullOrEmpty(selectedScriptPath))
            {
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = "Please select a script to analyze first.",
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
                return;
            }
            
            string scriptContent = ScriptUtility.ReadScriptContent(selectedScriptPath);
            if (string.IsNullOrEmpty(scriptContent))
            {
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = "Error: Could not read script content.",
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
                return;
            }
            
            // This is just feedback, the actual analysis is done by the AI
            chatHistory.Add(new ChatMessage
            {
                IsUser = false,
                Content = $"I've analyzed the script at {selectedScriptPath}. Please see my feedback above.",
                Timestamp = DateTime.Now,
                IsNew = true
            });
        }
        
        private void GenerateProjectSummary()
        {
            if (!codebaseInitialized)
            {
                return;
            }
            
            try
            {
                StringBuilder summary = new StringBuilder();
                
                // Get all analyzed files
                var allFiles = codebaseContext.GetAnalyzedFiles();
                
                summary.AppendLine($"Project contains {allFiles.Count} scripts.");
                
                // Count namespaces and classes
                Dictionary<string, int> namespaceCount = new Dictionary<string, int>();
                int totalClasses = 0;
                int totalInterfaces = 0;
                
                // Find key scripts
                List<string> potentialMainScripts = new List<string>();
                
                foreach (var file in allFiles)
                {
                    var symbols = ScriptUtility.ExtractSymbols(file);
                    
                    // Count classes
                    totalClasses += symbols["classes"].Count;
                    totalInterfaces += symbols["interfaces"].Count;
                    
                    // Count namespaces
                    foreach (var ns in symbols["namespaces"])
                    {
                        if (!namespaceCount.ContainsKey(ns.Name))
                        {
                            namespaceCount[ns.Name] = 0;
                        }
                        namespaceCount[ns.Name]++;
                    }
                    
                    // Check if this might be a main/important script
                    if (file.Contains("Manager") || file.Contains("Controller") || file.Contains("Main") || 
                        file.Contains("Game") || file.Contains("Core"))
                    {
                        potentialMainScripts.Add(file);
                    }
                }
                
                // Add namespace summary
                if (namespaceCount.Count > 0)
                {
                    summary.AppendLine("\nMain namespaces:");
                    foreach (var ns in namespaceCount.OrderByDescending(kv => kv.Value).Take(5))
                    {
                        summary.AppendLine($"- {ns.Key} ({ns.Value} references)");
                    }
                }
                
                // Add key scripts
                if (potentialMainScripts.Count > 0)
                {
                    summary.AppendLine("\nKey scripts:");
                    foreach (var script in potentialMainScripts.Take(Math.Min(5, potentialMainScripts.Count)))
                    {
                        summary.AppendLine($"- {script}");
                    }
                }
                
                // Add stats
                summary.AppendLine($"\nProject stats: {totalClasses} classes, {totalInterfaces} interfaces");
                
                // Store the summary
                projectSummary = summary.ToString();
                lastProjectSummaryUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating project summary: {ex.Message}");
                projectSummary = "Error generating project summary.";
            }
        }
        
        private string FormatCodeBlocks(string text)
        {
            // This is a simple implementation - in a real editor with rich text support,
            // you would actually style the code blocks differently
            return text;
        }
        
        /// <summary>
        /// Processes scene manipulation commands from the AI
        /// </summary>
        /// <param name="command">The scene command to process</param>
        /// <returns>Result message</returns>
        private string ProcessSceneCommand(string command)
        {
            try
            {
                string[] parts = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length == 0)
                    return "Invalid scene command format";
                    
                string action = parts[0].ToLower();
                
                switch (action)
                {
                    case "scene.list":
                        var sceneHierarchy = AICodingAssistant.Scripts.SceneManipulationService.GetSceneHierarchy();
                        return "Scene Hierarchy:\n" + string.Join("\n", sceneHierarchy);
                        
                    case "scene.create":
                        if (parts.Length < 2)
                            return "Usage: scene.create [name] [optional:parent_path]";
                            
                        string name = parts[1];
                        string parentPath = parts.Length > 2 ? string.Join(" ", parts, 2, parts.Length - 2) : null;
                        
                        var createResult = AICodingAssistant.Scripts.SceneManipulationService.CreateEmptyGameObject(name, parentPath);
                        return createResult.Message;
                        
                    case "scene.primitive":
                        if (parts.Length < 3)
                            return "Usage: scene.primitive [type] [name] [optional:parent_path]";
                            
                        string primitiveTypeStr = parts[1];
                        if (!Enum.TryParse(primitiveTypeStr, true, out PrimitiveType primitiveType))
                            return $"Invalid primitive type: {primitiveTypeStr}. Valid types are: Cube, Sphere, Capsule, Cylinder, Plane, Quad";
                            
                        string primitiveName = parts[2];
                        string primitiveParentPath = parts.Length > 3 ? string.Join(" ", parts, 3, parts.Length - 3) : null;
                        
                        var primitiveResult = AICodingAssistant.Scripts.SceneManipulationService.CreatePrimitiveGameObject(
                            primitiveType, primitiveName, primitiveParentPath);
                        return primitiveResult.Message;
                        
                    case "scene.position":
                        if (parts.Length < 5)
                            return "Usage: scene.position [path] [x] [y] [z] [optional:isLocal]";
                            
                        string posPath = parts[1];
                        
                        if (!float.TryParse(parts[2], out float posX) ||
                            !float.TryParse(parts[3], out float posY) ||
                            !float.TryParse(parts[4], out float posZ))
                            return "Invalid position values. Please provide numerical values for x, y, z.";
                            
                        bool posIsLocal = parts.Length > 5 && bool.TryParse(parts[5], out bool localPos) ? localPos : true;
                        
                        var positionResult = AICodingAssistant.Scripts.SceneManipulationService.SetPosition(
                            posPath, posX, posY, posZ, posIsLocal);
                        return positionResult.Message;
                        
                    case "scene.rotation":
                        if (parts.Length < 5)
                            return "Usage: scene.rotation [path] [x] [y] [z] [optional:isLocal]";
                            
                        string rotPath = parts[1];
                        
                        if (!float.TryParse(parts[2], out float rotX) ||
                            !float.TryParse(parts[3], out float rotY) ||
                            !float.TryParse(parts[4], out float rotZ))
                            return "Invalid rotation values. Please provide numerical values for x, y, z.";
                            
                        bool rotIsLocal = parts.Length > 5 && bool.TryParse(parts[5], out bool localRot) ? localRot : true;
                        
                        var rotationResult = AICodingAssistant.Scripts.SceneManipulationService.SetRotation(
                            rotPath, rotX, rotY, rotZ, rotIsLocal);
                        return rotationResult.Message;
                        
                    case "scene.scale":
                        if (parts.Length < 5)
                            return "Usage: scene.scale [path] [x] [y] [z]";
                            
                        string scalePath = parts[1];
                        
                        if (!float.TryParse(parts[2], out float scaleX) ||
                            !float.TryParse(parts[3], out float scaleY) ||
                            !float.TryParse(parts[4], out float scaleZ))
                            return "Invalid scale values. Please provide numerical values for x, y, z.";
                            
                        var scaleResult = AICodingAssistant.Scripts.SceneManipulationService.SetScale(
                            scalePath, scaleX, scaleY, scaleZ);
                        return scaleResult.Message;
                        
                    case "scene.components":
                        if (parts.Length < 2)
                            return "Usage: scene.components [path]";
                            
                        string componentsPath = parts[1];
                        var componentsResult = AICodingAssistant.Scripts.SceneManipulationService.GetComponents(componentsPath);
                        
                        if (!componentsResult.Success)
                            return componentsResult.Message;
                            
                        return $"Components on {componentsPath}:\n" + string.Join("\n", componentsResult.Value);
                        
                    case "scene.addcomponent":
                        if (parts.Length < 3)
                            return "Usage: scene.addcomponent [path] [componentName]";
                            
                        string componentPath = parts[1];
                        string componentName = parts[2];
                        
                        var addComponentResult = AICodingAssistant.Scripts.SceneManipulationService.AddComponent(
                            componentPath, componentName);
                        return addComponentResult.Message;
                        
                    case "scene.setfield":
                        if (parts.Length < 5)
                            return "Usage: scene.setfield [path] [componentName] [fieldName] [value]";
                            
                        string fieldPath = parts[1];
                        string fieldComponentName = parts[2];
                        string fieldName = parts[3];
                        string fieldValue = string.Join(" ", parts, 4, parts.Length - 4);
                        
                        var setFieldResult = AICodingAssistant.Scripts.SceneManipulationService.SetComponentField(
                            fieldPath, fieldComponentName, fieldName, fieldValue);
                        return setFieldResult.Message;
                        
                    case "scene.material":
                        if (parts.Length < 3)
                            return "Usage: scene.material [path] [materialPath] [optional:materialIndex]";
                            
                        string materialPath = parts[1];
                        string materialAssetPath = parts[2];
                        int materialIndex = parts.Length > 3 && int.TryParse(parts[3], out int matIndex) ? matIndex : 0;
                        
                        var materialResult = AICodingAssistant.Scripts.SceneManipulationService.SetMaterial(
                            materialPath, materialAssetPath, materialIndex);
                        return materialResult.Message;
                        
                    case "scene.prefab":
                        if (parts.Length < 2)
                            return "Usage: scene.prefab [prefabPath] [optional:parent_path]";
                            
                        string prefabPath = parts[1];
                        string prefabParentPath = parts.Length > 2 ? string.Join(" ", parts, 2, parts.Length - 2) : null;
                        
                        var prefabResult = AICodingAssistant.Scripts.SceneManipulationService.InstantiatePrefab(
                            prefabPath, prefabParentPath);
                        return prefabResult.Message;
                        
                    case "scene.delete":
                        if (parts.Length < 2)
                            return "Usage: scene.delete [path]";
                            
                        string deletePath = parts[1];
                        
                        var deleteResult = AICodingAssistant.Scripts.SceneManipulationService.DeleteGameObject(deletePath);
                        return deleteResult.Message;
                        
                    case "scene.materials":
                        var materialsResult = AICodingAssistant.Scripts.SceneManipulationService.GetAllMaterials();
                        if (!materialsResult.Success)
                            return materialsResult.Message;
                            
                        return "Available Materials:\n" + string.Join("\n", materialsResult.Value);
                        
                    case "scene.prefabs":
                        var prefabsResult = AICodingAssistant.Scripts.SceneManipulationService.GetAllPrefabs();
                        if (!prefabsResult.Success)
                            return prefabsResult.Message;
                            
                        return "Available Prefabs:\n" + string.Join("\n", prefabsResult.Value);
                        
                    default:
                        return $"Unknown scene command: {action}";
                }
            }
            catch (Exception ex)
            {
                return $"Error processing scene command: {ex.Message}";
            }
        }
        
        #region Settings Tab
        
        private void DrawSettingsTab()
        {
            // Settings code (same as before)
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("AI Coding Assistant Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // AI Backend Selection
            EditorGUILayout.LabelField("AI Backend Selection", EditorStyles.boldLabel);
            string[] backendOptions = { "Grok (xAI)", "Claude (Anthropic)", "Local LLM (Ollama)", "Google Gemini" };
            int currentBackendIndex = (int)selectedBackendType;
            int newBackendIndex = EditorGUILayout.Popup("Backend:", currentBackendIndex, backendOptions);
            
            if (newBackendIndex != currentBackendIndex)
            {
                selectedBackendType = (AIBackendType)newBackendIndex;
                currentBackend = AIBackend.CreateBackend(selectedBackendType);
                
                // Update backend-specific settings
                if (currentBackend is OllamaBackend ollamaBackend)
                {
                    ollamaBackend.SetServerUrl(ollamaUrl);
                    ollamaBackend.SetModel(ollamaModel);
                }
                else if (currentBackend is GrokBackend grokBackend && !string.IsNullOrEmpty(grokApiKey))
                {
                    grokBackend.SetApiKey(grokApiKey);
                }
                else if (currentBackend is ClaudeBackend claudeBackend && !string.IsNullOrEmpty(claudeApiKey))
                {
                    claudeBackend.SetApiKey(claudeApiKey);
                    claudeBackend.SetModel(claudeModel);
                }
                else if (currentBackend is GeminiBackend geminiBackend && !string.IsNullOrEmpty(geminiApiKey))
                {
                    geminiBackend.SetApiKey(geminiApiKey);
                    geminiBackend.SetModel(geminiModel);
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Conversation settings
            EditorGUILayout.LabelField("Conversation Settings", EditorStyles.boldLabel);
            maxHistoryMessages = EditorGUILayout.IntSlider("Max History Exchanges", maxHistoryMessages, 2, 20);
            
            EditorGUILayout.Space(10);
            
            // Grok settings
            if (selectedBackendType == AIBackendType.Grok)
            {
                EditorGUILayout.LabelField("Grok (xAI) Settings", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("API Key:", GUILayout.Width(80));
                grokApiKey = EditorGUILayout.PasswordField(grokApiKey);
                EditorGUILayout.EndHorizontal();
            }
            
            // Claude settings
            if (selectedBackendType == AIBackendType.Claude)
            {
                EditorGUILayout.LabelField("Claude (Anthropic) Settings", EditorStyles.boldLabel);
                
                // API Key
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("API Key:", GUILayout.Width(80));
                string newClaudeApiKey = EditorGUILayout.PasswordField(claudeApiKey);
                if (newClaudeApiKey != claudeApiKey)
                {
                    claudeApiKey = newClaudeApiKey;
                    // Trigger refresh of available models when API key changes
                    if (!string.IsNullOrEmpty(claudeApiKey))
                    {
                        EditorApplication.delayCall += RefreshClaudeModels;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // Model selection dropdown
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Model:", GUILayout.Width(80));
                
                // Show loading indicator or dropdown
                if (isLoadingClaudeModels)
                {
                    EditorGUILayout.LabelField("Loading models...", EditorStyles.boldLabel);
                }
                else
                {
                    // Show dropdown if we have models, otherwise show a button to fetch models
                    if (claudeModelOptions.Length > 0)
                    {
                        // Ensure our selected index is valid
                        if (selectedClaudeModelIndex >= claudeModelOptions.Length)
                        {
                            selectedClaudeModelIndex = 0;
                        }
                        
                        int newSelectedIndex = EditorGUILayout.Popup(selectedClaudeModelIndex, claudeModelOptions);
                        if (newSelectedIndex != selectedClaudeModelIndex)
                        {
                            selectedClaudeModelIndex = newSelectedIndex;
                            UpdateClaudeModelFromSelection();
                        }
                        
                        // Show current model ID for debugging
                        EditorGUILayout.LabelField($"ID: {claudeModel}", EditorStyles.miniLabel);
                    }
                    else
                    {
                        // If we have an API key but no models, show a refresh button
                        if (!string.IsNullOrEmpty(claudeApiKey))
                        {
                            EditorGUILayout.LabelField("No models loaded.", EditorStyles.boldLabel);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Enter API Key to load models");
                        }
                    }
                }
                
                // Refresh button
                if (!isLoadingClaudeModels && !string.IsNullOrEmpty(claudeApiKey) && 
                    GUILayout.Button("⟳", GUILayout.Width(25)))
                {
                    RefreshClaudeModels();
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Debug button for model loading
                if (!string.IsNullOrEmpty(claudeApiKey))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Force Reload Models", GUILayout.Width(150)))
                    {
                        // Clear the cache and force a reload
                        try
                        {
                            Debug.Log("Forcing model reload...");
                            if (currentBackend is ClaudeBackend claudeBackend)
                            {
                                // Access the backend's private fields via reflection to force a reload
                                var field = typeof(ClaudeBackend).GetField("lastModelsFetchTime", 
                                    System.Reflection.BindingFlags.NonPublic | 
                                    System.Reflection.BindingFlags.Instance);
                                    
                                if (field != null)
                                {
                                    field.SetValue(claudeBackend, DateTime.MinValue);
                                    Debug.Log("Reset model cache timestamp");
                                }
                                
                                var modelsField = typeof(ClaudeBackend).GetField("availableModels", 
                                    System.Reflection.BindingFlags.NonPublic | 
                                    System.Reflection.BindingFlags.Instance);
                                    
                                if (modelsField != null)
                                {
                                    modelsField.SetValue(claudeBackend, new List<ClaudeModel>());
                                    Debug.Log("Cleared model cache");
                                }
                            }
                            
                            // Now trigger a refresh
                            claudeModelOptions = new string[0]; // Clear UI options
                            RefreshClaudeModels();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error forcing model reload: {ex.Message}");
                        }
                    }
                    
                    // Show model count for debugging
                    EditorGUILayout.LabelField($"Models: {claudeModelOptions.Length}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            // Gemini settings
            if (selectedBackendType == AIBackendType.Gemini)
            {
                EditorGUILayout.LabelField("Google Gemini Settings", EditorStyles.boldLabel);
                
                // API Key
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("API Key:", GUILayout.Width(80));
                string newGeminiApiKey = EditorGUILayout.PasswordField(geminiApiKey);
                if (newGeminiApiKey != geminiApiKey)
                {
                    geminiApiKey = newGeminiApiKey;
                    
                    // Update the backend
                    if (currentBackend is GeminiBackend geminiBackend1)
                    {
                        geminiBackend1.SetApiKey(geminiApiKey);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // Model selection
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Model:", GUILayout.Width(80));
                
                // Load model options if they're not loaded yet
                if (geminiModelOptions.Length == 0 && currentBackend is GeminiBackend geminiBackend2)
                {
                    geminiModelOptions = geminiBackend2.GetModelDisplayNames();
                    
                    // Find the current model index
                    string currentDisplayName = geminiBackend2.GetCurrentModelDisplayName();
                    selectedGeminiModelIndex = Array.IndexOf(geminiModelOptions, currentDisplayName);
                    if (selectedGeminiModelIndex < 0)
                    {
                        selectedGeminiModelIndex = 0;
                    }
                }
                
                if (geminiModelOptions.Length > 0)
                {
                    int newModelIndex = EditorGUILayout.Popup(selectedGeminiModelIndex, geminiModelOptions);
                    if (newModelIndex != selectedGeminiModelIndex)
                    {
                        selectedGeminiModelIndex = newModelIndex;
                        
                        // Update the model
                        if (currentBackend is GeminiBackend geminiBackend3)
                        {
                            string modelId = geminiBackend3.GetModelIdFromDisplayName(geminiModelOptions[selectedGeminiModelIndex]);
                            geminiModel = modelId;
                            geminiBackend3.SetModel(modelId);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No models available", EditorStyles.boldLabel);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Ollama settings
            if (selectedBackendType == AIBackendType.LocalLLM)
            {
                EditorGUILayout.LabelField("Local LLM (Ollama) Settings", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("URL:", GUILayout.Width(80));
                ollamaUrl = EditorGUILayout.TextField(ollamaUrl);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Model:", GUILayout.Width(80));
                ollamaModel = EditorGUILayout.TextField(ollamaModel);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(10);
            
            // Advanced settings
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
            
            bool newEnableCodeIndexing = EditorGUILayout.Toggle("Enable Codebase Indexing", enableCodeIndexing);
            if (newEnableCodeIndexing != enableCodeIndexing)
            {
                enableCodeIndexing = newEnableCodeIndexing;
                if (enableCodeIndexing && !codebaseInitialized)
                {
                    EditorApplication.delayCall += async () => 
                    {
                        await InitializeCodebaseContext();
                    };
                }
            }
            
            bool newEnableEnhancedConsole = EditorGUILayout.Toggle("Enable Enhanced Console", enableEnhancedConsole);
            if (newEnableEnhancedConsole != enableEnhancedConsole)
            {
                enableEnhancedConsole = newEnableEnhancedConsole;
                if (enableEnhancedConsole && consoleMonitor != null)
                {
                    consoleMonitor.StartCapturing();
                }
                else if (!enableEnhancedConsole && consoleMonitor != null)
                {
                    consoleMonitor.StopCapturing();
                }
            }
            
            int newMaxConsoleEntries = EditorGUILayout.IntSlider("Max Console Entries", maxConsoleEntries, 100, 1000);
            if (newMaxConsoleEntries != maxConsoleEntries)
            {
                maxConsoleEntries = newMaxConsoleEntries;
                // Note: We would need to recreate the console monitor to apply this, so we'll just save it for next time
            }
            
            EditorGUILayout.Space(20);
            
            // Context update interval
            float intervalMinutes = (float)projectSummaryUpdateInterval.TotalMinutes;
            float newIntervalMinutes = EditorGUILayout.Slider("Context Update Interval (min)", intervalMinutes, 1, 60);
            if (Math.Abs(newIntervalMinutes - intervalMinutes) > 0.1f)
            {
                projectSummaryUpdateInterval = TimeSpan.FromMinutes(newIntervalMinutes);
            }
            
            // Codebase management
            EditorGUILayout.LabelField("Codebase Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Re-Index Codebase"))
            {
                EditorApplication.delayCall += async () => 
                {
                    await InitializeCodebaseContext();
                };
            }
            
            EditorGUI.BeginDisabledGroup(!enableEnhancedConsole || consoleMonitor == null);
            if (GUILayout.Button("Clear Console History"))
            {
                consoleMonitor?.ClearLogs();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // Save button
            if (GUILayout.Button("Save Settings"))
            {
                SaveSettings();
                EditorUtility.DisplayDialog("Settings Saved", "Your settings have been saved.", "OK");
            }
        }
        
        /// <summary>
        /// Refresh the list of available Claude models
        /// </summary>
        private async void RefreshClaudeModels()
        {
            if (string.IsNullOrEmpty(claudeApiKey) || isLoadingClaudeModels)
            {
                Debug.Log("Cannot fetch Claude models: API key not configured or already loading models");
                return;
            }
            
            isLoadingClaudeModels = true;
            Repaint(); // Ensure UI updates to show loading state
            
            try
            {
                Debug.Log("Fetching Claude models...");
                
                // Ensure we have a Claude backend instance
                ClaudeBackend claudeBackend = null;
                
                if (currentBackend is ClaudeBackend)
                {
                    claudeBackend = (ClaudeBackend)currentBackend;
                }
                else
                {
                    claudeBackend = new ClaudeBackend();
                    claudeBackend.SetApiKey(claudeApiKey);
                }
                
                // Load models
                var models = await claudeBackend.ListModels();
                Debug.Log($"Retrieved {models.Count} Claude models from API");
                
                // Update UI options
                claudeModelOptions = new string[models.Count];
                for (int i = 0; i < models.Count; i++)
                {
                    claudeModelOptions[i] = models[i].DisplayName;
                    Debug.Log($"Model {i}: {models[i].DisplayName} (ID: {models[i].Id})");
                    
                    // If this is the currently selected model, update the index
                    if (models[i].Id == claudeModel)
                    {
                        selectedClaudeModelIndex = i;
                    }
                }
                
                // If we couldn't find the current model in the list, reset to first option
                if (selectedClaudeModelIndex >= claudeModelOptions.Length && claudeModelOptions.Length > 0)
                {
                    selectedClaudeModelIndex = 0;
                    UpdateClaudeModelFromSelection();
                }
                
                // If the current backend is Claude, update it with the current model
                if (currentBackend is ClaudeBackend)
                {
                    ((ClaudeBackend)currentBackend).SetModel(claudeModel);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading Claude models: {ex.Message}\n{ex.StackTrace}");
                
                // Add some default options for fallback
                claudeModelOptions = new string[]
                {
                    "Claude 3 Opus",
                    "Claude 3 Sonnet",
                    "Claude 3 Haiku"
                };
                
                // Try to find a sensible default based on the current model ID
                selectedClaudeModelIndex = 0;
                if (claudeModel.Contains("opus"))
                {
                    selectedClaudeModelIndex = 0;
                }
                else if (claudeModel.Contains("sonnet"))
                {
                    selectedClaudeModelIndex = 1;
                }
                else if (claudeModel.Contains("haiku"))
                {
                    selectedClaudeModelIndex = 2;
                }
                
                // Show error notification
                EditorUtility.DisplayDialog("Error Loading Models", 
                    "Failed to load Claude models. Using default options instead.\n\nError: " + ex.Message, 
                    "OK");
            }
            finally
            {
                isLoadingClaudeModels = false;
                Repaint(); // Ensure UI updates after loading completes
            }
        }
        
        /// <summary>
        /// Update the selected Claude model based on the dropdown selection
        /// </summary>
        private async void UpdateClaudeModelFromSelection()
        {
            if (selectedClaudeModelIndex < 0 || selectedClaudeModelIndex >= claudeModelOptions.Length)
            {
                return;
            }
            
            try
            {
                // Get the selected model name
                string selectedDisplayName = claudeModelOptions[selectedClaudeModelIndex];
                
                // Convert to model ID
                ClaudeBackend claudeBackend = null;
                
                if (currentBackend is ClaudeBackend)
                {
                    claudeBackend = (ClaudeBackend)currentBackend;
                }
                else
                {
                    claudeBackend = new ClaudeBackend();
                    claudeBackend.SetApiKey(claudeApiKey);
                }
                
                string modelId = await claudeBackend.GetModelIdFromDisplayName(selectedDisplayName);
                
                // Update current model
                claudeModel = modelId;
                
                // If the current backend is Claude, update it
                if (currentBackend is ClaudeBackend)
                {
                    ((ClaudeBackend)currentBackend).SetModel(claudeModel);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating Claude model: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            grokApiKey = EditorPrefs.GetString("AICodingAssistant_GrokApiKey", "");
            claudeApiKey = EditorPrefs.GetString("AICodingAssistant_ClaudeApiKey", "");
            claudeModel = EditorPrefs.GetString("AICodingAssistant_ClaudeModel", "claude-3-opus-20240229");
            ollamaUrl = EditorPrefs.GetString("AICodingAssistant_OllamaUrl", "http://localhost:11434");
            ollamaModel = EditorPrefs.GetString("AICodingAssistant_OllamaModel", "llama2");
            geminiApiKey = EditorPrefs.GetString("AICodingAssistant_GeminiApiKey", "");
            geminiModel = EditorPrefs.GetString("AICodingAssistant_GeminiModel", "gemini-2.0-flash");
            
            // Advanced settings
            enableCodeIndexing = EditorPrefs.GetBool("AICodingAssistant_EnableCodeIndexing", true);
            enableEnhancedConsole = EditorPrefs.GetBool("AICodingAssistant_EnableEnhancedConsole", true);
            maxConsoleEntries = EditorPrefs.GetInt("AICodingAssistant_MaxConsoleEntries", 500);
            maxHistoryMessages = EditorPrefs.GetInt("AICodingAssistant_MaxHistoryMessages", 10);
            float intervalMinutes = EditorPrefs.GetFloat("AICodingAssistant_UpdateInterval", 10);
            projectSummaryUpdateInterval = TimeSpan.FromMinutes(intervalMinutes);
            
            // Load selected backend
            selectedBackendType = (AIBackendType)EditorPrefs.GetInt("AICodingAssistant_SelectedBackend", (int)AIBackendType.LocalLLM);
            currentBackend = AIBackend.CreateBackend(selectedBackendType);
            
            // Set backend-specific settings
            if (currentBackend is OllamaBackend ollamaBackend)
            {
                ollamaBackend.SetServerUrl(ollamaUrl);
                ollamaBackend.SetModel(ollamaModel);
            }
            else if (currentBackend is GrokBackend grokBackend && !string.IsNullOrEmpty(grokApiKey))
            {
                grokBackend.SetApiKey(grokApiKey);
            }
            else if (currentBackend is ClaudeBackend claudeBackend && !string.IsNullOrEmpty(claudeApiKey))
            {
                claudeBackend.SetApiKey(claudeApiKey);
                claudeBackend.SetModel(claudeModel);
                
                // Load Claude models if API key is configured
                if (!string.IsNullOrEmpty(claudeApiKey))
                {
                    EditorApplication.delayCall += RefreshClaudeModels;
                }
            }
            else if (currentBackend is GeminiBackend geminiBackend && !string.IsNullOrEmpty(geminiApiKey))
            {
                geminiBackend.SetApiKey(geminiApiKey);
                geminiBackend.SetModel(geminiModel);
            }
        }
        
        private void SaveSettings()
        {
            EditorPrefs.SetString("AICodingAssistant_GrokApiKey", grokApiKey);
            EditorPrefs.SetString("AICodingAssistant_ClaudeApiKey", claudeApiKey);
            EditorPrefs.SetString("AICodingAssistant_ClaudeModel", claudeModel);
            EditorPrefs.SetString("AICodingAssistant_OllamaUrl", ollamaUrl);
            EditorPrefs.SetString("AICodingAssistant_OllamaModel", ollamaModel);
            EditorPrefs.SetString("AICodingAssistant_GeminiApiKey", geminiApiKey);
            EditorPrefs.SetString("AICodingAssistant_GeminiModel", geminiModel);
            EditorPrefs.SetInt("AICodingAssistant_SelectedBackend", (int)selectedBackendType);
            
            // Advanced settings
            EditorPrefs.SetBool("AICodingAssistant_EnableCodeIndexing", enableCodeIndexing);
            EditorPrefs.SetBool("AICodingAssistant_EnableEnhancedConsole", enableEnhancedConsole);
            EditorPrefs.SetInt("AICodingAssistant_MaxConsoleEntries", maxConsoleEntries);
            EditorPrefs.SetInt("AICodingAssistant_MaxHistoryMessages", maxHistoryMessages);
            EditorPrefs.SetFloat("AICodingAssistant_UpdateInterval", (float)projectSummaryUpdateInterval.TotalMinutes);
            
            // Update backend settings
            if (currentBackend is OllamaBackend ollamaBackend)
            {
                ollamaBackend.SetServerUrl(ollamaUrl);
                ollamaBackend.SetModel(ollamaModel);
            }
            else if (currentBackend is GrokBackend grokBackend)
            {
                grokBackend.SetApiKey(grokApiKey);
            }
            else if (currentBackend is ClaudeBackend claudeBackend)
            {
                claudeBackend.SetApiKey(claudeApiKey);
                claudeBackend.SetModel(claudeModel);
            }
            else if (currentBackend is GeminiBackend geminiBackend)
            {
                geminiBackend.SetApiKey(geminiApiKey);
                geminiBackend.SetModel(geminiModel);
            }
        }
        
        #endregion
        
        #region Compilation Error Handling
        
        /// <summary>
        /// Handles compilation completed events and automatically suggests fixes for errors
        /// </summary>
        private async void HandleCompilationCompleted(CompilationResult result)
        {
            // Only respond to compilation failures
            if (result.Success || result.Errors.Count == 0 || result.ChangesWithErrors.Count == 0)
            {
                return;
            }
            
            try
            {
                // Build a message about the compilation errors
                StringBuilder errorMessage = new StringBuilder();
                errorMessage.AppendLine("⚠️ **I detected compilation errors in your recent changes:**");
                errorMessage.AppendLine();
                
                foreach (var change in result.ChangesWithErrors)
                {
                    errorMessage.AppendLine($"**File:** {Path.GetFileName(change.FilePath)}");
                    errorMessage.AppendLine($"**Change:** {change.ChangeType} at {change.Timestamp.ToString("HH:mm:ss")}");
                    errorMessage.AppendLine($"**Description:** {change.Description}");
                    errorMessage.AppendLine("**Errors:**");
                    
                    foreach (var error in change.CompilationErrors)
                    {
                        errorMessage.AppendLine($"- {error}");
                    }
                    
                    errorMessage.AppendLine();
                }
                
                // Add the error message to the chat
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = errorMessage.ToString(),
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
                
                Repaint();
                
                // Analyze errors and suggest fixes
                await SuggestFixesForCompilationErrors(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling compilation result: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Analyzes compilation errors and suggests fixes based on the context of recent changes
        /// </summary>
        private async Task SuggestFixesForCompilationErrors(CompilationResult result)
        {
            if (result.ChangesWithErrors.Count == 0)
            {
                return;
            }
            
            try
            {
                // Show "thinking" message
                var processingMessage = new ChatMessage
                {
                    IsUser = false,
                    Content = "🔍 Analyzing errors and suggesting fixes...",
                    Timestamp = DateTime.Now,
                    IsNew = true
                };
                chatHistory.Add(processingMessage);
                Repaint();
                
                // Build a prompt focused on fixing the specific errors
                StringBuilder fixPrompt = new StringBuilder();
                fixPrompt.AppendLine("You need to help fix compilation errors that resulted from recent code changes.");
                fixPrompt.AppendLine("\nRECENT CHANGES THAT CAUSED ERRORS:");
                
                // Add details of changes with errors
                foreach (var change in result.ChangesWithErrors)
                {
                    fixPrompt.AppendLine($"File: {change.FilePath}");
                    fixPrompt.AppendLine($"Change Type: {change.ChangeType}");
                    fixPrompt.AppendLine($"Description: {change.Description}");
                    
                    // Read current content of the file to provide context
                    string fileContent = ScriptUtility.ReadScriptContent(change.FilePath);
                    if (!string.IsNullOrEmpty(fileContent))
                    {
                        fixPrompt.AppendLine("\nCurrent file content:");
                        fixPrompt.AppendLine("```csharp");
                        fixPrompt.AppendLine(fileContent);
                        fixPrompt.AppendLine("```");
                    }
                    
                    // Add compilation errors
                    fixPrompt.AppendLine("\nCompilation Errors:");
                    foreach (var error in change.CompilationErrors)
                    {
                        fixPrompt.AppendLine($"- {error}");
                    }
                    
                    fixPrompt.AppendLine();
                }
                
                // Get the entire change history for context
                fixPrompt.AppendLine("\nFULL CHANGE CONTEXT:");
                fixPrompt.AppendLine(ChangeTracker.Instance.GetChangesSummary());
                
                // Add instruction for the response
                fixPrompt.AppendLine("\nPlease analyze these errors and provide specific fixes. If appropriate, use the code edit formatting described in your system instructions.");
                fixPrompt.AppendLine("Be concise but clear in your explanation. Focus only on fixing these specific compilation errors.");
                
                // Send to AI backend
                string response = await currentBackend.SendRequest(fixPrompt.ToString());
                
                // Remove the processing message
                chatHistory.Remove(processingMessage);
                
                // Add AI response to chat history
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = response,
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
                
                Repaint();
                
                // Process any code edit suggestions in the response
                var codeEdits = CodeEditUtility.ExtractEdits(response);
                if (codeEdits.Count > 0)
                {
                    ShowCodeEditDialog(codeEdits);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error suggesting fixes: {ex.Message}");
                
                // Add error message
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = $"Error analyzing compilation errors: {ex.Message}",
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
            }
        }
        
        #endregion
        
        #region Utilities
        
        /// <summary>
        /// Extract code block from markdown-formatted text
        /// </summary>
        private string ExtractCodeBlock(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            
            // Look for code blocks marked with ```csharp or ``` (common in markdown)
            const string codeBlockStart = "```csharp";
            const string altCodeBlockStart = "```cs";
            const string simpleCodeBlockStart = "```";
            const string codeBlockEnd = "```";
            
            int startIndex = text.IndexOf(codeBlockStart);
            if (startIndex < 0)
            {
                startIndex = text.IndexOf(altCodeBlockStart);
            }
            if (startIndex < 0)
            {
                startIndex = text.IndexOf(simpleCodeBlockStart);
            }
            
            if (startIndex < 0)
            {
                return null; // No code block found
            }
            
            // Move to the end of the start marker
            startIndex = text.IndexOf('\n', startIndex);
            if (startIndex < 0)
            {
                return null;
            }
            
            startIndex++; // Skip the newline
            
            // Find the end of the code block
            int endIndex = text.IndexOf(codeBlockEnd, startIndex);
            if (endIndex < 0)
            {
                return null;
            }
            
            // Extract the code between the markers
            return text.Substring(startIndex, endIndex - startIndex).Trim();
        }
        
        /// <summary>
        /// Draws a single chat message in the UI
        /// </summary>
        private void DrawChatMessage(ChatMessage message)
        {
            // Calculate approximate height based on content
            float contentHeight = EditorStyles.textArea.CalcHeight(
                new GUIContent(message.Content), 
                EditorGUIUtility.currentViewWidth - 40);
            
            // Minimum height
            contentHeight = Mathf.Max(contentHeight, 40f);
            
            // Message container
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Message header (User/AI + timestamp)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                message.IsUser ? "You:" : "AI Assistant:", 
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                message.Timestamp.ToString("HH:mm:ss"), 
                GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
            
            // Message content
            GUIStyle messageStyle = new GUIStyle(EditorStyles.textArea);
            messageStyle.wordWrap = true;
            messageStyle.richText = true;
            
            // Format code blocks in AI responses for better readability
            string formattedContent = message.IsUser 
                ? message.Content 
                : FormatCodeBlocks(message.Content);
            
            EditorGUILayout.TextArea(formattedContent, messageStyle, GUILayout.Height(contentHeight));
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            
            // Ensure we scroll to the bottom when new messages arrive
            if (Event.current.type == EventType.Layout && message.IsNew)
            {
                message.IsNew = false;
                scrollPosition.y = float.MaxValue; // Scroll to bottom
                Repaint();
            }
        }
        
        #endregion
        
        /// <summary>
        /// Extracts the object path from a scene command
        /// </summary>
        /// <param name="command">The scene command</param>
        /// <returns>The extracted object path, or empty string if not found</returns>
        private string ExtractObjectPathFromCommand(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command))
                {
                    return string.Empty;
                }
                
                string[] parts = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length < 2)
                {
                    return string.Empty;
                }
                
                string action = parts[0].ToLower();
                
                // For most commands, the object path is the second parameter
                switch (action)
                {
                    case "scene.create":
                    case "scene.primitive":
                        // For creation commands, return the name/path that was created
                        return parts[1];
                        
                    case "scene.position":
                    case "scene.rotation":
                    case "scene.scale":
                    case "scene.components":
                    case "scene.addcomponent":
                    case "scene.material":
                    case "scene.delete":
                        // For these commands, the object path is the second parameter
                        return parts[1];
                        
                    case "scene.setfield":
                        // For setfield, it's also the second parameter
                        return parts[1];
                        
                    case "scene.prefab":
                        // For prefab instantiation, if there's a parent path specified (3+ parts),
                        // return the parent path, otherwise return empty (as we don't know the created object's path yet)
                        return parts.Length > 2 ? parts[2] : string.Empty;
                        
                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error extracting object path from command: {ex.Message}");
                return string.Empty;
            }
        }
    }
    
    /// <summary>
    /// Represents a message in the chat history
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        /// <summary>
        /// Whether this message is from the user (true) or the AI (false)
        /// </summary>
        public bool IsUser { get; set; }
        
        /// <summary>
        /// The content of the message
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// The timestamp when the message was sent
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Whether this is a new message that should trigger auto-scrolling
        /// </summary>
        public bool IsNew { get; set; }
        
        /// <summary>
        /// Whether this is a system message that's only visible to the AI, not to the user in the chat UI
        /// </summary>
        public bool IsSystemMessage { get; set; }
    }
} 