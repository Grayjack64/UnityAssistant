using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// A simple class to represent a captured log message
    /// </summary>
    public class ConsoleLog
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public LogType Type { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Captures Unity console logs for the AI to analyze
    /// </summary>
    public class EnhancedConsoleMonitor
    {
        private readonly List<ConsoleLog> capturedLogs = new List<ConsoleLog>();
        private const int MaxLogCount = 100; // Store up to 100 recent logs

        /// <summary>
        /// Start listening for console log messages
        /// </summary>
        public void StartCapturing()
        {
            Application.logMessageReceived += HandleLog;
            Debug.Log("AI Coding Assistant: Enhanced console monitoring started");
        }

        /// <summary>
        /// Stop listening for console log messages
        /// </summary>
        public void StopCapturing()
        {
            Application.logMessageReceived -= HandleLog;
            Debug.Log("AI Coding Assistant: Enhanced console monitoring stopped");
        }

        /// <summary>
        /// Callback for when a log message is received from Unity
        /// </summary>
        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Add the new log to our list
            capturedLogs.Add(new ConsoleLog
            {
                Message = logString,
                StackTrace = stackTrace,
                Type = type,
                Timestamp = DateTime.Now
            });

            // Keep the list from growing indefinitely
            if (capturedLogs.Count > MaxLogCount)
            {
                capturedLogs.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get the most recent log messages
        /// </summary>
        /// <param name="count">How many log messages to retrieve</param>
        /// <returns>A list of recent logs</returns>
        public List<ConsoleLog> GetRecentLogs(int count)
        {
            // Return the last 'count' logs from the list
            return capturedLogs.Skip(Math.Max(0, capturedLogs.Count - count)).ToList();
        }

        /// <summary>
        /// Get the most recent error messages.
        /// </summary>
        /// <param name="count">The maximum number of errors to retrieve.</param>
        /// <returns>A list of recent logs that are errors or exceptions.</returns>
        public List<ConsoleLog> GetRecentErrors(int count = 10)
        {
            // Filter the captured logs for errors and exceptions, then take the most recent ones.
            return capturedLogs
                .Where(log => log.Type == LogType.Error || log.Type == LogType.Exception)
                .Skip(Math.Max(0, capturedLogs.Count(l => l.Type == LogType.Error || l.Type == LogType.Exception) - count))
                .ToList();
        }
    }
}
