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
        
        // UI State
        private Vector2 chatScrollPosition;
        private GUIStyle readOnlyTextStyle;
        private GUIStyle inputTextAreaStyle;


        public ChatTab(AICodingAssistantWindow window, AIChatController controller)
        {
            this.window = window;
            this.chatController = controller;
        }

        public void Draw()
        {
            if (readOnlyTextStyle == null)
            {
                readOnlyTextStyle = new GUIStyle(EditorStyles.textArea) 
                { 
                    wordWrap = true,
                    richText = true,
                    normal = { background = null }, 
                };
                
                inputTextAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true
                };
            }

            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            
            chatScrollPosition = EditorGUILayout.BeginScrollView(chatScrollPosition, GUILayout.ExpandHeight(true));
            
            foreach (var message in chatController.ChatHistory)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(message.IsUser ? "You:" : "AI:", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(message.Content, readOnlyTextStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Your message:", EditorStyles.boldLabel);
            userQuery = EditorGUILayout.TextArea(userQuery, inputTextAreaStyle, GUILayout.Height(120), GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Send", GUILayout.Height(30)) && !isProcessing)
            {
                if (!string.IsNullOrWhiteSpace(userQuery))
                {
                    isProcessing = true;
                    // Corrected method call with only one argument
                    chatController.SendChatMessage(userQuery)
                        .ContinueWith(t => {
                            isProcessing = false;
                            window.Repaint();
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    userQuery = "";
                    GUI.FocusControl(null);
                }
            }

            if (isProcessing)
            {
                EditorGUILayout.HelpBox("AI is thinking...", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
