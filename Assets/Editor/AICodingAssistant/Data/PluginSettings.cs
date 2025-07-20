using UnityEngine;

namespace AICodingAssistant.Data
{
    [CreateAssetMenu(fileName = "AICompanion_Settings", menuName = "AI Coding Assistant/Settings", order = 1)]
    public class PluginSettings : ScriptableObject
    {
        [Header("Google Gemini API")]
        public string GeminiApiKey;

        [Header("Debugging")]
        public bool enablePromptLogging = true;
    }
}
