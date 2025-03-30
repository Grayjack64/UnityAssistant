using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace AICodingAssistant.AI
{
    /// <summary>
    /// Implementation of the AIBackend for Grok (xAI)
    /// </summary>
    public class GrokBackend : AIBackend
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiEndpoint = "https://api.x.ai/v1/chat/completions";
        private string apiKey;
        private readonly string model = "grok-2-latest";  // Using the latest Grok model
        
        /// <summary>
        /// Create a new GrokBackend with the stored API key
        /// </summary>
        public GrokBackend()
        {
            // Load the API key from EditorPrefs
            apiKey = EditorPrefs.GetString("AICodingAssistant_GrokApiKey", "");
        }
        
        /// <summary>
        /// Send a request to the Grok API
        /// </summary>
        /// <param name="prompt">The prompt to send to the AI</param>
        /// <returns>The AI's response</returns>
        public override async Task<AIResponse> SendRequest(string prompt)
        {
            if (!IsConfigured())
            {
                return AIResponse.CreateError("Grok API key not configured. Please set it in the Settings tab.");
            }
            
            try
            {
                var request = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful AI coding assistant for Unity development." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7
                };
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");
                
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiEndpoint))
                {
                    requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
                    requestMessage.Content = content;
                    
                    var response = await client.SendAsync(requestMessage);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Debug.LogError($"Grok API error: {response.StatusCode} - {errorContent}");
                        return AIResponse.CreateError($"Error communicating with Grok: {response.StatusCode} - {errorContent}");
                    }
                    
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<GrokResponse>(responseBody);
                    
                    string responseText = responseObj?.Choices?[0]?.Message?.Content ?? "No response from Grok";
                    return AIResponse.CreateSuccess(responseText);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error communicating with Grok: {ex.Message}");
                return AIResponse.CreateError($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if this backend is properly configured
        /// </summary>
        /// <returns>True if the backend is ready to use</returns>
        public override bool IsConfigured()
        {
            return !string.IsNullOrEmpty(apiKey);
        }
        
        /// <summary>
        /// Get the name of this backend
        /// </summary>
        /// <returns>Backend name</returns>
        public override string GetName()
        {
            return "Grok (xAI)";
        }
        
        /// <summary>
        /// Set the API key for Grok
        /// </summary>
        /// <param name="key">API key</param>
        public void SetApiKey(string key)
        {
            apiKey = key;
            EditorPrefs.SetString("AICodingAssistant_GrokApiKey", key);
        }
    }
    
    /// <summary>
    /// Classes to deserialize Grok API responses
    /// </summary>
    [Serializable]
    public class GrokResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("object")]
        public string Object { get; set; }
        
        [JsonProperty("created")]
        public long Created { get; set; }
        
        [JsonProperty("model")]
        public string Model { get; set; }
        
        [JsonProperty("choices")]
        public GrokChoice[] Choices { get; set; }
    }
    
    [Serializable]
    public class GrokChoice
    {
        [JsonProperty("index")]
        public int Index { get; set; }
        
        [JsonProperty("message")]
        public GrokMessage Message { get; set; }
        
        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }
    
    [Serializable]
    public class GrokMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }
        
        [JsonProperty("content")]
        public string Content { get; set; }
    }
} 