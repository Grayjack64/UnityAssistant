using System;
using System.Collections.Generic;
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
        private AIOrchestrator orchestrator;
        private AICodingAssistantWindow window;

        // Corrected constructor with 3 arguments
        public AIChatController(AIBackend mainBackend, EnhancedConsoleMonitor monitor, AICodingAssistantWindow w)
        {
            this.orchestrator = new AIOrchestrator(mainBackend, monitor);
            this.window = w;
        }

        public List<ChatMessage> ChatHistory => chatHistory;

        public async Task SendChatMessage(string userQuery)
        {
            if (string.IsNullOrWhiteSpace(userQuery)) return;

            AddMessage(userQuery, true);

            string history = GetChatHistoryAsString();
            
            // Corrected method call with 2 arguments
            string aiResponse = await orchestrator.ProcessUserRequest(userQuery, history);

            AddMessage(aiResponse, false);
        }
        
        private void AddMessage(string content, bool isUser)
        {
            chatHistory.Add(new ChatMessage
            {
                IsUser = isUser,
                Content = content,
                Timestamp = DateTime.Now,
                IsNew = true
            });
            window.Repaint();
        }

        private string GetChatHistoryAsString()
        {
            var sb = new StringBuilder();
            // Send a limited amount of history to keep prompts concise
            int historyLimit = 10;
            var relevantHistory = chatHistory.Count > historyLimit 
                ? chatHistory.GetRange(chatHistory.Count - historyLimit, historyLimit) 
                : chatHistory;

            foreach(var msg in relevantHistory)
            {
                sb.AppendLine($"{(msg.IsUser ? "User" : "AI")}: {msg.Content}");
            }
            return sb.ToString();
        }
    }
}
