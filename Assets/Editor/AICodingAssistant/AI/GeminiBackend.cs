using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AICodingAssistant.AI
{
    public class GeminiBackend : AIBackend
    {
        private string apiKey;
        private string model = "gemini-1.5-pro-latest"; // Default to the powerful model

        public void SetApiKey(string key) => apiKey = key;
        public void SetModel(string modelName) => model = modelName;

        public override string GetName() => $"Gemini ({model})";
        public override bool IsConfigured() => !string.IsNullOrEmpty(apiKey);

        public override async Task<AIResponse> SendRequest(string prompt)
        {
            if (!IsConfigured())
            {
                return new AIResponse { Success = false, ErrorMessage = "Gemini API key is not set." };
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };
            string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            // The model is now part of the URL, making it dynamic
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 300;

                var asyncOp = request.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseJson = request.downloadHandler.text;
                    JObject responseObject = JObject.Parse(responseJson);
                    string message = responseObject["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString().Trim();
                    return new AIResponse { Success = true, Message = message };
                }
                else
                {
                    string error = $"Error communicating with Gemini: {request.error} - {request.downloadHandler.text}";
                    Debug.LogError(error);
                    return new AIResponse { Success = false, ErrorMessage = error };
                }
            }
        }
    }
}
