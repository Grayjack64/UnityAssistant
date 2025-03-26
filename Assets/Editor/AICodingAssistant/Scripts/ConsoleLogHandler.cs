using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Captures Unity console logs for use with the AI Coding Assistant
    /// </summary>
    public class ConsoleLogHandler
    {
        private readonly List<LogEntry> logs = new List<LogEntry>();
        private readonly int maxLogCount;
        private bool isCapturing = false;
        
        /// <summary>
        /// Create a new console log handler
        /// </summary>
        /// <param name="maxLogCount">Maximum number of logs to store (default: 100)</param>
        public ConsoleLogHandler(int maxLogCount = 100)
        {
            this.maxLogCount = maxLogCount;
        }
        
        /// <summary>
        /// Start capturing console logs
        /// </summary>
        public void StartCapturing()
        {
            if (!isCapturing)
            {
                Application.logMessageReceived += OnLogMessageReceived;
                isCapturing = true;
                Debug.Log("AI Coding Assistant: Console log capture started");
            }
        }
        
        /// <summary>
        /// Stop capturing console logs
        /// </summary>
        public void StopCapturing()
        {
            if (isCapturing)
            {
                Application.logMessageReceived -= OnLogMessageReceived;
                isCapturing = false;
                Debug.Log("AI Coding Assistant: Console log capture stopped");
            }
        }
        
        /// <summary>
        /// Clear captured logs
        /// </summary>
        public void ClearLogs()
        {
            logs.Clear();
            Debug.Log("AI Coding Assistant: Console logs cleared");
        }
        
        /// <summary>
        /// Get recent logs as a formatted string
        /// </summary>
        /// <param name="count">Number of recent logs to retrieve (default: all)</param>
        /// <param name="includeStackTrace">Whether to include stack traces (default: false)</param>
        /// <returns>Formatted string of recent logs</returns>
        public string GetRecentLogs(int count = -1, bool includeStackTrace = false)
        {
            if (count < 0 || count > logs.Count)
            {
                count = logs.Count;
            }
            
            var recentLogs = logs.TakeLast(count).ToList();
            var sb = new StringBuilder();
            
            foreach (var log in recentLogs)
            {
                sb.AppendLine($"[{log.Type}] {log.Message}");
                if (includeStackTrace && !string.IsNullOrEmpty(log.StackTrace))
                {
                    sb.AppendLine(log.StackTrace);
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Handler for Unity log messages
        /// </summary>
        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            logs.Add(new LogEntry
            {
                Message = condition,
                StackTrace = stackTrace,
                Type = type,
                Timestamp = DateTime.Now
            });
            
            // Maintain maximum log count
            while (logs.Count > maxLogCount)
            {
                logs.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Structure to store log entries
        /// </summary>
        private class LogEntry
        {
            public string Message { get; set; }
            public string StackTrace { get; set; }
            public LogType Type { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
} 