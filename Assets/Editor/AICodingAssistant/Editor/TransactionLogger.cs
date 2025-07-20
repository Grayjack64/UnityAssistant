using UnityEngine;
using System.IO;
using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AICodingAssistant.Editor
{
    /// <summary>
    /// Logs a complete AI transaction (request, prompts, response, actions) to a file.
    /// </summary>
    public class TransactionLogger
    {
        private StringBuilder logContent;
        private string logFilePath;

        public void StartTransaction()
        {
            logContent = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string logDirectory = "Assets/Editor/AICodingAssistant/Logs";

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            logFilePath = Path.Combine(logDirectory, $"Transaction_{timestamp}.log");

            LogHeader($"Transaction started at: {DateTime.Now}");
        }

        public void LogUserPrompt(string prompt)
        {
            LogSection("User Prompt", prompt);
        }

        public void LogAgenticPrompt(string prompt)
        {
            LogSection("Agentic Prompt Sent to AI", prompt);
        }

        public void LogAIResponse(string response)
        {
            LogSection("Raw AI Plan Response", response);
        }

        public void LogToolExecution(string toolName, JObject arguments)
        {
            logContent.AppendLine($"--- EXECUTING TOOL: {toolName} ---");
            logContent.AppendLine(arguments.ToString());
            logContent.AppendLine("------------------------");
            logContent.AppendLine();
        }

        public void LogMessage(string message)
        {
            logContent.AppendLine($"[INFO] {message}");
        }

        public void EndTransaction()
        {
            LogHeader($"Transaction ended at: {DateTime.Now}");
            try
            {
                File.WriteAllText(logFilePath, logContent.ToString());
                Debug.Log($"Transaction log saved to: {logFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save transaction log. Error: {e.Message}");
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
