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
                    originalContent = File.ReadAllText(filePath);
                    modifiedContent = originalContent;
                }
                
                // Generate description of the edits
                StringBuilder editDescription = new StringBuilder();
                editDescription.AppendLine($"Applied {edits.Count} edit(s):");
                
                // Apply edits (in correct order)
                foreach (var edit in edits)
                {
                    switch (edit.Type)
                    {
                        case EditType.FullFileEdit:
                            if (edit.FilePath == filePath)
                            {
                                modifiedContent = edit.Content;
                                editDescription.AppendLine(isNewFile ? 
                                    "- Created file with content" : 
                                    "- Replaced entire file content");
                            }
                            break;
                            
                        case EditType.ReplaceSnippet:
                            modifiedContent = modifiedContent.Replace(edit.OldContent, edit.Content);
                            string shortOldContent = edit.OldContent.Length > 50 ? edit.OldContent.Substring(0, 47) + "..." : edit.OldContent;
                            editDescription.AppendLine($"- Replaced: \"{shortOldContent}\"");
                            break;
                            
                        case EditType.InsertAtLine:
                            modifiedContent = InsertAtLine(modifiedContent, edit.LineNumber, edit.Content);
                            editDescription.AppendLine($"- Inserted code at line {edit.LineNumber}");
                            break;
                    }
                }
                
                // Only write if content actually changed or it's a new file
                if (isNewFile || modifiedContent != originalContent)
                {
                    // Create backup of original file if it exists
                    if (!isNewFile)
                    {
                        string backupPath = filePath + ".bak";
                        File.WriteAllText(backupPath, originalContent);
                    }
                    
                    // Ensure directory exists
                    string directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Write modified content
                    File.WriteAllText(filePath, modifiedContent);
                    
                    // Track the change
                    ChangeTracker.Instance.RecordChange(filePath, isNewFile ? "Create" : "Edit", editDescription.ToString());
                    
                    // Refresh Asset Database
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
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying code edits: {ex.Message}");
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
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write file
                File.WriteAllText(filePath, content);
                
                // Track the change
                string className = "Unknown";
                Match classMatch = Regex.Match(content, @"class\s+(\w+)");
                if (classMatch.Success && classMatch.Groups.Count > 1)
                {
                    className = classMatch.Groups[1].Value;
                }
                ChangeTracker.Instance.RecordChange(filePath, "Create", $"Created new script '{className}'");
                
                // Refresh Asset Database
                AssetDatabase.Refresh();
                Debug.Log($"Successfully created script: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating script: {ex.Message}");
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