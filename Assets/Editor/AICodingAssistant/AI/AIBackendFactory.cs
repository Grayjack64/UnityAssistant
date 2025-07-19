using AICodingAssistant.Data; // Corrected: using .Data instead of .Scripts
using UnityEngine;

namespace AICodingAssistant.AI
{
    /// <summary>
    /// Factory for creating AIBackend instances.
    /// </summary>
    public static class AIBackendFactory
    {
        /// <summary>
        /// Creates the appropriate backend based on the specified type and settings.
        /// </summary>
        /// <param name="backendType">The type of backend to create.</param>
        /// <param name="settings">The plugin settings asset.</param>
        /// <returns>An instance of the requested backend.</returns>
        public static AIBackend CreateBackend(AIBackendType backendType, PluginSettings settings) // Corrected: No longer needs Scripts.PluginSettings
        {
            switch (backendType)
            {
                case AIBackendType.Grok:
                    var grokBackend = new GrokBackend();
                    grokBackend.SetApiKey(settings.GrokApiKey);
                    return grokBackend;

                case AIBackendType.Claude:
                    var claudeBackend = new ClaudeBackend();
                    claudeBackend.SetApiKey(settings.ClaudeApiKey);
                    claudeBackend.SetModel("claude-3-opus-20240229");
                    return claudeBackend;

                case AIBackendType.LocalLLM:
                    var ollamaBackend = new OllamaBackend();
                    ollamaBackend.SetServerUrl(settings.OllamaUrl);
                    ollamaBackend.SetModel(settings.OllamaModel);
                    return ollamaBackend;

                case AIBackendType.Gemini:
                    var geminiBackend = new GeminiBackend();
                    geminiBackend.SetApiKey(settings.GeminiApiKey);
                    geminiBackend.SetModel("gemini-1.5-flash");
                    return geminiBackend;

                default:
                    Debug.LogError($"Unknown backend type: {backendType}");
                    return null;
            }
        }
    }
}
