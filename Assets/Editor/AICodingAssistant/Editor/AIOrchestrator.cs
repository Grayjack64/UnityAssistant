// ... (keep all the using statements)
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
using Newtonsoft.Json.Linq;

namespace AICodingAssistant.Editor
{
    public class AIOrchestrator
    {
        // ... (keep the class variables)
        private AIBackend mainBackend;
        private EnhancedConsoleMonitor consoleMonitor;
        private TransactionLogger transactionLogger;
        private static readonly string tempPlanPath = "Temp/ai_assistant_plan.json";

        public AIOrchestrator(AIBackend main, EnhancedConsoleMonitor monitor)
        {
            this.mainBackend = main;
            this.consoleMonitor = monitor;
            this.transactionLogger = new TransactionLogger();
        }

        // ... ProcessUserRequest and ExecutePreCompilePlan are unchanged ...
        public async Task<string> ProcessUserRequest(string userQuery, string chatHistory)
        {
            Debug.Log("[AIOrchestrator] Starting ProcessUserRequest.");
            transactionLogger.StartTransaction();
            transactionLogger.LogUserPrompt(userQuery);

            try
            {
                string initialPrompt = BuildAgenticPrompt(userQuery, chatHistory);
                transactionLogger.LogAgenticPrompt(initialPrompt);

                AIResponse initialResponse = await mainBackend.SendRequest(initialPrompt);
                if (!initialResponse.Success)
                {
                    string errorMsg = $"Error: The AI failed to generate an initial plan. {initialResponse.ErrorMessage}";
                    transactionLogger.LogMessage(errorMsg);
                    return errorMsg;
                }

                transactionLogger.LogAIResponse(initialResponse.Message);
                
                await ExecutePreCompilePlan(initialResponse.Message, userQuery);

                return "AI has created the necessary scripts. Waiting for Unity to compile before continuing...";
            }
            catch (Exception ex)
            {
                string exceptionMessage = $"[AIOrchestrator] An unexpected error occurred: {ex.Message}\n{ex.StackTrace}";
                Debug.LogError(exceptionMessage);
                transactionLogger.LogMessage(exceptionMessage);
                return "An unexpected error occurred. Please check the Unity console for more details.";
            }
            finally
            {
                Debug.Log("[AIOrchestrator] Reached the finally block for the pre-compile phase. Ending transaction.");
                transactionLogger.EndTransaction();
            }
        }
        public async Task ExecutePreCompilePlan(string rawPlan, string userQuery)
        {
            string cleanJson = ExtractJsonFromMarkdown(rawPlan);
            if (string.IsNullOrEmpty(cleanJson)) return;

            JObject planObject = JObject.Parse(cleanJson);
            planObject["originalUserQuery"] = userQuery;

            File.WriteAllText(tempPlanPath, planObject.ToString());
            Debug.Log($"[AIOrchestrator] AI plan saved to temp file: {tempPlanPath}");

            ActionPlan plan = planObject.ToObject<ActionPlan>();
            var scriptCreationSteps = plan.Plan.Where(step => step.Tool == "CreateCSharpScript").ToList();

            if (scriptCreationSteps.Any())
            {
                 foreach (var step in scriptCreationSteps)
                {
                    Debug.Log($"[AIOrchestrator] Executing pre-compile tool: {step.Tool}");
                    transactionLogger.LogToolExecution(step.Tool, step.Arguments);
                    
                    string filePath = step.Arguments["filePath"].ToString();
                    string content = step.Arguments["content"].ToString();
                    UnityEditorTools.CreateCSharpScript(filePath, content);
                }
            }
            else
            {
                await ExecutePostCompilePlan(planObject.ToString());
            }
        }

        // UPDATE THIS METHOD
        private async Task ExecutePlan(string rawPlan)
        {
            string cleanJson = ExtractJsonFromMarkdown(rawPlan);
            if (string.IsNullOrEmpty(cleanJson)) return;

            ActionPlan plan = JsonConvert.DeserializeObject<ActionPlan>(cleanJson);
            if (plan?.Plan == null) return;
            
            foreach (var step in plan.Plan)
            {
                transactionLogger.LogToolExecution(step.Tool, step.Arguments);
                switch (step.Tool)
                {
                    case "UpdateSystemGDD": // Add the new tool here
                        UnityEditorTools.UpdateSystemGDD(
                            step.Arguments["systemName"].ToString(),
                            step.Arguments["content"].ToString()
                        );
                        break;
                    case "UpdateFile":
                        UnityEditorTools.UpdateFile(
                            step.Arguments["filePath"].ToString(),
                            step.Arguments["newContent"].ToString()
                        );
                        break;
                    case "ReadFile":
                        break;
                    default:
                        Debug.LogWarning($"[AIOrchestrator] Unknown tool in documentation plan: {step.Tool}");
                        break;
                }
            }
        }

        // UPDATE THIS METHOD
        private string BuildDocumentationSyncPrompt(string originalQuery, Dictionary<string, string> createdFiles)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("You are an expert technical writer AI. Your task is to document the newly created C# scripts.");
            promptBuilder.AppendLine("Your response MUST be a single, raw JSON object that strictly adheres to the provided `ActionPlan` schema.");
            
            promptBuilder.AppendLine("\n--- AVAILABLE TOOLS ---");
            // We only give it the tools we want it to use for this task
            promptBuilder.AppendLine("UpdateSystemGDD(string systemName, string content)");
            promptBuilder.AppendLine("UpdateFile(string filePath, string newContent)");
            promptBuilder.AppendLine("ReadFile(string filePath)");

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
            promptBuilder.AppendLine("Generate a JSON `ActionPlan` to document the new scripts. Use the 'UpdateSystemGDD' tool to add documentation to the GDD for the relevant system (e.g., 'Ship Module System'). Use the 'UpdateFile' tool to update the 'ai_knowledgebase.json' file with a summary of the new classes and their properties.");

            return promptBuilder.ToString();
        }

        // ... (The rest of the file, including ExecutePostCompilePlan, BuildAgenticPrompt, and ExtractJsonFromMarkdown, remains the same)
        public async Task<string> ExecutePostCompilePlan(string planJson)
        {
            Debug.Log("[AIOrchestrator] Starting post-compile execution.");
            transactionLogger.StartTransaction();
            transactionLogger.LogMessage("--- POST-COMPILE EXECUTION PHASE ---");

            try
            {
                JObject planObject = JObject.Parse(planJson);
                ActionPlan plan = planObject.ToObject<ActionPlan>();
                string userQuery = planObject["originalUserQuery"].ToString();

                var postCompileSteps = plan.Plan.Where(step => step.Tool != "CreateCSharpScript").ToList();
                var createdFiles = new Dictionary<string, string>();
                
                foreach (var step in postCompileSteps)
                {
                    Debug.Log($"[AIOrchestrator] Executing post-compile tool: {step.Tool}");
                    transactionLogger.LogToolExecution(step.Tool, step.Arguments);
                    switch (step.Tool)
                    {
                        case "CreateScriptableObjectAsset":
                            UnityEditorTools.CreateScriptableObjectAsset(
                                step.Arguments["scriptName"].ToString(),
                                step.Arguments["assetPath"].ToString()
                            );
                            break;
                        default:
                             Debug.LogWarning($"[AIOrchestrator] Unknown post-compile tool requested by AI: {step.Tool}");
                             break;
                    }
                }

                transactionLogger.LogMessage("Asset creation complete. Starting documentation sync cycle...");
                
                var scriptCreationSteps = plan.Plan.Where(s => s.Tool == "CreateCSharpScript").ToList();
                foreach(var step in scriptCreationSteps)
                {
                    createdFiles[step.Arguments["filePath"].ToString()] = step.Arguments["content"].ToString();
                }

                string docPrompt = BuildDocumentationSyncPrompt(userQuery, createdFiles);
                transactionLogger.LogAgenticPrompt(docPrompt);
                AIResponse docResponse = await mainBackend.SendRequest(docPrompt);

                if (docResponse.Success)
                {
                    transactionLogger.LogAIResponse(docResponse.Message);
                    await ExecutePlan(docResponse.Message);
                    return "Post-compile tasks, including documentation, have been completed successfully!";
                }
                else
                {
                    return "Post-compile tasks completed, but documentation update failed.";
                }
            }
            catch (Exception e)
            {
                string errorMsg = $"[AIOrchestrator] Failed during post-compile execution: {e.Message}";
                Debug.LogError(errorMsg);
                transactionLogger.LogMessage(errorMsg);
                return "An error occurred during post-compile execution. Check the logs for details.";
            }
            finally
            {
                Debug.Log("[AIOrchestrator] Reached finally block for post-compile phase. Ending transaction.");
                transactionLogger.EndTransaction();
            }
        }
        private string BuildAgenticPrompt(string userQuery, string chatHistory)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("You are an expert Unity development agent. Your task is to analyze a user's request and create a step-by-step plan to fulfill it using a predefined set of tools.");

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
        private string ExtractJsonFromMarkdown(string text)
        {
            var match = Regex.Match(text, @"\{[\s\S]*\}");
            return match.Success ? match.Value : null;
        }
    }
}