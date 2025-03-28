using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Tracks changes made to code files and monitors compilation results
    /// </summary>
    public class ChangeTracker
    {
        // Maximum number of changes to keep in history
        private const int MaxChangeHistory = 10;
        
        // Change records and compilation status
        private List<ChangeRecord> recentChanges = new List<ChangeRecord>();
        private CompilationStatus lastCompilationStatus = CompilationStatus.Unknown;
        private List<string> lastCompilationErrors = new List<string>();
        private DateTime lastCompilationTime = DateTime.MinValue;
        
        // Event for compilation finished
        public event Action<CompilationResult> OnCompilationCompleted;
        
        // Singleton instance
        private static ChangeTracker instance;
        public static ChangeTracker Instance 
        {
            get 
            {
                if (instance == null)
                {
                    instance = new ChangeTracker();
                }
                return instance;
            }
        }
        
        /// <summary>
        /// Initialize the change tracker and set up compilation hooks
        /// </summary>
        private ChangeTracker()
        {
            // Register for compilation events
            CompilationPipeline.assemblyCompilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }
        
        /// <summary>
        /// Register a change made to a file
        /// </summary>
        /// <param name="filePath">Path to the changed file</param>
        /// <param name="changeType">Type of change (create, edit)</param>
        /// <param name="description">Description of the change</param>
        public void RecordChange(string filePath, string changeType, string description)
        {
            // Create new change record
            var change = new ChangeRecord
            {
                FilePath = filePath,
                ChangeType = changeType,
                Description = description,
                Timestamp = DateTime.Now,
                CompilationStatus = CompilationStatus.Pending
            };
            
            // Add to recent changes list
            recentChanges.Insert(0, change);
            
            // Trim list if it's too long
            if (recentChanges.Count > MaxChangeHistory)
            {
                recentChanges.RemoveAt(recentChanges.Count - 1);
            }
            
            // Set compilation status to pending for this and all uncompiled changes
            foreach (var recentChange in recentChanges)
            {
                if (recentChange.CompilationStatus != CompilationStatus.Success && 
                    recentChange.CompilationStatus != CompilationStatus.Failed)
                {
                    recentChange.CompilationStatus = CompilationStatus.Pending;
                }
            }
            
            // Log the change
            Debug.Log($"ChangeTracker: Recorded {changeType} to {filePath}");
        }
        
        /// <summary>
        /// Called when compilation starts
        /// </summary>
        private void OnCompilationStarted(string assemblyPath)
        {
            lastCompilationStatus = CompilationStatus.Compiling;
            lastCompilationErrors.Clear();
            
            // Update all pending changes to compiling
            foreach (var change in recentChanges)
            {
                if (change.CompilationStatus == CompilationStatus.Pending)
                {
                    change.CompilationStatus = CompilationStatus.Compiling;
                }
            }
        }
        
        /// <summary>
        /// Called when compilation finishes
        /// </summary>
        private void OnCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            bool hasErrors = false;
            lastCompilationErrors.Clear();
            
            // Process compilation messages
            foreach (var message in messages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    hasErrors = true;
                    lastCompilationErrors.Add($"{message.file}({message.line}): {message.message}");
                }
            }
            
            // Update compilation status
            lastCompilationStatus = hasErrors ? CompilationStatus.Failed : CompilationStatus.Success;
            lastCompilationTime = DateTime.Now;
            
            // Update all compiling changes
            foreach (var change in recentChanges)
            {
                if (change.CompilationStatus == CompilationStatus.Compiling)
                {
                    change.CompilationStatus = lastCompilationStatus;
                    
                    // Associate errors with specific files
                    if (hasErrors)
                    {
                        foreach (var error in lastCompilationErrors)
                        {
                            if (error.Contains(Path.GetFileName(change.FilePath)))
                            {
                                change.CompilationErrors.Add(error);
                            }
                        }
                    }
                }
            }
            
            // Log compilation result
            if (hasErrors)
            {
                Debug.LogError($"ChangeTracker: Compilation failed with {lastCompilationErrors.Count} errors.");
            }
            else
            {
                Debug.Log("ChangeTracker: Compilation succeeded.");
            }
            
            // Create compilation result and notify subscribers
            var result = new CompilationResult
            {
                Success = !hasErrors,
                Errors = new List<string>(lastCompilationErrors),
                ChangesWithErrors = GetChangesWithErrors(),
                CompletionTime = lastCompilationTime
            };
            
            // Raise event if there are subscribers
            OnCompilationCompleted?.Invoke(result);
        }
        
        /// <summary>
        /// Get all recent changes that have compilation errors
        /// </summary>
        private List<ChangeRecord> GetChangesWithErrors()
        {
            List<ChangeRecord> changesWithErrors = new List<ChangeRecord>();
            foreach (var change in recentChanges)
            {
                if (change.CompilationStatus == CompilationStatus.Failed && change.CompilationErrors.Count > 0)
                {
                    changesWithErrors.Add(change);
                }
            }
            return changesWithErrors;
        }
        
        /// <summary>
        /// Get a summary of recent changes for the AI context
        /// </summary>
        /// <returns>Summary text of recent changes and their compilation status</returns>
        public string GetChangesSummary()
        {
            if (recentChanges.Count == 0)
            {
                return "No recent code changes have been tracked.";
            }
            
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("RECENT CODE CHANGES:");
            
            foreach (var change in recentChanges)
            {
                summary.AppendLine($"- {change.ChangeType} to {change.FilePath} at {change.Timestamp.ToString("HH:mm:ss")}");
                summary.AppendLine($"  Status: {GetStatusText(change.CompilationStatus)}");
                
                if (change.CompilationStatus == CompilationStatus.Failed && change.CompilationErrors.Count > 0)
                {
                    summary.AppendLine("  Errors:");
                    foreach (var error in change.CompilationErrors)
                    {
                        summary.AppendLine($"    - {error}");
                    }
                }
            }
            
            // Add overall compilation status
            summary.AppendLine();
            summary.AppendLine($"Last compilation: {GetStatusText(lastCompilationStatus)} at {lastCompilationTime.ToString("HH:mm:ss")}");
            
            if (lastCompilationStatus == CompilationStatus.Failed && lastCompilationErrors.Count > 0)
            {
                summary.AppendLine("Compilation errors:");
                foreach (var error in lastCompilationErrors.GetRange(0, Math.Min(5, lastCompilationErrors.Count)))
                {
                    summary.AppendLine($"  - {error}");
                }
                
                if (lastCompilationErrors.Count > 5)
                {
                    summary.AppendLine($"  ... and {lastCompilationErrors.Count - 5} more errors.");
                }
            }
            
            return summary.ToString();
        }
        
        /// <summary>
        /// Convert compilation status to a friendly text representation
        /// </summary>
        private string GetStatusText(CompilationStatus status)
        {
            switch (status)
            {
                case CompilationStatus.Success:
                    return "✅ Compiled successfully";
                case CompilationStatus.Failed:
                    return "❌ Compilation failed";
                case CompilationStatus.Compiling:
                    return "⏳ Currently compiling";
                case CompilationStatus.Pending:
                    return "⏳ Waiting for compilation";
                default:
                    return "Unknown status";
            }
        }
        
        /// <summary>
        /// Check if there are any compilation errors
        /// </summary>
        /// <returns>True if there are compilation errors</returns>
        public bool HasCompilationErrors()
        {
            return lastCompilationStatus == CompilationStatus.Failed && lastCompilationErrors.Count > 0;
        }
        
        /// <summary>
        /// Get recent compilation errors as structured data
        /// </summary>
        /// <returns>List of compilation error objects with file and line information</returns>
        public List<CompilationErrorInfo> GetRecentCompilationErrors()
        {
            List<CompilationErrorInfo> errors = new List<CompilationErrorInfo>();
            
            // Parse compilation errors into structured format
            foreach (string errorText in lastCompilationErrors)
            {
                var error = ParseCompilationError(errorText);
                if (error != null)
                {
                    errors.Add(error);
                }
            }
            
            return errors;
        }
        
        /// <summary>
        /// Parse a compilation error string into structured format
        /// </summary>
        private CompilationErrorInfo ParseCompilationError(string errorText)
        {
            // Parse format like: "Assets/Path/To/File.cs(42,10): error CS1234: Error message"
            var match = Regex.Match(errorText, @"(.*?)\((\d+),(\d+)\):(.*?):(.*?)$");
            if (match.Success && match.Groups.Count > 5)
            {
                return new CompilationErrorInfo
                {
                    File = match.Groups[1].Value.Trim(),
                    Line = int.Parse(match.Groups[2].Value),
                    Column = int.Parse(match.Groups[3].Value),
                    ErrorCode = match.Groups[4].Value.Trim(),
                    Message = match.Groups[5].Value.Trim()
                };
            }
            
            // Fallback for different error formats
            return new CompilationErrorInfo
            {
                Message = errorText,
                File = "Unknown",
                Line = 0,
                Column = 0,
                ErrorCode = "Unknown"
            };
        }
        
        /// <summary>
        /// Get the last compilation status
        /// </summary>
        public CompilationStatus GetLastCompilationStatus()
        {
            return lastCompilationStatus;
        }
        
        /// <summary>
        /// Get the list of recent changes
        /// </summary>
        public List<ChangeRecord> GetRecentChanges()
        {
            return recentChanges;
        }
        
        /// <summary>
        /// Get the list of recent changes, limited to the specified count
        /// </summary>
        /// <param name="count">Maximum number of changes to return</param>
        /// <returns>List of the most recent changes</returns>
        public List<ChangeRecord> GetRecentChanges(int count)
        {
            if (count <= 0 || count >= recentChanges.Count)
            {
                return recentChanges;
            }
            
            return recentChanges.Take(count).ToList();
        }
        
        /// <summary>
        /// Get compilation errors from the last compilation
        /// </summary>
        public List<string> GetLastCompilationErrors()
        {
            return lastCompilationErrors;
        }
    }
    
    /// <summary>
    /// Structured information about a compilation error
    /// </summary>
    public class CompilationErrorInfo
    {
        /// <summary>
        /// Path to the file containing the error
        /// </summary>
        public string File { get; set; }
        
        /// <summary>
        /// Line number of the error
        /// </summary>
        public int Line { get; set; }
        
        /// <summary>
        /// Column number of the error
        /// </summary>
        public int Column { get; set; }
        
        /// <summary>
        /// Error code (e.g., CS1234)
        /// </summary>
        public string ErrorCode { get; set; }
        
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }
    }
    
    /// <summary>
    /// Result of a compilation process
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// Whether compilation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// List of all compilation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Changes that have associated errors
        /// </summary>
        public List<ChangeRecord> ChangesWithErrors { get; set; } = new List<ChangeRecord>();
        
        /// <summary>
        /// When compilation completed
        /// </summary>
        public DateTime CompletionTime { get; set; }
    }
    
    /// <summary>
    /// Records a single change to a file
    /// </summary>
    public class ChangeRecord
    {
        /// <summary>
        /// Path to the file that was changed
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// Type of change ("Edit", "Create", etc.)
        /// </summary>
        public string ChangeType { get; set; }
        
        /// <summary>
        /// Description of the change
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// When the change was made
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Current compilation status of this change
        /// </summary>
        public CompilationStatus CompilationStatus { get; set; } = CompilationStatus.Unknown;
        
        /// <summary>
        /// Compilation errors associated with this change, if any
        /// </summary>
        public List<string> CompilationErrors { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Possible compilation statuses for a change
    /// </summary>
    public enum CompilationStatus
    {
        Unknown,
        Pending,
        Compiling,
        Success,
        Failed
    }
} 