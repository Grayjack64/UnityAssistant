using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Utility for reading and modifying script files
    /// </summary>
    public static class ScriptUtility
    {
        /// <summary>
        /// Read the contents of a script file
        /// </summary>
        /// <param name="scriptPath">Path to the script file (relative to project or absolute)</param>
        /// <returns>Contents of the script file</returns>
        public static string ReadScriptContent(string scriptPath)
        {
            try
            {
                // Convert to absolute path if needed
                string fullPath = GetFullPath(scriptPath);
                
                if (File.Exists(fullPath))
                {
                    return File.ReadAllText(fullPath);
                }
                else
                {
                    Debug.LogError($"Script file not found: {scriptPath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading script: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Write content to a script file
        /// </summary>
        /// <param name="scriptPath">Path to the script file (relative to project or absolute)</param>
        /// <param name="content">Content to write to the file</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool WriteScriptContent(string scriptPath, string content)
        {
            try
            {
                // Convert to absolute path if needed
                string fullPath = GetFullPath(scriptPath);
                
                // Create directories if they don't exist
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(fullPath, content);
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error writing script: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Create a new script from a template
        /// </summary>
        /// <param name="scriptName">Name of the script (without extension)</param>
        /// <param name="content">Content of the script</param>
        /// <param name="directory">Directory to save the script (default: Assets)</param>
        /// <returns>Path to the created script, or null if failed</returns>
        public static string CreateNewScript(string scriptName, string content, string directory = "Assets")
        {
            try
            {
                // Ensure script name has proper extension
                if (!scriptName.EndsWith(".cs"))
                {
                    scriptName += ".cs";
                }
                
                // Create directories if they don't exist
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Build full path
                string fullPath = Path.Combine(directory, scriptName);
                
                // Check if file already exists
                if (File.Exists(fullPath))
                {
                    Debug.LogWarning($"Script already exists: {fullPath}");
                    return null;
                }
                
                // Write content
                File.WriteAllText(fullPath, content);
                AssetDatabase.Refresh();
                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating script: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get full path from a relative or absolute path
        /// </summary>
        /// <param name="path">Path to convert</param>
        /// <returns>Full path</returns>
        private static string GetFullPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }
            else
            {
                return Path.Combine(Application.dataPath, "..", path)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
            }
        }
        
        /// <summary>
        /// Find all script files in the project
        /// </summary>
        /// <param name="includePackages">Whether to include package files</param>
        /// <returns>List of script file paths</returns>
        public static List<string> FindAllScriptFiles(bool includePackages = false)
        {
            List<string> scriptFiles = new List<string>();
            
            try
            {
                // Get all .cs files in the Assets folder
                string[] assetGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
                
                foreach (string guid in assetGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".cs"))
                    {
                        scriptFiles.Add(path);
                    }
                }
                
                // Include package files if requested
                if (includePackages)
                {
                    string[] packageGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Packages" });
                    
                    foreach (string guid in packageGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (path.EndsWith(".cs"))
                        {
                            scriptFiles.Add(path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error finding script files: {ex.Message}");
            }
            
            return scriptFiles;
        }
        
        /// <summary>
        /// Get specific lines from a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="startLine">Start line (1-based index)</param>
        /// <param name="endLine">End line (1-based index)</param>
        /// <returns>The requested lines as a string</returns>
        public static string GetFileLines(string filePath, int startLine, int endLine)
        {
            try
            {
                string fullPath = GetFullPath(filePath);
                
                if (!File.Exists(fullPath))
                {
                    return null;
                }
                
                string[] allLines = File.ReadAllLines(fullPath);
                
                // Adjust for 1-based indexing
                startLine = Math.Max(1, startLine) - 1;
                endLine = Math.Min(allLines.Length, endLine) - 1;
                
                if (startLine > endLine || startLine >= allLines.Length)
                {
                    return null;
                }
                
                return string.Join(Environment.NewLine, 
                    allLines.Skip(startLine).Take(endLine - startLine + 1));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading file lines: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Extract namespaces, classes, methods, properties from a script
        /// </summary>
        /// <param name="scriptPath">Path to the script file</param>
        /// <returns>Dictionary of symbol types to lists of SymbolInfo objects</returns>
        public static Dictionary<string, List<SymbolInfo>> ExtractSymbols(string scriptPath)
        {
            Dictionary<string, List<SymbolInfo>> symbols = new Dictionary<string, List<SymbolInfo>>
            {
                { "namespaces", new List<SymbolInfo>() },
                { "classes", new List<SymbolInfo>() },
                { "interfaces", new List<SymbolInfo>() },
                { "methods", new List<SymbolInfo>() },
                { "properties", new List<SymbolInfo>() },
                { "fields", new List<SymbolInfo>() }
            };
            
            try
            {
                string content = ReadScriptContent(scriptPath);
                if (string.IsNullOrEmpty(content))
                {
                    return symbols;
                }
                
                string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Extract namespaces
                var namespaceMatches = Regex.Matches(content, @"namespace\s+([^\s{]+)");
                foreach (Match match in namespaceMatches)
                {
                    int lineNumber = GetLineNumber(content, match.Index);
                    symbols["namespaces"].Add(new SymbolInfo
                    {
                        Name = match.Groups[1].Value,
                        FilePath = scriptPath,
                        LineNumber = lineNumber,
                        Line = GetLineContent(lines, lineNumber)
                    });
                }
                
                // Extract classes
                var classMatches = Regex.Matches(content, 
                    @"(?:public|private|protected|internal|static)?\s+(?:class|struct)\s+([^\s:<]+)");
                foreach (Match match in classMatches)
                {
                    int lineNumber = GetLineNumber(content, match.Index);
                    symbols["classes"].Add(new SymbolInfo
                    {
                        Name = match.Groups[1].Value,
                        FilePath = scriptPath,
                        LineNumber = lineNumber,
                        Line = GetLineContent(lines, lineNumber)
                    });
                }
                
                // Extract interfaces
                var interfaceMatches = Regex.Matches(content, 
                    @"(?:public|private|protected|internal)?\s+interface\s+([^\s:<]+)");
                foreach (Match match in interfaceMatches)
                {
                    int lineNumber = GetLineNumber(content, match.Index);
                    symbols["interfaces"].Add(new SymbolInfo
                    {
                        Name = match.Groups[1].Value,
                        FilePath = scriptPath,
                        LineNumber = lineNumber,
                        Line = GetLineContent(lines, lineNumber)
                    });
                }
                
                // Extract methods
                var methodMatches = Regex.Matches(content, 
                    @"(?:public|private|protected|internal|static|virtual|override|abstract)?\s+(?:[A-Za-z0-9_<>\[\],\s]+)\s+([A-Za-z0-9_]+)\s*\([^)]*\)");
                foreach (Match match in methodMatches)
                {
                    string methodName = match.Groups[1].Value;
                    
                    // Skip property accessors
                    if (methodName == "get" || methodName == "set" || methodName == "add" || methodName == "remove")
                    {
                        continue;
                    }
                    
                    int lineNumber = GetLineNumber(content, match.Index);
                    symbols["methods"].Add(new SymbolInfo
                    {
                        Name = methodName,
                        FilePath = scriptPath,
                        LineNumber = lineNumber,
                        Line = GetLineContent(lines, lineNumber)
                    });
                }
                
                // Extract properties
                var propertyMatches = Regex.Matches(content, 
                    @"(?:public|private|protected|internal|static|virtual|override|abstract)?\s+(?:[A-Za-z0-9_<>\[\],\s]+)\s+([A-Za-z0-9_]+)\s*\{");
                foreach (Match match in propertyMatches)
                {
                    int lineNumber = GetLineNumber(content, match.Index);
                    symbols["properties"].Add(new SymbolInfo
                    {
                        Name = match.Groups[1].Value,
                        FilePath = scriptPath,
                        LineNumber = lineNumber,
                        Line = GetLineContent(lines, lineNumber)
                    });
                }
                
                // Extract fields
                var fieldMatches = Regex.Matches(content, 
                    @"(?:public|private|protected|internal|static|readonly)?\s+(?:[A-Za-z0-9_<>\[\],\s]+)\s+([A-Za-z0-9_]+)\s*=?[^;]*;");
                foreach (Match match in fieldMatches)
                {
                    string fieldName = match.Groups[1].Value;
                    
                    // Skip common keywords
                    if (fieldName == "var" || fieldName == "void" || fieldName == "string" || 
                        fieldName == "int" || fieldName == "bool" || fieldName == "float" ||
                        fieldName == "double" || fieldName == "long" || fieldName == "object")
                    {
                        continue;
                    }
                    
                    int lineNumber = GetLineNumber(content, match.Index);
                    symbols["fields"].Add(new SymbolInfo
                    {
                        Name = fieldName,
                        FilePath = scriptPath,
                        LineNumber = lineNumber,
                        Line = GetLineContent(lines, lineNumber)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error extracting symbols from {scriptPath}: {ex.Message}");
            }
            
            return symbols;
        }
        
        /// <summary>
        /// Extract using statements from a script
        /// </summary>
        /// <param name="scriptPath">Path to the script file</param>
        /// <returns>List of namespaces being imported</returns>
        public static List<string> ExtractImports(string scriptPath)
        {
            List<string> imports = new List<string>();
            
            try
            {
                string content = ReadScriptContent(scriptPath);
                if (string.IsNullOrEmpty(content))
                {
                    return imports;
                }
                
                var importMatches = Regex.Matches(content, @"using\s+([^;]+);");
                foreach (Match match in importMatches)
                {
                    imports.Add(match.Groups[1].Value.Trim());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error extracting imports from {scriptPath}: {ex.Message}");
            }
            
            return imports;
        }
        
        /// <summary>
        /// Get the line number for a character position in a string
        /// </summary>
        private static int GetLineNumber(string content, int position)
        {
            int lineNumber = 1;
            for (int i = 0; i < position; i++)
            {
                if (i < content.Length && content[i] == '\n')
                {
                    lineNumber++;
                }
            }
            return lineNumber;
        }
        
        /// <summary>
        /// Get the content of a specific line (safely)
        /// </summary>
        private static string GetLineContent(string[] lines, int lineNumber)
        {
            int index = lineNumber - 1;
            if (index >= 0 && index < lines.Length)
            {
                return lines[index];
            }
            return "";
        }
    }
    
    /// <summary>
    /// Information about a symbol (class, method, etc.) in the codebase
    /// </summary>
    public class SymbolInfo
    {
        /// <summary>
        /// Name of the symbol
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Path to the file containing the symbol
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// Line number where the symbol is defined
        /// </summary>
        public int LineNumber { get; set; }
        
        /// <summary>
        /// Content of the line where the symbol is defined
        /// </summary>
        public string Line { get; set; }
    }
} 