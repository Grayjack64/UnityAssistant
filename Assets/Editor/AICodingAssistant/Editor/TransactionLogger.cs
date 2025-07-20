using UnityEngine;
using System.IO;
using System;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace AICodingAssistant.Editor
{
    public class TransactionLogger
    {
        private StringBuilder logContent;
        private string logFilePath;

        // Add this public property to safely expose the file path
        public string LogFilePath => logFilePath;

        public void StartTransaction()
        {
            Debug.Log("[TransactionLogger] Starting new transaction.");
            logContent = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string logDirectory = "Assets/Editor/AICodingAssistant/Logs";

            if (!Directory.Exists(logDirectory))
            {
                Debug.Log($"[TransactionLogger] Creating Logs directory at: {logDirectory}");
                Directory.CreateDirectory(logDirectory);
            }

            logFilePath = Path.Combine(logDirectory, $"Transaction_{timestamp}.log");
            Debug.Log($"[TransactionLogger] Log file path set to: {logFilePath}");

            LogHeader($"Transaction started at: {DateTime.Now}");
        }
        
        // ... (The rest of the methods remain the same)

        public void LogUserPrompt(string prompt)
        {
            Debug.Log("[TransactionLogger] Logging User Prompt.");
            LogSection("User Prompt", prompt);
        }

        public void LogAgenticPrompt(string prompt)
        {
            Debug.Log("[TransactionLogger] Logging Agentic Prompt.");
            LogSection("Agentic Prompt Sent to AI", prompt);
        }

        public void LogAIResponse(string response)
        {
            Debug.Log("[TransactionLogger] Logging AI Response.");
            LogSection("Raw AI Plan Response", response);
        }

        public void LogToolExecution(string toolName, JObject arguments)
        {
            Debug.Log($"[TransactionLogger] Logging Tool Execution: {toolName}");
            logContent.AppendLine($"--- EXECUTING TOOL: {toolName} ---");
            logContent.AppendLine(arguments.ToString());
            logContent.AppendLine("------------------------");
            logContent.AppendLine();
        }

        public void LogMessage(string message)
        {
            Debug.Log($"[TransactionLogger] Logging Message: {message}");
            logContent.AppendLine($"[INFO] {message}");
        }

        public void EndTransaction()
        {
            Debug.Log("[TransactionLogger] Ending transaction and writing log file.");
            LogHeader($"Transaction ended at: {DateTime.Now}");
            try
            {
                File.WriteAllText(logFilePath, logContent.ToString());
                Debug.Log($"[TransactionLogger] Transaction log saved successfully to: {logFilePath}");
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransactionLogger] Failed to save transaction log. Error: {e.Message}");
            }
        }

        private void LogHeader(string header)
        {
            logContent.AppendLine("==================================================");
            logContent.AppendLine(header);
            logContent.AppendLine("==================================================");
            logContent.AppendLine();
        }

        private void LogSection(string title, string content)
        {
            logContent.AppendLine($"########## {title.ToUpper()} ##########");
            logContent.AppendLine(content);
            logContent.AppendLine("--------------------------------------------------");
            logContent.AppendLine();
        }
    }
}