using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Utility for handling code edits from AI suggestions
    /// </summary>
    public static class CodeEditUtility
    {
        private static readonly Regex EditBlockRegex = new Regex(@"```edit:([^\n]+)\n(.*?)```", RegexOptions.Singleline);
        private static readonly Regex ReplaceBlockRegex = new Regex(@"```replace\n(.*?)\n```\n```with\n(.*?)\n```", RegexOptions.Singleline);
        private static readonly Regex InsertAtLineRegex = new Regex(@"```insert:(\d+)\n(.*?)\n```", RegexOptions.Singleline);
        
        /// <summary>
        /// Extracts code edits from AI response
        /// </summary>
        /// <param name="aiResponse">The response from the AI</param>
        /// <returns>List of code edits to apply</returns>
        public static List<CodeEdit> ExtractEdits(string aiResponse)
        {
            var edits = new List<CodeEdit>();
            
            // Extract edit blocks (file path + full edit content)
            foreach (Match match in EditBlockRegex.Matches(aiResponse))
            {
                if (match.Groups.Count >= 3)
                {
                    string filePath = match.Groups[1].Value.Trim();
                    string editContent = match.Groups[2].Value.Trim();
                    
                    edits.Add(new CodeEdit
                    {
                        Type = EditType.FullFileEdit,
                        FilePath = filePath,
                        Content = editContent
                    });
                }
            }
            
            // Extract replace blocks
            foreach (Match match in ReplaceBlockRegex.Matches(aiResponse))
            {
                if (match.Groups.Count >= 3)
                {
                    string oldCode = match.Groups[1].Value.Trim();
                    string newCode = match.Groups[2].Value.Trim();
                    
                    edits.Add(new CodeEdit
                    {
                        Type = EditType.ReplaceSnippet,
                        OldContent = oldCode,
                        Content = newCode
                    });
                }
            }
            
            // Extract line insertions
            foreach (Match match in InsertAtLineRegex.Matches(aiResponse))
            {
                if (match.Groups.Count >= 3)
                {
                    int lineNumber;
                    if (int.TryParse(match.Groups[1].Value, out lineNumber))
                    {
                        string insertContent = match.Groups[2].Value.Trim();
                        
                        edits.Add(new CodeEdit
                        {
                            Type = EditType.InsertAtLine,
                            LineNumber = lineNumber,
                            Content = insertContent
                        });
                    }
                }
            }
            
            return edits;
        }
        
        /// <summary>
        /// Apply code edits to the specified script
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="edits">List of edits to apply</param>
        /// <returns>True if successful</returns>
        public static bool ApplyEdits(string filePath, List<CodeEdit> edits)
        {
            try
            {
                Debug.Log($"Attempting to apply {edits.Count} edits to {filePath}");
                
                string originalContent = "";
                string modifiedContent = "";
                bool isNewFile = false;
                
                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    // File doesn't exist - check if we have a full file edit to create it
                    bool hasFullFileEdit = edits.Any(e => e.Type == EditType.FullFileEdit);
                    if (hasFullFileEdit)
                    {
                        Debug.Log($"File not found: {filePath} - Will create it with the provided content");
                        isNewFile = true;
                        originalContent = ""; // Empty for new files
                        modifiedContent = ""; // Will be set with full content edit
                    }
                    else
                    {
                        Debug.LogError($"File not found: {filePath} - Cannot apply partial edits to non-existent file");
                        return false;
                    }
                }
                else
                {
                    // Read existing file content
                    Debug.Log($"Reading existing file content from {filePath}");
                    originalContent = File.ReadAllText(filePath);
                    modifiedContent = originalContent;
                    Debug.Log($"Successfully read {originalContent.Length} characters from file");
                }
                
                // Generate description of the edits
                StringBuilder editDescription = new StringBuilder();
                editDescription.AppendLine($"Applied {edits.Count} edit(s):");
                
                // Apply edits (in correct order)
                int editCount = 0;
                foreach (var edit in edits)
                {
                    editCount++;
                    Debug.Log($"Applying edit {editCount}/{edits.Count}, type: {edit.Type}");
                    
                    switch (edit.Type)
                    {
                        case EditType.FullFileEdit:
                            if (edit.FilePath == filePath)
                            {
                                Debug.Log("Replacing entire file content");
                                modifiedContent = edit.Content;
                                editDescription.AppendLine(isNewFile ? 
                                    "- Created file with content" : 
                                    "- Replaced entire file content");
                            }
                            else
                            {
                                Debug.LogWarning($"FilePath mismatch: edit has path {edit.FilePath} but we're editing {filePath}");
                            }
                            break;
                            
                        case EditType.ReplaceSnippet:
                            Debug.Log($"Replacing snippet: '{edit.OldContent?.Substring(0, Math.Min(50, edit.OldContent?.Length ?? 0))}...'");
                            if (modifiedContent.Contains(edit.OldContent))
                            {
                                modifiedContent = modifiedContent.Replace(edit.OldContent, edit.Content);
                                string shortOldContent = edit.OldContent.Length > 50 ? edit.OldContent.Substring(0, 47) + "..." : edit.OldContent;
                                editDescription.AppendLine($"- Replaced: \"{shortOldContent}\"");
                            }
                            else
                            {
                                Debug.LogWarning($"Could not find snippet to replace. Snippet not found in file content.");
                            }
                            break;
                            
                        case EditType.InsertAtLine:
                            Debug.Log($"Inserting at line {edit.LineNumber}");
                            modifiedContent = InsertAtLine(modifiedContent, edit.LineNumber, edit.Content);
                            editDescription.AppendLine($"- Inserted code at line {edit.LineNumber}");
                            break;
                    }
                }
                
                // Only write if content actually changed or it's a new file
                Debug.Log("Checking if content changed...");
                bool contentChanged = isNewFile || modifiedContent != originalContent;
                Debug.Log($"Content changed: {contentChanged}");
                
                if (contentChanged)
                {
                    // Create backup of original file if it exists
                    if (!isNewFile)
                    {
                        string backupPath = filePath + ".bak";
                        Debug.Log($"Creating backup at {backupPath}");
                        File.WriteAllText(backupPath, originalContent);
                    }
                    
                    // Ensure directory exists
                    string directory = Path.GetDirectoryName(filePath);
                    Debug.Log($"Ensuring directory exists: {directory}");
                    if (!Directory.Exists(directory))
                    {
                        Debug.Log($"Creating directory: {directory}");
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Write modified content
                    Debug.Log($"Writing modified content to {filePath}...");
                    File.WriteAllText(filePath, modifiedContent);
                    
                    // Verify file was written
                    if (File.Exists(filePath))
                    {
                        string newContent = File.ReadAllText(filePath);
                        Debug.Log($"File written with {newContent.Length} characters");
                    }
                    else
                    {
                        Debug.LogError($"File was not created at {filePath} even though no exception was thrown");
                    }
                    
                    // Track the change
                    ChangeTracker.Instance.RecordChange(filePath, isNewFile ? "Create" : "Edit", editDescription.ToString());
                    
                    // Refresh Asset Database
                    Debug.Log("Refreshing AssetDatabase...");
                    AssetDatabase.Refresh();
                    if (isNewFile)
                    {
                        Debug.Log($"Successfully created file: {filePath}");
                    }
                    else
                    {
                        Debug.Log($"Successfully edited file: {filePath} (backup created at {filePath}.bak)");
                    }
                    return true;
                }
                else
                {
                    Debug.Log("No changes were made to the file content");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying code edits: {ex.Message}\nStack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Create a new script file with the given content
        /// </summary>
        /// <param name="filePath">Path for the new file</param>
        /// <param name="content">Content to write to the file</param>
        /// <returns>True if successful</returns>
        public static bool CreateScript(string filePath, string content)
        {
            try
            {
                Debug.Log($"Attempting to create script at path: {filePath}");
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                Debug.Log($"Directory: {directory}");
                
                if (string.IsNullOrEmpty(directory))
                {
                    Debug.LogError("Invalid directory path: empty or null");
                    return false;
                }
                
                if (!Directory.Exists(directory))
                {
                    Debug.Log($"Directory does not exist. Creating directory: {directory}");
                    try
                    {
                        Directory.CreateDirectory(directory);
                        Debug.Log($"Directory created successfully: {directory}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to create directory: {ex.Message}\nStack trace: {ex.StackTrace}");
                        return false;
                    }
                }
                
                // Check if file already exists
                if (File.Exists(filePath))
                {
                    Debug.Log($"File already exists at {filePath}. Will be overwritten.");
                }
                
                // Write file
                Debug.Log($"Writing file content to {filePath}...");
                File.WriteAllText(filePath, content);
                
                // Verify file was created
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File was not created at {filePath} even though no exception was thrown.");
                    return false;
                }
                
                // Track the change
                string className = "Unknown";
                Match classMatch = Regex.Match(content, @"class\s+(\w+)");
                if (classMatch.Success && classMatch.Groups.Count > 1)
                {
                    className = classMatch.Groups[1].Value;
                }
                ChangeTracker.Instance.RecordChange(filePath, "Create", $"Created new script '{className}'");
                
                // Refresh Asset Database
                Debug.Log("Refreshing AssetDatabase...");
                AssetDatabase.Refresh();
                Debug.Log($"Successfully created script: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating script: {ex.Message}\nStack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Insert content at specified line
        /// </summary>
        private static string InsertAtLine(string content, int lineNumber, string insertText)
        {
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            // Ensure line number is valid
            if (lineNumber < 1 || lineNumber > lines.Length + 1)
            {
                Debug.LogWarning($"Invalid line number: {lineNumber}. File has {lines.Length} lines.");
                return content;
            }
            
            StringBuilder sb = new StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                // Line numbers start at 1, indices at 0
                if (i == lineNumber - 1)
                {
                    sb.AppendLine(insertText);
                }
                
                sb.AppendLine(lines[i]);
            }
            
            // Add at end if line number is after the last line
            if (lineNumber == lines.Length + 1)
            {
                sb.AppendLine(insertText);
            }
            
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Represents a single code edit to be applied
    /// </summary>
    public class CodeEdit
    {
        /// <summary>
        /// Type of edit
        /// </summary>
        public EditType Type { get; set; }
        
        /// <summary>
        /// File path for edits (when needed)
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// Line number for line-specific edits
        /// </summary>
        public int LineNumber { get; set; }
        
        /// <summary>
        /// Original content for replacements
        /// </summary>
        public string OldContent { get; set; }
        
        /// <summary>
        /// New content to insert/replace
        /// </summary>
        public string Content { get; set; }
    }
    
    /// <summary>
    /// Types of code edits
    /// </summary>
    public enum EditType
    {
        /// <summary>
        /// Edit the entire file
        /// </summary>
        FullFileEdit,
        
        /// <summary>
        /// Replace a snippet of code
        /// </summary>
        ReplaceSnippet,
        
        /// <summary>
        /// Insert at a specific line number
        /// </summary>
        InsertAtLine
    }
} 