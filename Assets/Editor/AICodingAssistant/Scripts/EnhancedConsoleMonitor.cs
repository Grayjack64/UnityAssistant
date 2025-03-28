using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using AICodingAssistant.Editor;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Enhanced console monitor that captures and analyzes Unity console output
    /// </summary>
    public class EnhancedConsoleMonitor
    {
        // Regex pattern for extracting file and line information from stack traces
        private static readonly Regex FileLinePattern = new Regex(@"\(at (.*):(\d+)\)", RegexOptions.Compiled);
        
        private readonly List<ConsoleEntry> entries = new List<ConsoleEntry>();
        private readonly int maxLogCount;
        private bool isCapturing = false;
        
        // Track error frequencies to identify common issues
        private Dictionary<string, int> errorFrequency = new Dictionary<string, int>();
        private Dictionary<string, List<ConsoleEntry>> errorsByFile = new Dictionary<string, List<ConsoleEntry>>();
        
        // Recent errors and warnings for UI
        private List<ConsoleEntry> recentErrors = new List<ConsoleEntry>();
        private List<ConsoleEntry> recentWarnings = new List<ConsoleEntry>();
        
        // Timestamp of last clear
        private DateTime lastClearTime = DateTime.Now;
        
        /// <summary>
        /// Create a new enhanced console monitor
        /// </summary>
        /// <param name="maxLogCount">Maximum number of logs to store</param>
        public EnhancedConsoleMonitor(int maxLogCount = 500)
        {
            this.maxLogCount = maxLogCount;
            
            // Listen to Console Clear events
            EditorApplication.update += OnEditorUpdate;
        }
        
        /// <summary>
        /// Check for console clear events
        /// </summary>
        private void OnEditorUpdate()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                // Add compilation failed entry
                AddEntry(new ConsoleEntry
                {
                    Message = "Script compilation failed",
                    StackTrace = "",
                    Type = LogType.Error,
                    Timestamp = DateTime.Now,
                    IsCompilationError = true
                });
            }
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
                Debug.Log("AI Coding Assistant: Enhanced console monitoring started");
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
                Debug.Log("AI Coding Assistant: Enhanced console monitoring stopped");
            }
        }
        
        /// <summary>
        /// Clear captured logs
        /// </summary>
        public void ClearLogs()
        {
            entries.Clear();
            errorFrequency.Clear();
            errorsByFile.Clear();
            recentErrors.Clear();
            recentWarnings.Clear();
            
            lastClearTime = DateTime.Now;
            
            Debug.Log("AI Coding Assistant: Console logs cleared");
        }
        
        /// <summary>
        /// Handler for Unity log messages
        /// </summary>
        private void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            var entry = new ConsoleEntry
            {
                Message = message,
                StackTrace = stackTrace,
                Type = type,
                Timestamp = DateTime.Now
            };
            
            // Extract file and line information from stack trace if available
            ParseStackTraceForFileInfo(entry);
            
            // Add to entries collection
            AddEntry(entry);
            
            // Process error for analysis
            if (type == LogType.Error || type == LogType.Exception)
            {
                ProcessError(entry);
            }
            else if (type == LogType.Warning)
            {
                ProcessWarning(entry);
            }
        }
        
        /// <summary>
        /// Add entry to the history and maintain size limits
        /// </summary>
        private void AddEntry(ConsoleEntry entry)
        {
            entries.Add(entry);
            
            // Maintain maximum log count
            while (entries.Count > maxLogCount)
            {
                entries.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Process error entry for analysis
        /// </summary>
        private void ProcessError(ConsoleEntry entry)
        {
            // Extract a normalized error message without variable data
            string normalizedError = NormalizeErrorMessage(entry.Message);
            
            // Update frequency counter
            if (!errorFrequency.ContainsKey(normalizedError))
            {
                errorFrequency[normalizedError] = 0;
            }
            errorFrequency[normalizedError]++;
            
            // Track errors by file
            if (!string.IsNullOrEmpty(entry.FilePath))
            {
                if (!errorsByFile.ContainsKey(entry.FilePath))
                {
                    errorsByFile[entry.FilePath] = new List<ConsoleEntry>();
                }
                errorsByFile[entry.FilePath].Add(entry);
            }
            
            // Update recent errors list (keep last 10)
            recentErrors.Add(entry);
            if (recentErrors.Count > 10)
            {
                recentErrors.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Process warning entry for analysis
        /// </summary>
        private void ProcessWarning(ConsoleEntry entry)
        {
            // Update recent warnings list (keep last 10)
            recentWarnings.Add(entry);
            if (recentWarnings.Count > 10)
            {
                recentWarnings.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Extract file and line information from a stack trace
        /// </summary>
        private void ParseStackTraceForFileInfo(ConsoleEntry entry)
        {
            if (string.IsNullOrEmpty(entry.StackTrace))
            {
                return;
            }
            
            var match = FileLinePattern.Match(entry.StackTrace);
            if (match.Success && match.Groups.Count > 2)
            {
                entry.FilePath = match.Groups[1].Value.Trim();
                
                if (int.TryParse(match.Groups[2].Value, out int lineNumber))
                {
                    entry.LineNumber = lineNumber;
                }
            }
        }
        
        /// <summary>
        /// Normalize error message by removing variable data
        /// </summary>
        private string NormalizeErrorMessage(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                return string.Empty;
            }
            
            // Replace numbers with [NUM]
            string normalized = Regex.Replace(errorMessage, @"\d+", "[NUM]");
            
            // Replace quoted strings with [STR]
            normalized = Regex.Replace(normalized, @"'[^']*'", "[STR]");
            normalized = Regex.Replace(normalized, @"""[^""]*""", "[STR]");
            
            // Replace paths with [PATH]
            normalized = Regex.Replace(normalized, @"[A-Za-z]:\\[^\s:;*?""<>|]*", "[PATH]");
            normalized = Regex.Replace(normalized, @"/[^\s:;*?""<>|]*", "[PATH]");
            
            return normalized;
        }
        
        /// <summary>
        /// Get all logs as a formatted string
        /// </summary>
        /// <param name="count">Number of recent logs to retrieve (default: all)</param>
        /// <param name="includeStackTrace">Whether to include stack traces (default: false)</param>
        /// <returns>Formatted string of recent logs</returns>
        public string GetLogs(int count = -1, bool includeStackTrace = false)
        {
            if (count < 0 || count > entries.Count)
            {
                count = entries.Count;
            }
            
            var recentLogs = entries.TakeLast(count).ToList();
            var sb = new StringBuilder();
            
            foreach (var entry in recentLogs)
            {
                sb.AppendLine($"[{entry.Type}] {entry.Message}");
                
                if (includeStackTrace && !string.IsNullOrEmpty(entry.StackTrace))
                {
                    sb.AppendLine(entry.StackTrace);
                }
                else if (entry.LineNumber > 0 && !string.IsNullOrEmpty(entry.FilePath))
                {
                    sb.AppendLine($"  at {entry.FilePath}:{entry.LineNumber}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get only errors and exceptions
        /// </summary>
        /// <param name="count">Maximum number of errors to return</param>
        /// <returns>Formatted string containing recent errors</returns>
        public string GetErrors(int count = 10)
        {
            var errors = entries
                .Where(e => e.Type == LogType.Error || e.Type == LogType.Exception)
                .TakeLast(count)
                .ToList();
            
            var sb = new StringBuilder();
            foreach (var error in errors)
            {
                sb.AppendLine($"[{error.Type}] {error.Message}");
                
                if (error.LineNumber > 0 && !string.IsNullOrEmpty(error.FilePath))
                {
                    sb.AppendLine($"  at {error.FilePath}:{error.LineNumber}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get errors for a specific file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>List of errors for the specified file</returns>
        public List<ConsoleEntry> GetErrorsForFile(string filePath)
        {
            if (errorsByFile.ContainsKey(filePath))
            {
                return errorsByFile[filePath];
            }
            
            return new List<ConsoleEntry>();
        }
        
        /// <summary>
        /// Get most common errors
        /// </summary>
        /// <param name="count">Maximum number of errors to return</param>
        /// <returns>Dictionary of error messages and their frequencies</returns>
        public Dictionary<string, int> GetMostCommonErrors(int count = 5)
        {
            return errorFrequency
                .OrderByDescending(pair => pair.Value)
                .Take(count)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }
        
        /// <summary>
        /// Get a contextual analysis of console output
        /// </summary>
        /// <returns>A summary of console state for use with AI</returns>
        public string GetContextualAnalysis()
        {
            var sb = new StringBuilder();
            
            // Add timestamp information
            sb.AppendLine($"Console Analysis (Since: {lastClearTime})");
            sb.AppendLine();
            
            // Add error count information
            int errorCount = entries.Count(e => e.Type == LogType.Error || e.Type == LogType.Exception);
            int warningCount = entries.Count(e => e.Type == LogType.Warning);
            sb.AppendLine($"Summary: {errorCount} errors, {warningCount} warnings, {entries.Count} total logs");
            sb.AppendLine();
            
            // Add recent errors
            if (recentErrors.Count > 0)
            {
                sb.AppendLine("Recent Errors:");
                foreach (var error in recentErrors.TakeLast(5))
                {
                    sb.AppendLine($"- {error.Message}");
                    
                    if (error.LineNumber > 0 && !string.IsNullOrEmpty(error.FilePath))
                    {
                        sb.AppendLine($"  at {error.FilePath}:{error.LineNumber}");
                    }
                }
                sb.AppendLine();
            }
            
            // Add most common errors
            var commonErrors = GetMostCommonErrors(3);
            if (commonErrors.Count > 0)
            {
                sb.AppendLine("Most Common Errors:");
                foreach (var error in commonErrors)
                {
                    sb.AppendLine($"- [{error.Value} occurrences] {error.Key}");
                }
                sb.AppendLine();
            }
            
            // Add files with most errors
            var filesWithMostErrors = errorsByFile
                .OrderByDescending(pair => pair.Value.Count)
                .Take(3);
            
            if (filesWithMostErrors.Any())
            {
                sb.AppendLine("Files with Most Errors:");
                foreach (var file in filesWithMostErrors)
                {
                    sb.AppendLine($"- {file.Key} ({file.Value.Count} errors)");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get a list of recent log entries
        /// </summary>
        /// <param name="count">Maximum number of logs to return</param>
        /// <returns>List of recent console entries</returns>
        public List<ConsoleEntry> GetRecentLogs(int count)
        {
            if (count <= 0 || entries.Count == 0)
            {
                return new List<ConsoleEntry>();
            }
            
            return entries.TakeLast(Math.Min(count, entries.Count)).ToList();
        }
    }
    
    /// <summary>
    /// Represents a console log entry with additional metadata
    /// </summary>
    public class ConsoleEntry
    {
        /// <summary>
        /// Log message
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Stack trace
        /// </summary>
        public string StackTrace { get; set; }
        
        /// <summary>
        /// Log type (error, warning, log)
        /// </summary>
        public LogType Type { get; set; }
        
        /// <summary>
        /// Timestamp when the log was recorded
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// File path where the error occurred (if available)
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// Line number where the error occurred (if available)
        /// </summary>
        public int LineNumber { get; set; }
        
        /// <summary>
        /// Whether this is a compilation error
        /// </summary>
        public bool IsCompilationError { get; set; }
    }
} 