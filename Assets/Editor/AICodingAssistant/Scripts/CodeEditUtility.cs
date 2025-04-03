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
        /// Apply multiple code edits to a file
        /// </summary>
        /// <param name="filePath">Path to the file to edit</param>
        /// <param name="edits">List of edits to apply</param>
        /// <returns>True if successful</returns>
        public static bool ApplyEdits(string filePath, List<CodeEdit> edits)
        {
            if (edits == null || edits.Count == 0)
            {
                Debug.LogWarning("No edits to apply");
                return false;
            }
            
            Debug.Log($"Applying {edits.Count} edits to {filePath}");
            
            try
            {
                bool isNewFile = !File.Exists(filePath);
                string originalContent = isNewFile ? "" : File.ReadAllText(filePath);
                
                // Keep track of what we're doing for better debug info
                StringBuilder editDescription = new StringBuilder();
                editDescription.AppendLine(isNewFile ? "Created new file:" : "Edited file:");
                editDescription.AppendLine(filePath);
                
                // Apply all edits to create the modified content
                string modifiedContent = originalContent;
                foreach (var edit in edits)
                {
                    editDescription.AppendLine($"- {edit.Type} edit");
                    
                    switch (edit.Type)
                    {
                        case EditType.InsertAtLine:
                            modifiedContent = InsertAtLine(modifiedContent, edit.LineNumber, edit.Content);
                            break;
                            
                        case EditType.ReplaceRegion:
                            // Not implemented yet
                            break;
                            
                        case EditType.FullFileEdit:
                            modifiedContent = edit.Content;
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
                        
                        // VERIFICATION: Verify backup was created
                        if (!File.Exists(backupPath))
                        {
                            Debug.LogError($"Failed to create backup at {backupPath}");
                        }
                        else
                        {
                            Debug.Log($"Backup created successfully at {backupPath}");
                        }
                    }
                    
                    // Ensure directory exists
                    string directory = Path.GetDirectoryName(filePath);
                    Debug.Log($"Ensuring directory exists: {directory}");
                    if (!Directory.Exists(directory))
                    {
                        Debug.Log($"Creating directory: {directory}");
                        Directory.CreateDirectory(directory);
                        
                        // VERIFICATION: Verify directory was created
                        if (!Directory.Exists(directory))
                        {
                            Debug.LogError($"Failed to create directory: {directory}");
                            return false;
                        }
                    }
                    
                    // Write modified content
                    Debug.Log($"Writing modified content to {filePath}...");
                    File.WriteAllText(filePath, modifiedContent);
                    
                    // VERIFICATION STEP 1: Verify file was written
                    if (!File.Exists(filePath))
                    {
                        Debug.LogError($"File was not created at {filePath} even though no exception was thrown");
                        return false;
                    }
                    
                    // VERIFICATION STEP 2: Read back content to verify it was written correctly
                    string writtenContent = File.ReadAllText(filePath);
                    if (writtenContent.Length != modifiedContent.Length)
                    {
                        Debug.LogWarning($"File content length mismatch. Expected: {modifiedContent.Length}, Actual: {writtenContent.Length}");
                    }
                    Debug.Log($"File written with {writtenContent.Length} characters");
                    
                    // Track the change
                    ChangeTracker.Instance.RecordChange(filePath, isNewFile ? "Create" : "Edit", editDescription.ToString());
                    
                    // SEQUENTIAL OPERATION: Import asset with forced update
                    if (filePath.StartsWith("Assets/"))
                    {
                        Debug.Log($"Importing asset at path: {filePath}");
                        
                        // First ensure Unity is aware of the file
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        
                        // Then explicitly import the asset with forced update
                        AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                        
                        // VERIFICATION STEP 3: Check if Unity recognizes the asset
                        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
                        if (asset == null)
                        {
                            Debug.LogWarning($"Asset import verification: Asset was not found in AssetDatabase at path: {filePath}");
                            
                            // Try once more with delay
                            Debug.Log("Attempting secondary import with delay...");
                            EditorApplication.delayCall += () => {
                                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                                AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                                
                                // Final verification
                                UnityEngine.Object secondaryAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
                                if (secondaryAsset != null)
                                {
                                    Debug.Log($"Secondary asset import successful: {filePath}");
                                }
                                else
                                {
                                    Debug.LogError($"Secondary asset import failed: {filePath}");
                                }
                            };
                        }
                        else
                        {
                            Debug.Log($"Asset import verification: Successfully loaded asset at {filePath}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Path doesn't start with 'Assets/': {filePath}. Using global refresh instead.");
                        AssetDatabase.Refresh();
                    }
                    
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
                        
                        // Verify directory creation
                        if (!Directory.Exists(directory))
                        {
                            Debug.LogError($"Directory creation failed - directory still doesn't exist: {directory}");
                            return false;
                        }
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
                
                // EXPLICIT VERIFICATION STEP 1: Check if file exists on disk
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File was not created at {filePath} even though no exception was thrown.");
                    return false;
                }
                
                // VERIFICATION STEP 2: Read back the file to ensure content was written correctly
                string writtenContent = File.ReadAllText(filePath);
                if (writtenContent.Length != content.Length)
                {
                    Debug.LogWarning($"File content length mismatch. Expected: {content.Length}, Actual: {writtenContent.Length}");
                }
                Debug.Log($"Successfully wrote {writtenContent.Length} characters to file");
                
                // Track the change
                string className = "Unknown";
                Match classMatch = Regex.Match(content, @"class\s+(\w+)");
                if (classMatch.Success && classMatch.Groups.Count > 1)
                {
                    className = classMatch.Groups[1].Value;
                }
                ChangeTracker.Instance.RecordChange(filePath, "Create", $"Created new script '{className}'");
                
                // SEQUENTIAL OPERATION: Import asset with forced update
                if (filePath.StartsWith("Assets/"))
                {
                    Debug.Log($"Importing asset at path: {filePath}");
                    
                    // First refresh to make Unity aware of the file
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    
                    // Then explicitly import the specific asset with forced update
                    AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                    
                    // VERIFICATION STEP 3: Check if Unity recognizes the asset
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
                    if (asset == null)
                    {
                        Debug.LogWarning($"Asset import verification: Asset was not found in AssetDatabase at path: {filePath}");
                        
                        // Try one more refresh with delay
                        Debug.Log("Attempting secondary import with delay...");
                        EditorApplication.delayCall += () => {
                            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                            
                            // Final verification
                            UnityEngine.Object secondaryAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
                            if (secondaryAsset != null)
                            {
                                Debug.Log($"Secondary asset import successful: {filePath}");
                            }
                            else
                            {
                                Debug.LogError($"Secondary asset import failed: {filePath}");
                            }
                        };
                    }
                    else
                    {
                        Debug.Log($"Asset import verification: Successfully loaded asset at {filePath}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Path doesn't start with 'Assets/': {filePath}. Using global refresh instead.");
                    AssetDatabase.Refresh();
                }
                
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