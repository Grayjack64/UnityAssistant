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
        private bool enableEnhancedConsole = true;
        private bool enableCodeIndexing = true;
        private int maxConsoleEntries = 500;
        
        // Utilities
        private EnhancedConsoleMonitor consoleMonitor;
        private CodebaseContext codebaseContext;
        private bool codebaseInitialized = false;
        private float codebaseInitProgress = 0f;
        private UnityEngine.Object scriptObject;
        
        [MenuItem("Window/AI Coding Assistant")]
        public static void ShowWindow()
        {
            var window = GetWindow<AICodingAssistantWindow>("AI Coding Assistant");
            window.minSize = new Vector2(450, 550);
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
                          "• Searching your codebase for relevant information\n\n" +
                          "Just describe what you need in natural language, and I'll help you out!",
                Timestamp = DateTime.Now
            });
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
        
        private void OnDisable()
        {
            // Stop capturing console logs
            consoleMonitor.StopCapturing();
            
            // Unsubscribe from compilation events
            ChangeTracker.Instance.OnCompilationCompleted -= HandleCompilationCompleted;
            
            // Save settings
            SaveSettings();
        }
        
        private void OnGUI()
        {
            // Tab selection
            currentTabIndex = GUILayout.Toolbar(currentTabIndex, tabOptions);
            
            // Show codebase initialization message if needed
            if (!codebaseInitialized && enableCodeIndexing)
            {
                EditorGUILayout.HelpBox("Codebase indexing is in progress...", MessageType.Info);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), codebaseInitProgress, "Indexing Codebase");
                EditorGUILayout.Space(5);
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Display current tab
            switch (currentTabIndex)
            {
                case 0: // Unified Chat
                    DrawUnifiedChatTab();
                    break;
                case 1: // Settings
                    DrawSettingsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        #region Unified Chat Interface
        
        private void DrawUnifiedChatTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("AI Coding Assistant", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Backend selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AI Backend:", GUILayout.Width(80));
            var newBackendType = (AIBackendType)EditorGUILayout.EnumPopup(selectedBackendType);
            if (newBackendType != selectedBackendType)
            {
                selectedBackendType = newBackendType;
                currentBackend = AIBackend.CreateBackend(selectedBackendType);
                
                // If switching to Claude, refresh model list if API key is configured
                if (selectedBackendType == AIBackendType.Claude && !string.IsNullOrEmpty(claudeApiKey))
                {
                    // Refresh models with a slight delay to ensure the backend is initialized
                    EditorApplication.delayCall += RefreshClaudeModels;
                }
            }
            
            // Show current model if using Claude
            if (selectedBackendType == AIBackendType.Claude)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Model:", GUILayout.Width(100));
                
                if (isLoadingClaudeModels)
                {
                    EditorGUILayout.LabelField("Loading models...");
                }
                else
                {
                    string modelDisplayName = "Unknown";
                    foreach (var option in claudeModelOptions)
                    {
                        // Find display name that corresponds to current model ID
                        if (claudeModelOptions.Length > selectedClaudeModelIndex && 
                            selectedClaudeModelIndex >= 0)
                        {
                            modelDisplayName = claudeModelOptions[selectedClaudeModelIndex];
                        }
                    }
                    
                    EditorGUILayout.LabelField(modelDisplayName);
                    
                    // Add a button to manage models
                    if (GUILayout.Button("Change Model", GUILayout.Width(100)))
                    {
                        currentTabIndex = 1; // Switch to settings tab
                        GUI.FocusControl(null); // Clear focus to ensure UI updates
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Context options
            EditorGUILayout.BeginHorizontal();
            
            // Script selection
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            EditorGUILayout.LabelField("Selected Script:", GUILayout.Width(100));
            var newScriptObject = EditorGUILayout.ObjectField(scriptObject, typeof(MonoScript), false, GUILayout.Width(240));
            if (newScriptObject != scriptObject)
            {
                scriptObject = newScriptObject;
                if (scriptObject != null)
                {
                    selectedScriptPath = AssetDatabase.GetAssetPath(scriptObject);
                }
                else
                {
                    selectedScriptPath = "";
                }
            }
            EditorGUILayout.EndVertical();
            
            // Context toggles
            EditorGUILayout.BeginVertical();
            includeConsoleLogs = EditorGUILayout.ToggleLeft("Include Console Logs", includeConsoleLogs);
            includeCodeContext = EditorGUILayout.ToggleLeft("Include Code Context", includeCodeContext);
            includeProjectSummary = EditorGUILayout.ToggleLeft("Include Project Summary", includeProjectSummary);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Chat history
            EditorGUILayout.LabelField("Conversation:", EditorStyles.boldLabel);
            
            // Display chat history
            float messageHeight = 0;
            foreach (var message in chatHistory)
            {
                // Calculate approximate height based on content
                float contentHeight = EditorStyles.textArea.CalcHeight(new GUIContent(message.Content), EditorGUIUtility.currentViewWidth - 40);
                
                // Display the message
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Message header (User/AI + timestamp)
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(message.IsUser ? "You:" : "AI Assistant:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(message.Timestamp.ToString("HH:mm:ss"), GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();
                
                // Message content
                GUIStyle messageStyle = new GUIStyle(EditorStyles.textArea);
                messageStyle.wordWrap = true;
                messageStyle.richText = true;
                
                // Format code blocks in AI responses
                string formattedContent = message.IsUser ? message.Content : FormatCodeBlocks(message.Content);
                
                EditorGUILayout.TextArea(formattedContent, messageStyle, GUILayout.Height(contentHeight));
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
                
                messageHeight += contentHeight + 60; // Add height for padding, headers, etc.
            }
            
            // Ensure we scroll to the bottom when new messages arrive
            if (Event.current.type == EventType.Layout && chatHistory.Count > 0 && chatHistory[chatHistory.Count - 1].IsNew)
            {
                chatHistory[chatHistory.Count - 1].IsNew = false;
                scrollPosition.y = messageHeight;
                Repaint();
            }
            
            // Processing message
            if (isProcessing)
            {
                EditorGUILayout.HelpBox("AI is thinking...", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // User input
            EditorGUILayout.LabelField("Your Message:");
            userQuery = EditorGUILayout.TextArea(userQuery, GUILayout.Height(60));
            
            EditorGUILayout.Space(5);
            
            // Send button
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(userQuery));
            if (GUILayout.Button("Send", GUILayout.Height(30)))
            {
                SendChatMessage();
            }
            EditorGUI.EndDisabledGroup();
            
            // Clear chat button
            if (GUILayout.Button("Clear Chat", GUILayout.Width(100), GUILayout.Height(30)))
            {
                chatHistory.Clear();
                chatHistory.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = "Chat history cleared. How can I help you?",
                    Timestamp = DateTime.Now,
                    IsNew = true
                });
                Repaint();
            }
            
            // Refresh context button
            if (GUILayout.Button("Refresh Context", GUILayout.Width(120), GUILayout.Height(30)))
            {
                GenerateProjectSummary();
                EditorUtility.DisplayDialog("Context Refreshed", "Project summary and context has been refreshed.", "OK");
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Handle Enter key to send message
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && Event.current.modifiers == EventModifiers.Control)
            {
                if (!string.IsNullOrWhiteSpace(userQuery) && !isProcessing)
                {
                    SendChatMessage();
                    Event.current.Use();
                }
            }
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
            StringBuilder promptBuilder = new StringBuilder();
            
            // System prompt defining capabilities
            promptBuilder.AppendLine("You are an AI assistant integrated into Unity Editor. You can help with analyzing code, generating scripts, searching the codebase, and providing advice on Unity development.");
            promptBuilder.AppendLine("For code, use format ```csharp ... ``` to highlight syntax.");
            promptBuilder.AppendLine();
            
            // Add recent code changes and compilation status
            string changesSummary = ChangeTracker.Instance.GetChangesSummary();
            if (!string.IsNullOrEmpty(changesSummary) && changesSummary != "No recent code changes have been tracked.")
            {
                promptBuilder.AppendLine("CODE CHANGE HISTORY AND COMPILATION STATUS:");
                promptBuilder.AppendLine(changesSummary);
                promptBuilder.AppendLine();
            }
            
            // Add project summary if enabled
            if (includeProjectSummary && !string.IsNullOrEmpty(projectSummary))
            {
                promptBuilder.AppendLine("PROJECT SUMMARY:");
                promptBuilder.AppendLine(projectSummary);
                promptBuilder.AppendLine();
            }
            
            // Add selected script content if available
            if (includeCodeContext && !string.IsNullOrEmpty(selectedScriptPath))
            {
                string scriptContent = ScriptUtility.ReadScriptContent(selectedScriptPath);
                if (!string.IsNullOrEmpty(scriptContent))
                {
                    promptBuilder.AppendLine($"SELECTED SCRIPT ({selectedScriptPath}):");
                    promptBuilder.AppendLine("```csharp");
                    promptBuilder.AppendLine(scriptContent);
                    promptBuilder.AppendLine("```");
                    promptBuilder.AppendLine();
                    
                    // Add file-specific errors
                    var fileErrors = consoleMonitor.GetErrorsForFile(selectedScriptPath);
                    if (fileErrors.Count > 0)
                    {
                        promptBuilder.AppendLine("ERRORS FOR THIS FILE:");
                        foreach (var error in fileErrors)
                        {
                            promptBuilder.AppendLine($"- Line {error.LineNumber}: {error.Message}");
                        }
                        promptBuilder.AppendLine();
                    }
                }
            }
            
            // Add console logs if enabled
            if (includeConsoleLogs)
            {
                string consoleAnalysis = consoleMonitor.GetContextualAnalysis();
                if (!string.IsNullOrEmpty(consoleAnalysis))
                {
                    promptBuilder.AppendLine("CONSOLE ANALYSIS:");
                    promptBuilder.AppendLine(consoleAnalysis);
                    promptBuilder.AppendLine();
                }
            }
            
            // Add conversation history for context
            int historyToInclude = Math.Min(chatHistory.Count, maxHistoryMessages * 2);
            if (historyToInclude > 0)
            {
                promptBuilder.AppendLine("CONVERSATION HISTORY:");
                for (int i = chatHistory.Count - historyToInclude; i < chatHistory.Count; i++)
                {
                    var message = chatHistory[i];
                    promptBuilder.AppendLine(message.IsUser ? $"User: {message.Content}" : $"Assistant: {message.Content}");
                }
                promptBuilder.AppendLine();
            }
            
            // Add the current user message
            promptBuilder.AppendLine($"User: {userMessage}");
            promptBuilder.AppendLine();
            
            // Add system instructions for response
            promptBuilder.AppendLine("When asked to perform actions like code analysis, script generation, or codebase search, provide the information requested.");
            promptBuilder.AppendLine("If code changes are requested, clearly indicate what files to modify and how.");
            promptBuilder.AppendLine("When generating scripts, provide complete and working code with all necessary using statements and proper Unity conventions.");
            
            // Add instructions for reviewing changes and compilation status
            promptBuilder.AppendLine("\nIMPORTANT: Pay close attention to CODE CHANGE HISTORY AND COMPILATION STATUS section above.");
            promptBuilder.AppendLine("When there are compilation errors after your changes:");
            promptBuilder.AppendLine("1. Refer back to your recent changes that may have caused the errors");
            promptBuilder.AppendLine("2. Analyze the specific error messages and their connection to your changes");
            promptBuilder.AppendLine("3. Provide fixes that directly address those errors, not unrelated features");
            promptBuilder.AppendLine("4. Explain how your proposed fixes relate to the errors and your previous changes");
            
            // Add instructions for code editing
            promptBuilder.AppendLine("\nFOR CODE EDITS: You can suggest edits using these formats:");
            promptBuilder.AppendLine("1. For full file edits: ```edit:Assets/Path/To/File.cs\n[COMPLETE FILE CONTENT]\n```");
            promptBuilder.AppendLine("2. For replacing code: ```replace\n[OLD CODE]\n```\n```with\n[NEW CODE]\n```");
            promptBuilder.AppendLine("3. For line insertions: ```insert:42\n[CODE TO INSERT AT LINE 42]\n```");
            promptBuilder.AppendLine("The user will be prompted to review and apply your suggestions.");
            
            return promptBuilder.ToString();
        }
        
        private async Task ProcessAIResponse(string query, string response)
        {
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
            
            // Additional command processing can be added here
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
                
                foreach (var fileEntry in editsByFile)
                {
                    confirmMessage.AppendLine($"File: {fileEntry.Key}");
                    confirmMessage.AppendLine($"- {fileEntry.Value.Count} edit(s)");
                }
                
                confirmMessage.AppendLine("\nWould you like to apply these edits? (Backups will be created)");
                
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
                                Content = $"✅ Successfully applied edits to {fileEntry.Key}",
                                Timestamp = DateTime.Now,
                                IsNew = true
                            });
                        }
                        else
                        {
                            chatHistory.Add(new ChatMessage
                            {
                                IsUser = false,
                                Content = $"❌ Failed to apply edits to {fileEntry.Key}",
                                Timestamp = DateTime.Now,
                                IsNew = true
                            });
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
        
        #endregion
        
        #region Settings Tab
        
        private void DrawSettingsTab()
        {
            // Settings code (same as before)
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("AI Coding Assistant Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // Conversation settings
            EditorGUILayout.LabelField("Conversation Settings", EditorStyles.boldLabel);
            maxHistoryMessages = EditorGUILayout.IntSlider("Max History Exchanges", maxHistoryMessages, 2, 20);
            
            EditorGUILayout.Space(10);
            
            // Grok settings
            EditorGUILayout.LabelField("Grok (xAI) Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("API Key:", GUILayout.Width(80));
            grokApiKey = EditorGUILayout.PasswordField(grokApiKey);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Claude settings
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
            
            EditorGUILayout.Space(10);
            
            // Ollama settings
            EditorGUILayout.LabelField("Local LLM (Ollama) Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("URL:", GUILayout.Width(80));
            ollamaUrl = EditorGUILayout.TextField(ollamaUrl);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Model:", GUILayout.Width(80));
            ollamaModel = EditorGUILayout.TextField(ollamaModel);
            EditorGUILayout.EndHorizontal();
            
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
        }
        
        private void SaveSettings()
        {
            EditorPrefs.SetString("AICodingAssistant_GrokApiKey", grokApiKey);
            EditorPrefs.SetString("AICodingAssistant_ClaudeApiKey", claudeApiKey);
            EditorPrefs.SetString("AICodingAssistant_ClaudeModel", claudeModel);
            EditorPrefs.SetString("AICodingAssistant_OllamaUrl", ollamaUrl);
            EditorPrefs.SetString("AICodingAssistant_OllamaModel", ollamaModel);
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
        
        #endregion
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
    }
} 