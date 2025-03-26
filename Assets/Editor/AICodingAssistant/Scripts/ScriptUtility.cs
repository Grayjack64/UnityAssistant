using System;
using System.IO;
using System.Threading.Tasks;
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
    }
} 