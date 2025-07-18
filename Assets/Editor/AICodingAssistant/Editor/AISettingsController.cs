using UnityEditor;
using UnityEngine;
using AICodingAssistant.Scripts; // Adjusted to match the new structure


namespace AICodingAssistant.Editor
{
    public class AISettingsController
    {
        private PluginSettings settings; // Removed the "Scripts." prefix

        public PluginSettings Settings => settings; // Removed the "Scripts." prefix

        public void LoadSettings()
        {
            // Find the settings asset in the project
            var guids = AssetDatabase.FindAssets("t:PluginSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<PluginSettings>(path); // Removed the "Scripts." prefix
            }
            else
            {
                // If no settings asset exists, create one
                settings = ScriptableObject.CreateInstance<PluginSettings>(); // Removed the "Scripts." prefix
                AssetDatabase.CreateAsset(settings, "Assets/Editor/AICodingAssistant/Data/AICompanion_Settings.asset");
                AssetDatabase.SaveAssets();
                Debug.LogWarning("AI Coding Assistant: No settings file found. A new one has been created in the Data folder.");
            }
        }

        public void SaveSettings()
        {
            if (settings != null)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
