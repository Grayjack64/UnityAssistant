using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AICodingAssistant.AI
{
    /// <summary>
    /// Handles communication with a local Ollama server.
    /// </summary>
    public class OllamaBackend : AIBackend
    {
        private string serverUrl = "http://localhost:11434";
        private string model = "gemma";

        public void SetServerUrl(string url) => serverUrl = url;
        public void SetModel(string modelName) => model = modelName;

        public override string GetName() => "Local LLM (Ollama)";

        public override bool IsConfigured() => !string.IsNullOrEmpty(serverUrl) && !string.IsNullOrEmpty(model);

        public override async Task<AIResponse> SendRequest(string prompt)
        {
            if (!IsConfigured())
            {
                return new AIResponse { Success = false, ErrorMessage = "Ollama backend is not configured." };
            }

            // --- THE FIX ---
            // Use Newtonsoft.Json for more reliable serialization, especially with anonymous types.
            var requestBody = new
            {
                model = this.model,
                prompt = prompt,
                stream = false
            };
            string jsonBody = JsonConvert.SerializeObject(requestBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            // Clean up the server URL to prevent common errors (e.g., trailing slashes)
            string cleanedUrl = serverUrl.Trim().TrimEnd('/');
            string fullUrl = $"{cleanedUrl}/api/generate";

            using (var request = new UnityWebRequest(fullUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 300;

                try
                {
                    var asyncOp = request.SendWebRequest();

                    while (!asyncOp.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseJson = request.downloadHandler.text;
                        JObject responseObject = JObject.Parse(responseJson);
                        string message = responseObject["response"]?.ToString().Trim();
                        return new AIResponse { Success = true, Message = message };
                    }
                    else
                    {
                        string error = $"Error communicating with Ollama: {request.error}";
                        Debug.LogError(error + $" (URL: {fullUrl})"); // Log the URL for easier debugging
                        return new AIResponse { Success = false, ErrorMessage = error };
                    }
                }
                catch (Exception e)
                {
                    string error = $"Exception communicating with Ollama: {e.Message}";
                    Debug.LogError(error);
                    return new AIResponse { Success = false, ErrorMessage = error };
                }
            }
        }
    }
}
