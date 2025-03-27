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
            
            return promptBuilder.ToString();
        }
        
        private async Task ProcessAIResponse(string query, string response)
        {
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
                // Handle script generation request
                // For now, the AI will just provide the code in the response
                // User can copy and save manually or we could implement a "save this code" button
            }
            
            // Additional command processing can be added here
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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("API Key:", GUILayout.Width(80));
            claudeApiKey = EditorGUILayout.PasswordField(claudeApiKey);
            EditorGUILayout.EndHorizontal();
            
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
        
        private void LoadSettings()
        {
            grokApiKey = EditorPrefs.GetString("AICodingAssistant_GrokApiKey", "");
            claudeApiKey = EditorPrefs.GetString("AICodingAssistant_ClaudeApiKey", "");
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
        }
        
        private void SaveSettings()
        {
            EditorPrefs.SetString("AICodingAssistant_GrokApiKey", grokApiKey);
            EditorPrefs.SetString("AICodingAssistant_ClaudeApiKey", claudeApiKey);
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