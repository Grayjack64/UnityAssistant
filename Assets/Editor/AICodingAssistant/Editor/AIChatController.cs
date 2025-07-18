using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AICodingAssistant.AI;
using AICodingAssistant.Scripts;
using UnityEditor;
using UnityEngine;

namespace AICodingAssistant.Editor
{
    public class AIChatController
    {
        private List<ChatMessage> chatHistory = new List<ChatMessage>();
        private AIBackend currentBackend;
        private EnhancedConsoleMonitor consoleMonitor;
        private CodebaseContext codebaseContext;
        private AICodingAssistantWindow window;

        public AIChatController(AIBackend backend, EnhancedConsoleMonitor monitor, CodebaseContext context, AICodingAssistantWindow w)
        {
            currentBackend = backend;
            consoleMonitor = monitor;
            codebaseContext = context;
            window = w;
        }

        public List<ChatMessage> ChatHistory => chatHistory;

        public async Task SendChatMessage(string userQuery, bool includeConsoleLogs, bool includeCodeContext, bool includeProjectSummary, string selectedScriptPath)
        {
            if (string.IsNullOrWhiteSpace(userQuery))
                return;

            string prompt = BuildAIPrompt(userQuery, includeConsoleLogs, includeCodeContext, includeProjectSummary, selectedScriptPath);

            chatHistory.Add(new ChatMessage
            {
                IsUser = true,
                Content = userQuery,
                Timestamp = DateTime.Now
            });

            var aiResponse = await currentBackend.SendRequest(prompt);

            chatHistory.Add(new ChatMessage
            {
                IsUser = false,
                Content = aiResponse.Success ? aiResponse.Message : $"Error: {aiResponse.ErrorMessage}",
                Timestamp = DateTime.Now
            });

            ProcessAIResponse(aiResponse.Message);
        }

        public void AddSystemMessage(string content, bool isFromUIAction = false)
        {
            chatHistory.Add(new ChatMessage
            {
                IsUser = false,
                Content = content,
                Timestamp = DateTime.Now,
                IsNew = true,
                IsSystemMessage = true
            });
            window.Repaint();
        }

        private string BuildAIPrompt(string userMessage, bool includeConsoleLogs, bool includeCodeContext, bool includeProjectSummary, string selectedScriptPath)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are an AI assistant for Unity development.");

            if (includeProjectSummary)
            {
                // This will be implemented later
            }

            if (includeConsoleLogs && consoleMonitor != null)
            {
                var recentLogs = consoleMonitor.GetRecentLogs(10);
                if (recentLogs.Count > 0)
                {
                    prompt.AppendLine("Recent Console Logs:");
                    foreach (var log in recentLogs)
                    {
                        prompt.AppendLine($"[{log.Type}] {log.Message}");
                    }
                }
            }

            if (includeCodeContext && !string.IsNullOrEmpty(selectedScriptPath))
            {
                prompt.AppendLine($"Selected Script ({selectedScriptPath}):");
                prompt.AppendLine("```csharp");
                prompt.AppendLine(File.ReadAllText(selectedScriptPath));
                prompt.AppendLine("```");
            }

            prompt.AppendLine("Chat History:");
            foreach (var message in chatHistory)
            {
                prompt.AppendLine(message.IsUser ? $"User: {message.Content}" : $"AI: {message.Content}");
            }

            prompt.AppendLine($"User: {userMessage}");
            prompt.AppendLine("AI:");

            return prompt.ToString();
        }

        private void ProcessAIResponse(string response)
        {
            // Placeholder for command processing
        }
    }
}