using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AICodingAssistant.AI
{
    /// <summary>
    /// Abstract base class for all AI backends (Grok, Claude, Local LLM)
    /// </summary>
    public abstract class AIBackend
    {
        /// <summary>
        /// Send a request to the AI backend and get a response
        /// </summary>
        /// <param name="prompt">The prompt to send to the AI</param>
        /// <returns>The AI's response</returns>
        public abstract Task<AIResponse> SendRequest(string prompt);

        /// <summary>
        /// Check if this backend is properly configured (e.g., has API keys)
        /// </summary>
        /// <returns>True if the backend is ready to use</returns>
        public abstract bool IsConfigured();

        /// <summary>
        /// Get the name of this backend
        /// </summary>
        /// <returns>Backend name (e.g., "Grok", "Claude", "Local LLM")</returns>
        public abstract string GetName();

        /// <summary>
        /// Factory method to create the appropriate backend
        /// </summary>
        /// <param name="backendType">The type of backend to create</param>
        /// <returns>An instance of the requested backend</returns>
        public static AIBackend CreateBackend(AIBackendType backendType)
        {
            switch (backendType)
            {
                case AIBackendType.Grok:
                    return new GrokBackend();
                case AIBackendType.Claude:
                    return new ClaudeBackend();
                case AIBackendType.LocalLLM:
                    return new OllamaBackend();
                case AIBackendType.Gemini:
                    return new GeminiBackend();
                default:
                    Debug.LogError($"Unknown backend type: {backendType}");
                    return null;
            }
        }
        
        /// <summary>
        /// Simple wrapper for backwards compatibility
        /// </summary>
        /// <param name="prompt">The prompt to send</param>
        /// <returns>The response text or error message</returns>
        public async Task<string> SendRequestLegacy(string prompt)
        {
            var response = await SendRequest(prompt);
            return response.Success ? response.Message : $"Error: {response.ErrorMessage}";
        }
    }

    /// <summary>
    /// Enum representing the available AI backend types
    /// </summary>
    public enum AIBackendType
    {
        Grok,
        Claude,
        LocalLLM,
        Gemini
    }
} 