using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Reflection;

namespace AICodingAssistant.Editor
{
    /// <summary>
    /// Contains a library of static methods that the AI agent can call to perform actions in the Unity Editor.
    /// </summary>
    public static class UnityEditorTools
    {
        [Tool("Creates a new C# script file in the project.")]
        public static void CreateCSharpScript(string filePath, string content)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(filePath, content);
                Debug.Log($"[Tool] Created C# script at: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Tool] Failed to create C# script at {filePath}. Error: {e.Message}");
            }
        }

        [Tool("Creates a new ScriptableObject asset from an existing script.")]
        public static void CreateScriptableObjectAsset(string scriptName, string assetPath)
        {
            try
            {
                var scriptableObject = ScriptableObject.CreateInstance(scriptName);
                if (scriptableObject == null)
                {
                    Debug.LogError($"[Tool] Failed to create instance of ScriptableObject '{scriptName}'. Does the script exist?");
                    return;
                }

                string directory = Path.GetDirectoryName(assetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(scriptableObject, assetPath);
                Debug.Log($"[Tool] Created ScriptableObject asset at: {assetPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Tool] Failed to create ScriptableObject asset for '{scriptName}'. Error: {e.Message}");
            }
        }
        
        // A helper attribute to provide descriptions for our tools to the AI.
        [AttributeUsage(AttributeTargets.Method)]
        public class ToolAttribute : Attribute
        {
            public string Description { get; }
            public ToolAttribute(string description) => Description = description;
        }

        // This method generates the tool descriptions for the AI's system prompt.
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
