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
        private AIOrchestrator orchestrator;
        private AICodingAssistantWindow window;

        public AIChatController(AIBackend localBackend, AIBackend mainBackend, AICodingAssistantWindow w)
        {
            this.orchestrator = new AIOrchestrator(localBackend, mainBackend);
            this.window = w;
        }

        public List<ChatMessage> ChatHistory => chatHistory;

        public async Task SendChatMessage(string userQuery)
        {
            if (string.IsNullOrWhiteSpace(userQuery)) return;

            AddMessage(userQuery, true);

            // Get chat history and previous response to send to the orchestrator
            string history = GetChatHistoryAsString();
            string previousResponse = GetLastAIResponse();

            string aiResponse = await orchestrator.ProcessUserRequest(userQuery, history, previousResponse);

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
            foreach(var msg in chatHistory)
            {
                sb.AppendLine($"{(msg.IsUser ? "User" : "AI")}: {msg.Content}");
            }
            return sb.ToString();
        }

        private string GetLastAIResponse()
        {
            for (int i = chatHistory.Count - 1; i >= 0; i--)
            {
                if (!chatHistory[i].IsUser)
                {
                    return chatHistory[i].Content;
                }
            }
            return null;
        }
    }
}
