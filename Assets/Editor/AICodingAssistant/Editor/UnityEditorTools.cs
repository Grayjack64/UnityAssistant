using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Reflection;

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

        [Tool("Overwrites an existing file with new content. Use this to update GDDs or the knowledge base.")]
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
                    CreateCSharpScript(filePath, newContent); // Fallback to create if it doesn't exist
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
