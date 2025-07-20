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
            Debug.Log("Orchestrator: Building agentic prompt with tool library...");
            string prompt = BuildAgenticPrompt(userQuery, chatHistory);
            
            if (mainBackend.GetName().Contains("Gemini"))
            {
                Debug.Log("--- AGENTIC PROMPT SENT ---\n" + prompt);
            }
            
            Debug.Log($"Orchestrator: Sending plan request to Main AI ({mainBackend.GetName()})...");
            AIResponse response = await mainBackend.SendRequest(prompt);
            if (!response.Success) return $"Error: The AI failed to generate a plan. {response.ErrorMessage}";

            Debug.Log("Orchestrator: Received plan from AI. Executing now...");
            string finalConfirmation = await ExecutePlan(response.Message);
            
            AssetDatabase.Refresh();
            return finalConfirmation;
        }

        private string BuildAgenticPrompt(string userQuery, string chatHistory)
        {
            var promptBuilder = new StringBuilder();
            // This is now the single, standardized prompt for Gemini.
            promptBuilder.AppendLine("You are an expert Unity development agent. Your task is to analyze a user's request and create a step-by-step plan to fulfill it using a predefined set of tools.");
            promptBuilder.AppendLine("Your response MUST be a single, raw JSON object that strictly adheres to the provided `ActionPlan` schema. Do not include any conversational text or Markdown fences.");
            
            promptBuilder.AppendLine("\nHere is an example of the required JSON format:");
            promptBuilder.AppendLine("```json");
            promptBuilder.AppendLine("{ \"plan\": [ { \"tool\": \"ToolName\", \"arguments\": { \"arg1\": \"value1\" }, \"description\": \"Step description.\" } ] }");
            promptBuilder.AppendLine("```");

            promptBuilder.AppendLine("\n--- AVAILABLE TOOLS ---");
            promptBuilder.AppendLine(UnityEditorTools.GetToolDescriptions());

            promptBuilder.AppendLine("\n--- CHAT HISTORY ---");
            promptBuilder.AppendLine(chatHistory);

            promptBuilder.AppendLine("\n--- USER'S CURRENT REQUEST ---");
            promptBuilder.AppendLine(userQuery);

            promptBuilder.AppendLine("\n--- YOUR TASK ---");
            promptBuilder.AppendLine("Generate the JSON `ActionPlan` now.");

            return promptBuilder.ToString();
        }

        private async Task<string> ExecutePlan(string rawPlan)
        {
            Debug.Log("--- RAW AI PLAN RESPONSE ---\n" + rawPlan);

            try
            {
                string cleanJson = ExtractJsonFromMarkdown(rawPlan);
                if (string.IsNullOrEmpty(cleanJson))
                {
                    Debug.LogError($"Could not extract valid JSON from the AI's response.");
                    return "Error: The AI returned a plan, but it was not in a valid format I could read.";
                }

                ActionPlan plan = JsonConvert.DeserializeObject<ActionPlan>(cleanJson);
                if (plan?.Plan == null || plan.Plan.Count == 0)
                {
                    return "The AI returned an empty or invalid plan.";
                }

                foreach (var step in plan.Plan)
                {
                    Debug.Log($"Executing step: {step.Description}");
                    switch (step.Tool)
                    {
                        case "CreateCSharpScript":
                            UnityEditorTools.CreateCSharpScript(
                                step.Arguments["filePath"].ToString(),
                                step.Arguments["content"].ToString()
                            );
                            break;
                        case "CreateScriptableObjectAsset":
                            UnityEditorTools.CreateScriptableObjectAsset(
                                step.Arguments["scriptName"].ToString(),
                                step.Arguments["assetPath"].ToString()
                            );
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
                    return $"Execution complete, but {errors.Count} compiler error(s) were detected. Please check the console.";
                }

                return $"Execution complete. {plan.Plan.Count} steps were executed successfully.";
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse or execute AI plan: {e.Message}");
                return "Error: I received a plan from the AI, but it was malformed. I could not execute it.";
            }
        }

        private string ExtractJsonFromMarkdown(string text)
        {
            var match = Regex.Match(text, @"\{[\s\S]*\}");
            if (match.Success)
            {
                return match.Value;
            }
            return null;
        }
    }
}
