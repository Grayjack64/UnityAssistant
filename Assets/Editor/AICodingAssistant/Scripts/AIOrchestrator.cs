using AICodingAssistant.AI;
using System.Threading.Tasks;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Manages the two-step conversation between a local LLM and a main AI.
    /// </summary>
    public class AIOrchestrator
    {
        private AIBackend localBackend;
        private AIBackend mainBackend;

        public AIOrchestrator(AIBackend local, AIBackend main)
        {
            this.localBackend = local;
            this.mainBackend = main;
        }

        public async Task<string> ProcessUserRequest(string userQuery, string chatHistory, string previousAIResponse = null)
        {
            // --- Step 1: Local LLM prepares the brief for the Main AI ---
            Debug.Log("Orchestrator: Asking Local LLM to prepare the prompt...");
            string promptForMainAI = await PreparePromptForMainAI(userQuery, chatHistory, previousAIResponse);

            if (string.IsNullOrEmpty(promptForMainAI))
            {
                return "Error: The local AI failed to prepare a plan.";
            }

            // --- Step 2: Main AI executes the plan ---
            Debug.Log("Orchestrator: Sending engineered prompt to Main AI...");
            AIResponse mainAIResponse = await mainBackend.SendRequest(promptForMainAI);

            if (!mainAIResponse.Success)
            {
                return $"Error: The main AI failed to execute the plan. {mainAIResponse.ErrorMessage}";
            }

            // --- Step 3: Local LLM processes the results and we execute them ---
            Debug.Log("Orchestrator: Asking Local LLM to process the results...");
            string finalConfirmation = await ProcessAndExecuteMainAIResponse(mainAIResponse.Message);
            
            AssetDatabase.Refresh(); // Refresh the asset database to show new files
            return finalConfirmation;
        }

        private async Task<string> PreparePromptForMainAI(string userQuery, string chatHistory, string previousAIResponse)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("You are a Project Manager AI. Your job is to take a user's request and a previous AI's data model, and create a clear, actionable prompt for an expert C# Unity developer AI.");
            promptBuilder.AppendLine("Analyze the user's request and the provided JSON data. Formulate a prompt that instructs the developer AI to create the necessary C# scripts (like ScriptableObjects and enums) based on the data model.");

            promptBuilder.AppendLine("\n--- CHAT HISTORY ---");
            promptBuilder.AppendLine(chatHistory);
            
            promptBuilder.AppendLine("\n--- PREVIOUS AI RESPONSE (JSON DATA MODEL) ---");
            promptBuilder.AppendLine(previousAIResponse);

            promptBuilder.AppendLine("\n--- USER'S CURRENT REQUEST ---");
            promptBuilder.AppendLine(userQuery);

            promptBuilder.AppendLine("\n--- YOUR TASK ---");
            promptBuilder.AppendLine("Generate the prompt for the expert developer AI now.");

            AIResponse response = await localBackend.SendRequest(promptBuilder.ToString());
            return response.Success ? response.Message : null;
        }

        private async Task<string> ProcessAndExecuteMainAIResponse(string mainAIResponse)
        {
            var promptBuilder = new StringBuilder();
            // --- THE FIX: Simplified Prompt for Local LLM ---
            promptBuilder.AppendLine("You are an Integration AI. Your job is to take the output from an expert developer AI and extract all C# code blocks into a structured format.");
            promptBuilder.AppendLine("For each C# code block you find, first determine a valid, full file path (e.g., `Assets/Scripts/Modules/ShipModule.cs`).");
            promptBuilder.AppendLine("Then, output the file path and the code content separated by a unique delimiter.");
            promptBuilder.AppendLine("The format MUST be exactly as follows for each file:");
            promptBuilder.AppendLine("---START_FILE: [Full File Path]---");
            promptBuilder.AppendLine("[C# Code Content]");
            promptBuilder.AppendLine("---END_FILE---");

            promptBuilder.AppendLine("\n--- EXPERT AI RESPONSE ---");
            promptBuilder.AppendLine(mainAIResponse);

            promptBuilder.AppendLine("\n--- YOUR TASK ---");
            promptBuilder.AppendLine("Generate the structured file output now.");

            AIResponse response = await localBackend.SendRequest(promptBuilder.ToString());

            if (response.Success)
            {
                Debug.Log("--- FILE CONTENT RECEIVED ---\n" + response.Message);
                try
                {
                    // --- EXECUTION STEP with Simplified Parsing ---
                    var fileEntries = ParseSimpleFormat(response.Message);
                    if (fileEntries.Count == 0)
                    {
                        return "The AI returned a response, but I couldn't find any files to create.";
                    }

                    int filesCreated = 0;
                    foreach (var file in fileEntries)
                    {
                        string directory = Path.GetDirectoryName(file.Key);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        await File.WriteAllTextAsync(file.Key, file.Value);
                        filesCreated++;
                        Debug.Log($"File created: {file.Key}");
                    }
                    return $"Execution complete. I have created {filesCreated} new script(s) for the Ship Module System.";
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse or execute file operations: {e.Message}");
                    return "Error: I received a response from the AI, but I failed to parse or execute it. Check the console for details.";
                }
            }
            return "Error: The local AI failed to parse the main AI's response.";
        }
        
        // New parsing method for the simpler format
        private Dictionary<string, string> ParseSimpleFormat(string response)
        {
            var files = new Dictionary<string, string>();
            const string startDelimiter = "---START_FILE:";
            const string endDelimiter = "---END_FILE---";

            var entries = response.Split(new[] { startDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                var parts = entry.Split(new[] { "---" }, 2, StringSplitOptions.None);
                if (parts.Length < 2) continue;

                string path = parts[0].Trim().TrimEnd(']'); // Clean up path
                string content = parts[1].Split(new[] { endDelimiter }, StringSplitOptions.None)[0].Trim();
                
                if (!string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(content))
                {
                    files[path] = content;
                }
            }
            return files;
        }
    }
}
