using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AICodingAssistant.Editor
{
    public static class UnityEditorTools
    {
        [Tool("Creates a new C# script file in the project. Use this for all new classes.")]
        public static void CreateCSharpScript(string filePath, string content)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(filePath, content);
                Debug.Log($"[Tool] Created C# script at: {filePath}");
            }
            catch (Exception e) { Debug.LogError($"[Tool] Failed to create C# script at {filePath}. Error: {e.Message}"); }
        }

        [Tool("Creates a new ScriptableObject asset from an existing script.")]
        public static void CreateScriptableObjectAsset(string scriptName, string assetPath)
        {
            try
            {
                var so = ScriptableObject.CreateInstance(scriptName);
                if (so == null)
                {
                    Debug.LogError($"[Tool] Failed to create instance of ScriptableObject '{scriptName}'.");
                    return;
                }
                string directory = Path.GetDirectoryName(assetPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                AssetDatabase.CreateAsset(so, assetPath);
                Debug.Log($"[Tool] Created ScriptableObject asset at: {assetPath}");
            }
            catch (Exception e) { Debug.LogError($"[Tool] Failed to create ScriptableObject asset for '{scriptName}'. Error: {e.Message}"); }
        }

        [Tool("Reads the entire content of an existing text file.")]
        public static string ReadFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    Debug.Log($"[Tool] Reading file: {filePath}");
                    return File.ReadAllText(filePath);
                }
                Debug.LogWarning($"[Tool] File not found for reading: {filePath}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Tool] Failed to read file at {filePath}. Error: {e.Message}");
                return null;
            }
        }
        
        // NEW, SMARTER TOOL
        [Tool("Finds or creates a Game Design Document for a specific system and appends new content to it.")]
        public static void UpdateSystemGDD(string systemName, string content)
        {
            try
            {
                string gddSystemPath = "Assets/GameDesignDocument/03_Game_Systems";
                string safeSystemName = systemName.Replace(" ", "_");

                // Try to find an existing file for this system
                var files = Directory.GetFiles(gddSystemPath, "*.md");
                string existingFile = files.FirstOrDefault(f => Path.GetFileName(f).ToLower().Contains(safeSystemName.ToLower()));

                if (existingFile != null)
                {
                    // Append content to the existing file
                    File.AppendAllText(existingFile, "\n\n" + content);
                    Debug.Log($"[Tool] Appended content to existing GDD: {existingFile}");
                }
                else
                {
                    // If no file exists, create a new one
                    int nextFileNum = files.Length + 1;
                    string newFilePath = Path.Combine(gddSystemPath, $"03_{nextFileNum:00}_{safeSystemName}.md");
                    
                    // Use a standard template for the new GDD file
                    StringBuilder fileContent = new StringBuilder();
                    fileContent.AppendLine($"# System: {systemName}");
                    fileContent.AppendLine("## Purpose");
                    fileContent.AppendLine($"*To manage the {systemName.ToLower()} of the game.*");
                    fileContent.AppendLine("\n---\n");
                    fileContent.AppendLine("## AI-Generated Details");
                    fileContent.AppendLine(content);

                    File.WriteAllText(newFilePath, fileContent.ToString());
                    Debug.Log($"[Tool] Created new GDD file: {newFilePath}");
                }
            }
            catch(Exception e)
            {
                Debug.LogError($"[Tool] Failed to update or create GDD for system '{systemName}'. Error: {e.Message}");
            }
        }

        // DEPRECATED UpdateFile TOOL (We keep it for now but the prompt will steer the AI away from it for GDDs)
        [Tool("Overwrites an existing file with new content. Use this for updating the knowledge base, but prefer 'UpdateSystemGDD' for Game Design Documents.")]
        public static void UpdateFile(string filePath, string newContent)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.WriteAllText(filePath, newContent);
                    Debug.Log($"[Tool] Updated file: {filePath}");
                }
                else
                {
                    Debug.LogWarning($"[Tool] File not found for updating: {filePath}. Creating it instead.");
                    CreateCSharpScript(filePath, newContent);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Tool] Failed to update file at {filePath}. Error: {e.Message}");
            }
        }
        
        [AttributeUsage(AttributeTargets.Method)]
        public class ToolAttribute : Attribute
        {
            public string Description { get; }
            public ToolAttribute(string description) => Description = description;
        }

        public static string GetToolDescriptions()
        {
            // ... (This method requires no changes)
            var descriptions = "";
            var methods = typeof(UnityEditorTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(ToolAttribute), false).Length > 0);

            foreach (var method in methods)
            {
                var attribute = (ToolAttribute)method.GetCustomAttribute(typeof(ToolAttribute));
                descriptions += $"Tool: {method.Name}\n";
                descriptions += $"Description: {attribute.Description}\n";
                descriptions += "Arguments:\n";
                foreach (var param in method.GetParameters())
                {
                    descriptions += $"  - {param.Name} ({param.ParameterType.Name})\n";
                }
                descriptions += "\n";
            }
            return descriptions;
        }
    }
}