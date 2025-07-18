using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using AICodingAssistant.AI;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Manages the creation and modification of Game Design Document files using AI.
    /// </summary>
    public class GDDManager
    {
        private AIBackend aiBackend;
        private const string GDD_ROOT_PATH = "Assets/GameDesignDocument";
        private const string SYSTEMS_PATH = "03_Game_Systems";
        private const string KNOWLEDGE_BASE_PATH = "ai_knowledgebase.json";

        public GDDManager(AIBackend backend)
        {
            this.aiBackend = backend;
        }

        /// <summary>
        /// Generates a new GDD .md file for a system based on a user's description.
        /// </summary>
        public async Task CreateSystemGDD(string systemName, string description)
        {
            if (aiBackend == null)
            {
                Debug.LogError("GDDManager: AI Backend is not initialized.");
                return;
            }

            EditorUtility.DisplayProgressBar("AI GDD Generation", $"Asking AI to design the {systemName}...", 0.25f);

            string prompt = BuildGDDGenerationPrompt(systemName, description);
            AIResponse response = await aiBackend.SendRequest(prompt);

            if (response.Success)
            {
                EditorUtility.DisplayProgressBar("AI GDD Generation", "Saving GDD file and updating knowledge base...", 0.75f);
                
                string markdownContent = response.Message;
                // Sanitize the AI's response to ensure it's valid Markdown
                markdownContent = SanitizeMarkdown(markdownContent);

                // Save the .md file
                string filePath = SaveSystemMarkdownFile(systemName, markdownContent);
                if (string.IsNullOrEmpty(filePath)) return;

                // Update the JSON knowledge base
                UpdateKnowledgeBase(systemName, description, filePath, markdownContent);
                
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("GDD Generation Complete",
                    $"Successfully created GDD for '{systemName}'.\nFile saved at: {filePath}", "OK");
                
                // Open the newly created file for the user to review
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(filePath));
            }
            else
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"AI GDD Generation Failed: {response.ErrorMessage}");
                EditorUtility.DisplayDialog("GDD Generation Failed",
                    "The AI failed to generate the GDD. Check the console for more details.", "OK");
            }
            
            AssetDatabase.Refresh();
        }

        private string BuildGDDGenerationPrompt(string systemName, string description)
        {
            string template = GetSystemTemplate();
            return $@"You are a senior game designer. Your task is to create a detailed Game Design Document (GDD) for a new game system based on a user's request.

User's Request:
System Name: ""{systemName}""
Description: ""{description}""

Please fill out the following Markdown template with detailed, professional-quality design information based on the user's request. Be specific and thorough.

**Strictly adhere to the provided Markdown format and headings.**

Template:
---
{template}
---
";
        }

        private string SaveSystemMarkdownFile(string systemName, string content)
        {
            string systemsDirectory = Path.Combine(GDD_ROOT_PATH, SYSTEMS_PATH);
            if (!Directory.Exists(systemsDirectory))
            {
                Debug.LogError($"GDD Error: Systems directory not found at {systemsDirectory}");
                return null;
            }

            // Create a file-safe name (e.g., "Health System" -> "Health_System")
            string safeFileName = systemName.Replace(" ", "_");
            
            // Determine the next file number
            int nextFileNum = Directory.GetFiles(systemsDirectory, "*.md")
                                     .Where(f => !Path.GetFileName(f).StartsWith("_"))
                                     .Count() + 1;

            string fileName = $"03_{nextFileNum:00}_{safeFileName}.md";
            string filePath = Path.Combine(systemsDirectory, fileName);

            try
            {
                File.WriteAllText(filePath, content);
                Debug.Log($"GDD file created: {filePath}");
                return filePath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to write GDD file at {filePath}. Reason: {e.Message}");
                return null;
            }
        }
        
        private void UpdateKnowledgeBase(string systemName, string description, string gddPath, string markdownContent)
        {
            string knowledgeBasePath = Path.Combine(GDD_ROOT_PATH, KNOWLEDGE_BASE_PATH);
            if (!File.Exists(knowledgeBasePath))
            {
                Debug.LogError($"Knowledge base file not found at {knowledgeBasePath}");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(knowledgeBasePath);
                JObject knowledgeBase = JObject.Parse(jsonContent);
                JArray systems = knowledgeBase["systems"] as JArray;

                if (systems == null)
                {
                    Debug.LogError("Invalid knowledge base format: 'systems' array not found.");
                    return;
                }

                // Create a new system entry from the markdown content
                JObject newSystem = new JObject
                {
                    ["name"] = systemName,
                    ["description"] = description, // Use the user's initial description
                    ["gddPath"] = gddPath.Replace('\\', '/'), // Use forward slashes for Unity paths
                    ["apiMethods"] = new JArray(), // Can be parsed from MD in the future
                    ["events"] = new JArray(), // Can be parsed from MD in the future
                    ["dependencies"] = new JArray(),
                    ["dependents"] = new JArray()
                };

                systems.Add(newSystem);
                
                File.WriteAllText(knowledgeBasePath, knowledgeBase.ToString(Newtonsoft.Json.Formatting.Indented));
                Debug.Log($"Knowledge base updated with new system: {systemName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update knowledge base. Reason: {e.Message}");
            }
        }

        private string SanitizeMarkdown(string content)
        {
            // The AI might wrap its response in ```markdown ... ```, so we strip that out.
            var match = Regex.Match(content, @"```markdown\s*(.*?)\s*```", RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            // Also handle cases where it might just use ```
            match = Regex.Match(content, @"```\s*(.*?)\s*```", RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            return content.Trim();
        }

        private string GetSystemTemplate() =>
@"# System: [System Name]

## Purpose
*One-sentence description of what this system does.*

## Integration Points
-   **Input Dependencies**: What this system needs from other systems.
-   **Output Provided**: What this system provides to other systems.
-   **Events Triggered**: List of events this system can trigger.

## Data Schema
```json
{
  ""requiredFields"": [""field1"", ""field2""],
  ""optionalFields"": [""field3""],
  ""validationRules"": [""field1 > 0""]
}
```

## AI Implementation Guide
-   **When to use**: Trigger conditions for this system.
-   **Common patterns**: Code snippets and patterns to follow.
-   **Anti-patterns**: What to avoid when implementing.
-   **Test scenarios**: Specific test cases to validate implementation.

## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated
";
    }
}
