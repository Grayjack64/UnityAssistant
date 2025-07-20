using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AICodingAssistant.AI
{
    public abstract class AIBackend
    {
        public abstract Task<AIResponse> SendRequest(string prompt);
        public abstract bool IsConfigured();
        public abstract string GetName();

        public async Task<string> SendRequestLegacy(string prompt)
        {
            var response = await SendRequest(prompt);
            return response.Success ? response.Message : $"Error: {response.ErrorMessage}";
        }
    }

    // The old, conflicting enum definitions that were previously here have been
    // permanently removed to resolve the namespace conflict. The correct versions
    // now live in BackendTypes.cs in the Data assembly.
}
