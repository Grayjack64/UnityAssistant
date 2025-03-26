using System;
using System.IO;
using System.Threading.Tasks;
using AICodingAssistant.AI;
using AICodingAssistant.Scripts;
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
        private string[] tabOptions = { "Chat", "Analyze Code", "Generate Code", "Settings" };
        private Vector2 scrollPosition;
        
        // AI settings
        private AIBackendType selectedBackendType = AIBackendType.LocalLLM;
        private AIBackend currentBackend;
        
        // Chat tab
        private string userQuery = "";
        private string aiResponse = "";
        private bool includeConsoleLogs = true;
        private string selectedScriptPath = "";
        private bool isProcessing = false;
        
        // Code analyze tab
        private string scriptToAnalyze = "";
        private string analysisResult = "";
        private UnityEngine.Object scriptObject;
        
        // Code generate tab
        private string codeRequirement = "";
        private string generatedCode = "";
        private string newScriptName = "NewScript";
        private string newScriptDirectory = "Assets";
        
        // Settings tab
        private string grokApiKey = "";
        private string claudeApiKey = "";
        private string ollamaUrl = "http://localhost:11434";
        private string ollamaModel = "llama2";
        
        // Utilities
        private ConsoleLogHandler consoleLogHandler;
        
        [MenuItem("Window/AI Coding Assistant")]
        public static void ShowWindow()
        {
            var window = GetWindow<AICodingAssistantWindow>("AI Coding Assistant");
            window.minSize = new Vector2(450, 550);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Initialize backend
            currentBackend = AIBackend.CreateBackend(selectedBackendType);
            
            // Initialize console log handler
            consoleLogHandler = new ConsoleLogHandler();
            consoleLogHandler.StartCapturing();
            
            // Load settings
            LoadSettings();
        }
        
        private void OnDisable()
        {
            // Stop capturing console logs
            consoleLogHandler.StopCapturing();
            
            // Save settings
            SaveSettings();
        }
        
        private void OnGUI()
        {
            // Tab selection
            currentTabIndex = GUILayout.Toolbar(currentTabIndex, tabOptions);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Display current tab
            switch (currentTabIndex)
            {
                case 0: // Chat
                    DrawChatTab();
                    break;
                case 1: // Analyze Code
                    DrawAnalyzeCodeTab();
                    break;
                case 2: // Generate Code
                    DrawGenerateCodeTab();
                    break;
                case 3: // Settings
                    DrawSettingsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        #region Chat Tab
        
        private void DrawChatTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Chat with AI Assistant", EditorStyles.boldLabel);
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
            
            // Script selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Script:", GUILayout.Width(80));
            var newScriptObject = EditorGUILayout.ObjectField(scriptObject, typeof(MonoScript), false);
            if (newScriptObject != scriptObject)
            {
                scriptObject = newScriptObject;
                if (scriptObject != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(scriptObject);
                    selectedScriptPath = assetPath;
                }
                else
                {
                    selectedScriptPath = "";
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Include console logs
            includeConsoleLogs = EditorGUILayout.Toggle("Include Console Logs", includeConsoleLogs);
            
            EditorGUILayout.Space(10);
            
            // User query input
            EditorGUILayout.LabelField("Your Query:");
            userQuery = EditorGUILayout.TextArea(userQuery, GUILayout.Height(60));
            
            EditorGUILayout.Space(5);
            
            // Send button
            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(userQuery));
            if (GUILayout.Button("Send Query to AI"))
            {
                SendChatQuery();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // AI response
            EditorGUILayout.LabelField("AI Response:");
            
            if (isProcessing)
            {
                EditorGUILayout.HelpBox("Processing your query...", MessageType.Info);
            }
            
            EditorStyles.textArea.wordWrap = true;
            aiResponse = EditorGUILayout.TextArea(aiResponse, GUILayout.Height(200));
        }
        
        private async void SendChatQuery()
        {
            if (string.IsNullOrWhiteSpace(userQuery) || isProcessing)
            {
                return;
            }
            
            isProcessing = true;
            
            try
            {
                // Build the prompt
                string prompt = $"User Query: {userQuery}\n\n";
                
                // Add script content if selected
                if (!string.IsNullOrEmpty(selectedScriptPath))
                {
                    string scriptContent = ScriptUtility.ReadScriptContent(selectedScriptPath);
                    if (!string.IsNullOrEmpty(scriptContent))
                    {
                        prompt += $"Selected Script ({selectedScriptPath}):\n```csharp\n{scriptContent}\n```\n\n";
                    }
                }
                
                // Add console logs if enabled
                if (includeConsoleLogs)
                {
                    string logs = consoleLogHandler.GetRecentLogs(10);
                    if (!string.IsNullOrEmpty(logs))
                    {
                        prompt += $"Recent Console Logs:\n```\n{logs}\n```\n\n";
                    }
                }
                
                // Send to AI backend
                prompt += "Please respond with clear, concise advice for Unity development. If providing code, ensure it's valid C# for Unity.";
                
                aiResponse = "Processing...";
                Repaint();
                
                string response = await currentBackend.SendRequest(prompt);
                aiResponse = response;
            }
            catch (Exception ex)
            {
                aiResponse = $"Error: {ex.Message}";
                Debug.LogError($"Error sending query to AI: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }
        
        #endregion
        
        #region Analyze Code Tab
        
        private void DrawAnalyzeCodeTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Analyze & Improve Code", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Script selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Script to Analyze:", GUILayout.Width(120));
            var newScriptObject = EditorGUILayout.ObjectField(scriptObject, typeof(MonoScript), false);
            if (newScriptObject != scriptObject)
            {
                scriptObject = newScriptObject;
                if (scriptObject != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(scriptObject);
                    scriptToAnalyze = assetPath;
                }
                else
                {
                    scriptToAnalyze = "";
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Analyze button
            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(scriptToAnalyze));
            if (GUILayout.Button("Analyze Script"))
            {
                AnalyzeScript();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // Analysis result
            EditorGUILayout.LabelField("Analysis Result:");
            
            if (isProcessing)
            {
                EditorGUILayout.HelpBox("Analyzing script...", MessageType.Info);
            }
            
            EditorStyles.textArea.wordWrap = true;
            analysisResult = EditorGUILayout.TextArea(analysisResult, GUILayout.Height(200));
            
            // Apply changes button (only enable if we have a result)
            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(analysisResult) || string.IsNullOrWhiteSpace(scriptToAnalyze));
            if (GUILayout.Button("Apply Suggested Changes"))
            {
                ApplySuggestedChanges();
            }
            EditorGUI.EndDisabledGroup();
        }
        
        private async void AnalyzeScript()
        {
            if (string.IsNullOrWhiteSpace(scriptToAnalyze) || isProcessing)
            {
                return;
            }
            
            isProcessing = true;
            
            try
            {
                string scriptContent = ScriptUtility.ReadScriptContent(scriptToAnalyze);
                if (string.IsNullOrEmpty(scriptContent))
                {
                    analysisResult = "Error: Could not read script content.";
                    return;
                }
                
                // Build the prompt
                string prompt = "Analyze this Unity C# script and suggest improvements:\n\n";
                prompt += $"```csharp\n{scriptContent}\n```\n\n";
                prompt += "Please provide specific suggestions to improve this code. For each suggestion, explain the issue and provide the improved code.";
                
                analysisResult = "Analyzing...";
                Repaint();
                
                string response = await currentBackend.SendRequest(prompt);
                analysisResult = response;
            }
            catch (Exception ex)
            {
                analysisResult = $"Error: {ex.Message}";
                Debug.LogError($"Error analyzing script: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }
        
        private async void ApplySuggestedChanges()
        {
            if (string.IsNullOrWhiteSpace(scriptToAnalyze) || string.IsNullOrWhiteSpace(analysisResult) || isProcessing)
            {
                return;
            }
            
            isProcessing = true;
            
            try
            {
                string scriptContent = ScriptUtility.ReadScriptContent(scriptToAnalyze);
                if (string.IsNullOrEmpty(scriptContent))
                {
                    EditorUtility.DisplayDialog("Error", "Could not read script content.", "OK");
                    return;
                }
                
                // Build the prompt
                string prompt = "Based on the original script and analysis, generate the improved version of the script.\n\n";
                prompt += $"Original Script:\n```csharp\n{scriptContent}\n```\n\n";
                prompt += $"Analysis:\n{analysisResult}\n\n";
                prompt += "Provide ONLY the complete improved script. Do not include any explanations, just the full script code.";
                
                // Ask for confirmation
                bool confirm = EditorUtility.DisplayDialog(
                    "Apply Changes",
                    "This will modify the script with AI-suggested improvements. Do you want to proceed?",
                    "Yes, Apply Changes",
                    "Cancel"
                );
                
                if (!confirm)
                {
                    return;
                }
                
                string response = await currentBackend.SendRequest(prompt);
                
                // Extract code block if present
                string improvedCode = ExtractCodeBlock(response);
                
                if (string.IsNullOrEmpty(improvedCode))
                {
                    improvedCode = response; // Use the full response if no code block found
                }
                
                // Show diff or preview
                bool applyChanges = EditorUtility.DisplayDialog(
                    "Review Changes",
                    "The AI has generated an improved version of your script. Would you like to apply these changes?",
                    "Apply Changes",
                    "Cancel"
                );
                
                if (applyChanges)
                {
                    // Backup the original file
                    string backupPath = scriptToAnalyze + ".backup";
                    ScriptUtility.WriteScriptContent(backupPath, scriptContent);
                    
                    // Write the improved code
                    ScriptUtility.WriteScriptContent(scriptToAnalyze, improvedCode);
                    
                    EditorUtility.DisplayDialog(
                        "Changes Applied",
                        $"Changes have been applied to the script. A backup has been created at {backupPath}.",
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Error applying changes: {ex.Message}", "OK");
                Debug.LogError($"Error applying changes: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }
        
        #endregion
        
        #region Generate Code Tab
        
        private void DrawGenerateCodeTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Generate New Code", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Backend selection for consistency
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AI Backend:", GUILayout.Width(80));
            var newBackendType = (AIBackendType)EditorGUILayout.EnumPopup(selectedBackendType);
            if (newBackendType != selectedBackendType)
            {
                selectedBackendType = newBackendType;
                currentBackend = AIBackend.CreateBackend(selectedBackendType);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Code requirement input
            EditorGUILayout.LabelField("Describe what the script should do:");
            codeRequirement = EditorGUILayout.TextArea(codeRequirement, GUILayout.Height(80));
            
            EditorGUILayout.Space(5);
            
            // New script details
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Script Name:", GUILayout.Width(80));
            newScriptName = EditorGUILayout.TextField(newScriptName);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Directory:", GUILayout.Width(80));
            newScriptDirectory = EditorGUILayout.TextField(newScriptDirectory);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Generate button
            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(codeRequirement));
            if (GUILayout.Button("Generate Script"))
            {
                GenerateScript();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // Generated code
            EditorGUILayout.LabelField("Generated Code:");
            
            if (isProcessing)
            {
                EditorGUILayout.HelpBox("Generating code...", MessageType.Info);
            }
            
            EditorStyles.textArea.wordWrap = true;
            generatedCode = EditorGUILayout.TextArea(generatedCode, GUILayout.Height(200));
            
            // Create script button (only enable if we have generated code)
            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(generatedCode));
            if (GUILayout.Button("Create Script"))
            {
                CreateGeneratedScript();
            }
            EditorGUI.EndDisabledGroup();
        }
        
        private async void GenerateScript()
        {
            if (string.IsNullOrWhiteSpace(codeRequirement) || isProcessing)
            {
                return;
            }
            
            isProcessing = true;
            
            try
            {
                // Build the prompt
                string prompt = $"Generate a complete C# script for Unity that does the following:\n\n{codeRequirement}\n\n";
                prompt += "Please create a fully functional Unity C# script. Include all necessary using statements, proper MonoBehaviour inheritance if needed, and complete implementation.";
                
                generatedCode = "Generating...";
                Repaint();
                
                string response = await currentBackend.SendRequest(prompt);
                
                // Extract code block if present
                string extractedCode = ExtractCodeBlock(response);
                
                if (string.IsNullOrEmpty(extractedCode))
                {
                    generatedCode = response; // Use the full response if no code block found
                }
                else
                {
                    generatedCode = extractedCode;
                }
            }
            catch (Exception ex)
            {
                generatedCode = $"Error: {ex.Message}";
                Debug.LogError($"Error generating script: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }
        
        private void CreateGeneratedScript()
        {
            if (string.IsNullOrWhiteSpace(generatedCode) || isProcessing)
            {
                return;
            }
            
            try
            {
                // Ensure script name has proper extension
                string scriptName = newScriptName;
                if (!scriptName.EndsWith(".cs"))
                {
                    scriptName += ".cs";
                }
                
                // Check if file already exists
                string fullPath = Path.Combine(newScriptDirectory, scriptName);
                
                if (File.Exists(fullPath))
                {
                    bool overwrite = EditorUtility.DisplayDialog(
                        "File Exists",
                        $"The file {scriptName} already exists. Do you want to overwrite it?",
                        "Overwrite",
                        "Cancel"
                    );
                    
                    if (!overwrite)
                    {
                        return;
                    }
                }
                
                // Create the script
                string newScriptPath = ScriptUtility.CreateNewScript(newScriptName, generatedCode, newScriptDirectory);
                
                if (newScriptPath != null)
                {
                    EditorUtility.DisplayDialog(
                        "Script Created",
                        $"Script created successfully at {newScriptPath}",
                        "OK"
                    );
                    
                    // Open the script in the editor
                    UnityEngine.Object createdScript = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newScriptPath);
                    if (createdScript != null)
                    {
                        AssetDatabase.OpenAsset(createdScript);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "Failed to create script. Check the console for details.",
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Error creating script: {ex.Message}", "OK");
                Debug.LogError($"Error creating script: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Settings Tab
        
        private void DrawSettingsTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("AI Coding Assistant Settings", EditorStyles.boldLabel);
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
} 