using System;
using System.Threading.Tasks;
using AICodingAssistant.AI;
using AICodingAssistant.Scripts;
using UnityEditor;
using UnityEngine;

namespace AICodingAssistant.Editor
{
    public class ChatTab
    {
        private AICodingAssistantWindow window;
        private AIChatController chatController;
        
        private string userQuery = "";
        private bool isProcessing = false;
        
        // Context toggles
        private bool includeConsoleLogs = true;
        private bool includeCodeContext = true;
        private bool includeProjectSummary = true;
        private string selectedScriptPath = "";
        private UnityEngine.Object scriptObject;


        public ChatTab(AICodingAssistantWindow window, AIChatController controller)
        {
            this.window = window;
            this.chatController = controller;
        }

        public void Draw()
        {
            // Chat history
            foreach (var message in chatController.ChatHistory)
            {
                EditorGUILayout.LabelField(message.IsUser ? "You:" : "AI:", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(message.Content, EditorStyles.textArea, GUILayout.Height(40));
            }

            userQuery = EditorGUILayout.TextField("Your message:", userQuery);

            if (GUILayout.Button("Send") && !isProcessing)
            {
                isProcessing = true;
                // Note: We'll need to pass the context variables to the controller
                chatController.SendChatMessage(userQuery, includeConsoleLogs, includeCodeContext, includeProjectSummary, selectedScriptPath)
                    .ContinueWith(t => isProcessing = false, TaskScheduler.FromCurrentSynchronizationContext());
                userQuery = "";
            }

            if (isProcessing)
            {
                EditorGUILayout.LabelField("Processing...");
            }
        }
    }
}
