using AICodingAssistant.AI;
using AICodingAssistant.Scripts;
using System.Threading.Tasks;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using UnityEditor;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace AICodingAssistant.Editor 
{
    public class AIOrchestrator
    {
        private AIBackend mainBackend;
        private EnhancedConsoleMonitor consoleMonitor;

        public AIOrchestrator(AIBackend main, EnhancedConsoleMonitor monitor)
        {
            this.mainBackend = main;
            this.consoleMonitor = monitor;
        }

        public async Task<string> ProcessUserRequest(string userQuery, string chatHistory)
        {
            // --- Step 1: Generate the initial code ---
            Debug.Log("Orchestrator: Building initial code generation prompt...");
            string initialPrompt = BuildAgenticPrompt(userQuery, chatHistory);
            
            Debug.Log($"Orchestrator: Sending code generation plan request to Main AI ({mainBackend.GetName()})...");
            AIResponse initialResponse = await mainBackend.SendRequest(initialPrompt);
            if (!initialResponse.Success) return $"Error: The AI failed to generate an initial plan. {initialResponse.ErrorMessage}";

            Debug.Log("Orchestrator: Received code plan. Executing now...");
            var createdFiles = await ExecutePlan(initialResponse.Message);
            if (createdFiles == null || createdFiles.Count == 0)
            {
                return "Execution failed. The AI's initial plan was empty or could not be executed.";
            }

            // --- Step 2: The "Awareness Cycle" - Update Documentation ---
            Debug.Log("Orchestrator: Code created successfully. Starting documentation sync cycle...");
            string docPrompt = BuildDocumentationSyncPrompt(userQuery, createdFiles);

            Debug.Log($"Orchestrator: Sending documentation plan request to Main AI ({mainBackend.GetName()})...");
            AIResponse docResponse = await mainBackend.SendRequest(docPrompt);
            if (!docResponse.Success) return "Code was created, but the AI failed to generate a documentation plan.";

            Debug.Log("Orchestrator: Received documentation plan. Executing now...");
            await ExecutePlan(docResponse.Message);
            
            AssetDatabase.Refresh();
            return $"Success! I have created {createdFiles.Count} script(s) and updated the relevant GDD and knowledge base.";
        }

         private string BuildAgenticPrompt(string userQuery, string chatHistory)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("You are an expert Unity development agent. Your task is to analyze a user's request and create a step-by-step plan to fulfill it using a predefined set of tools.");
            
            // --- THE FIX: Stricter instructions and an explicit example ---
            promptBuilder.AppendLine("Your response MUST be a single, raw JSON object that strictly adheres to the provided `ActionPlan` schema. Do not include any conversational text or Markdown fences. Your entire response must be only the JSON object.");
            promptBuilder.AppendLine("Here is an example of the required JSON format, using the keys 'plan', 'tool', 'arguments', and 'description':");
            promptBuilder.AppendLine("```json");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("  \"plan\": [");
            promptBuilder.AppendLine("    {");
            promptBuilder.AppendLine("      \"tool\": \"CreateCSharpScript\",");
            promptBuilder.AppendLine("      \"arguments\": {");
            promptBuilder.AppendLine("        \"filePath\": \"Assets/Scripts/MyScript.cs\",");
            promptBuilder.AppendLine("        \"content\": \"public class MyScript { }\"");
            promptBuilder.AppendLine("      },");
            promptBuilder.AppendLine("      \"description\": \"Create the main script.\",");
            promptBuilder.AppendLine("    }");
            promptBuilder.AppendLine("  ]");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("```");

            promptBuilder.AppendLine("\n--- AVAILABLE TOOLS ---");
            promptBuilder.AppendLine(UnityEditorTools.GetToolDescriptions());

            promptBuilder.AppendLine("\n--- CHAT HISTORY ---");
            promptBuilder.AppendLine(chatHistory);

            promptBuilder.AppendLine("\n--- USER'S CURRENT REQUEST ---");
            promptBuilder.AppendLine(userQuery);

            promptBuilder.AppendLine("\n--- YOUR TASK ---");
            promptBuilder.AppendLine("Generate the JSON `ActionPlan` now, strictly following the format provided in the example.");

            return promptBuilder.ToString();
        }

        private string BuildDocumentationSyncPrompt(string originalQuery, Dictionary<string, string> createdFiles)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("You are an expert technical writer AI. Your task is to update the project's documentation based on newly created C# scripts.");
            promptBuilder.AppendLine("You must generate a JSON `ActionPlan` to perform the necessary updates using the `ReadFile` and `UpdateFile` tools.");

            promptBuilder.AppendLine("\n--- AVAILABLE TOOLS ---");
            promptBuilder.AppendLine("ReadFile(string filePath)");
            promptBuilder.AppendLine("UpdateFile(string filePath, string newContent)");

            promptBuilder.AppendLine("\n--- ORIGINAL USER REQUEST ---");
            promptBuilder.AppendLine(originalQuery);

            promptBuilder.AppendLine("\n--- NEWLY CREATED SCRIPTS ---");
            foreach (var file in createdFiles)
            {
                promptBuilder.AppendLine($"--- FILE: {file.Key} ---");
                promptBuilder.AppendLine("```csharp");
                promptBuilder.AppendLine(file.Value);
                promptBuilder.AppendLine("```");
            }

            promptBuilder.AppendLine("\n--- YOUR TASK ---");
            promptBuilder.AppendLine("Generate a JSON `ActionPlan` to update the `ai_knowledgebase.json` and any relevant `.md` GDD files to reflect the new code. First, read the existing files to get context, then update them with the new information, such as new classes, public methods, and dependencies.");

            return promptBuilder.ToString();
        }

        private async Task<Dictionary<string, string>> ExecutePlan(string rawPlan)
        {
            Debug.Log("--- RAW AI PLAN RESPONSE ---\n" + rawPlan);
            var affectedFiles = new Dictionary<string, string>();

            try
            {
                string cleanJson = ExtractJsonFromMarkdown(rawPlan);
                if (string.IsNullOrEmpty(cleanJson)) return null;

                ActionPlan plan = JsonConvert.DeserializeObject<ActionPlan>(cleanJson);
                if (plan?.Plan == null || plan.Plan.Count == 0) return null;

                foreach (var step in plan.Plan)
                {
                    Debug.Log($"Executing step: {step.Description}");
                    switch (step.Tool)
                    {
                        case "CreateCSharpScript":
                            string filePath = step.Arguments["filePath"].ToString();
                            string content = step.Arguments["content"].ToString();
                            UnityEditorTools.CreateCSharpScript(filePath, content);
                            affectedFiles[filePath] = content;
                            break;
                        case "UpdateFile":
                            UnityEditorTools.UpdateFile(
                                step.Arguments["filePath"].ToString(),
                                step.Arguments["newContent"].ToString()
                            );
                            break;
                        case "ReadFile": 
                            // This tool is primarily for the AI's context, so we don't need to execute it here.
                            // In a more advanced system, we could store the result for later steps in the plan.
                            break; 
                        default:
                            Debug.LogWarning($"Unknown tool requested by AI: {step.Tool}");
                            break;
                    }
                }
                
                Debug.Log("Plan execution finished. Waiting for Unity to compile...");
                AssetDatabase.Refresh();
                await Task.Delay(3000);

                var errors = consoleMonitor.GetRecentErrors();
                if (errors.Count > 0)
                {
                    Debug.LogError($"Execution resulted in {errors.Count} compiler error(s).");
                }

                return affectedFiles;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse or execute AI plan: {e.Message}");
                return null;
            }
        }

        private string ExtractJsonFromMarkdown(string text)
        {
            var match = Regex.Match(text, @"\{[\s\S]*\}");
            return match.Success ? match.Value : null;
        }
    }
}
