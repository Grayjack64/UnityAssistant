using UnityEngine;

namespace AICodingAssistant.Scripts
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
        public string OllamaModel = "llama2";
    }
}