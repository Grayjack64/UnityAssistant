using AICodingAssistant.Data;
using UnityEngine;

namespace AICodingAssistant.AI
{
    public static class AIBackendFactory
    {
        public static AIBackend CreateBackend(PluginSettings settings)
        {
            var gemini = new GeminiBackend();
            gemini.SetApiKey(settings.GeminiApiKey);
            // We will use the powerful 1.5 Pro model for all tasks.
            gemini.SetModel("gemini-1.5-pro-latest"); 
            return gemini;
        }
    }
}
