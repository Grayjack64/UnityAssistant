using UnityEngine;

namespace AICodingAssistant.Data
{
    [CreateAssetMenu(fileName = "AICompanion_Settings", menuName = "AI Coding Assistant/Settings", order = 1)]
    public class PluginSettings : ScriptableObject
    {
        [Header("AI Backend APIs")]
        public string GrokApiKey;
        public string ClaudeApiKey;
        public string GeminiApiKey;

        [Header("Ollama (Local LLM)")]
        public string OllamaUrl = "http://localhost:11434";
        public string OllamaModel = "Gemma3n";

        [Header("Debugging")]
        public bool enablePromptLogging = true; // Set to true by default for easy testing
    }
}
