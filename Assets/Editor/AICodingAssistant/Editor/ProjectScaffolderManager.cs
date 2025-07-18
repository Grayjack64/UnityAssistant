using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using AICodingAssistant.AI;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace AICodingAssistant.Editor // Corrected namespace
{
    /// <summary>
    /// Handles the logic for scaffolding an entire GDD structure based on a high-level project description.
    /// </summary>
    public class ProjectScaffolderManager
    {
        private AIBackend aiBackend;
        private const string GDD_ROOT_PATH = "Assets/GameDesignDocument";
        private const string SYSTEMS_PATH = "03_Game_Systems";
        private const string KNOWLEDGE_BASE_PATH = "ai_knowledgebase.json";

        public ProjectScaffolderManager(AIBackend backend)
        {
            this.aiBackend = backend;
        }

        public async Task ScaffoldProject(string projectName, string genre, string pillars, string loop)
        {
            if (aiBackend == null)
            {
                Debug.LogError("Scaffolder: AI Backend is not initialized.");
                return;
            }

            EditorUtility.DisplayProgressBar("AI Project Scaffolding", "Asking AI to design the core systems...", 0.1f);
            
            // Step 1: Generate the GDD files from the AI
            string prompt = BuildScaffoldingPrompt(projectName, genre, pillars, loop);
            AIResponse response = await aiBackend.SendRequest(prompt);

            if (!response.Success)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"AI Scaffolding Failed: {response.ErrorMessage}");
                return;
            }

            // Step 2: Parse the AI's response into separate files
            EditorUtility.DisplayProgressBar("AI Project Scaffolding", "Parsing AI response...", 0.5f);
            var gddFiles = ParseMultiFileResponse(response.Message);
            if (gddFiles.Count == 0)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("AI Scaffolding Failed: Could not parse any GDD files from the AI response.");
                return;
            }

            // Step 3: Create the directory structure and save the files
            GDDGenerator.GenerateGDD(); // Use our existing generator for the base structure

            // Step 4: Save the AI-generated system files
            EditorUtility.DisplayProgressBar("AI Project Scaffolding", "Saving GDD files...", 0.7f);
            List<string> savedFilePaths = new List<string>();
            foreach (var file in gddFiles)
            {
                string filePath = SaveSystemMarkdownFile(file.Key, file.Value);
                if (!string.IsNullOrEmpty(filePath))
                {
                    savedFilePaths.Add(filePath);
                }
            }

            // Step 5: Update the knowledge base with all new systems
            EditorUtility.DisplayProgressBar("AI Project Scaffolding", "Updating knowledge base...", 0.9f);
            UpdateKnowledgeBase(gddFiles);

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Scaffolding Complete",
                $"Successfully generated {gddFiles.Count} core system documents for '{projectName}'.", "OK");
            
            AssetDatabase.Refresh();
        }

        private string BuildScaffoldingPrompt(string projectName, string genre, string pillars, string loop)
        {
            return $@"You are a senior game designer tasked with scaffolding a new project.
Based on the following high-level description, first, identify the essential, core game systems that will be required.
Then, for each system you identify, create a complete Game Design Document in Markdown format using the provided template.

**Project Details:**
- **Project Name:** {projectName}
- **Genre:** {genre}
- **Core Pillars:** {pillars}
- **Core Gameplay Loop:** {loop}

**Instructions:**
1.  Identify 4-6 core game systems based on the project details.
2.  For EACH system, generate a complete GDD file.
3.  Use the exact Markdown template provided for each file.
4.  Separate each file's content with the exact delimiter: '---FILE_SEPARATOR---'.

**Markdown Template:**
# System: [System Name]
## Purpose
*One-sentence description of what this system does.*
## Integration Points
- **Input Dependencies**: What this system needs from other systems.
- **Output Provided**: What this system provides to other systems.
- **Events Triggered**: List of events this system can trigger.
## Data Schema
```json
{{
  ""requiredFields"": [""field1"", ""field2""],
  ""optionalFields"": [""field3""],
  ""validationRules"": [""field1 > 0""]
}}
```
## AI Implementation Guide
- **When to use**: Trigger conditions for this system.
- **Common patterns**: Code snippets and patterns to follow.
- **Anti-patterns**: What to avoid when implementing.
- **Test scenarios**: Specific test cases to validate implementation.
## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated
";
        }

        private Dictionary<string, string> ParseMultiFileResponse(string response)
        {
            var files = new Dictionary<string, string>();
            string[] separator = new string[] { "---FILE_SEPARATOR---" };
            string[] fileContents = response.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string content in fileContents)
            {
                // Extract the system name from the first line (e.g., "# System: Health System")
                var match = Regex.Match(content.Trim(), @"# System:\s*(.*)");
                if (match.Success)
                {
                    string systemName = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(systemName))
                    {
                        files[systemName] = content.Trim();
                    }
                }
            }
            return files;
        }

        private string SaveSystemMarkdownFile(string systemName, string content)
        {
            string systemsDirectory = Path.Combine(GDD_ROOT_PATH, SYSTEMS_PATH);
            string safeFileName = systemName.Replace(" ", "_");
            int nextFileNum = Directory.GetFiles(systemsDirectory, "*.md").Count(f => !Path.GetFileName(f).StartsWith("_")) + 1;
            string fileName = $"03_{nextFileNum:00}_{safeFileName}.md";
            string filePath = Path.Combine(systemsDirectory, fileName);

            try
            {
                File.WriteAllText(filePath, content);
                return filePath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to write GDD file at {filePath}. Reason: {e.Message}");
                return null;
            }
        }

        private void UpdateKnowledgeBase(Dictionary<string, string> gddFiles)
        {
            string knowledgeBasePath = Path.Combine(GDD_ROOT_PATH, KNOWLEDGE_BASE_PATH);
            if (!File.Exists(knowledgeBasePath)) return;

            try
            {
                string jsonContent = File.ReadAllText(knowledgeBasePath);
                JObject knowledgeBase = JObject.Parse(jsonContent);
                JArray systems = knowledgeBase["systems"] as JArray;
                systems.Clear(); // Clear existing systems to replace with the new scaffold

                foreach (var file in gddFiles)
                {
                    systems.Add(new JObject { ["name"] = file.Key, ["description"] = "Generated by Project Scaffolder." });
                }
                
                File.WriteAllText(knowledgeBasePath, knowledgeBase.ToString(Newtonsoft.Json.Formatting.Indented));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update knowledge base. Reason: {e.Message}");
            }
        }
    }
}
