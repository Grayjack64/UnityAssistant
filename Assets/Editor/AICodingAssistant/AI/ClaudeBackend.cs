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
    /// Implementation of the AIBackend for Claude (Anthropic)
    /// </summary>
    public class ClaudeBackend : AIBackend
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiEndpoint = "https://api.anthropic.com/v1/messages";
        private string apiKey;
        private readonly string model = "claude-3-opus-20240229";  // Using Claude 3 Opus - you can change this to another Claude model
        
        /// <summary>
        /// Create a new ClaudeBackend with the stored API key
        /// </summary>
        public ClaudeBackend()
        {
            // Load the API key from EditorPrefs
            apiKey = EditorPrefs.GetString("AICodingAssistant_ClaudeApiKey", "");
        }
        
        /// <summary>
        /// Send a request to the Claude API
        /// </summary>
        /// <param name="prompt">The prompt to send to the AI</param>
        /// <returns>The AI's response as a string</returns>
        public override async Task<string> SendRequest(string prompt)
        {
            if (!IsConfigured())
            {
                return "Error: Claude API key not configured. Please set it in the Settings tab.";
            }
            
            try
            {
                var request = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    system = "You are a helpful AI coding assistant for Unity development. Provide clear, concise, and accurate advice. When sharing code, ensure it's correct and follows C# and Unity best practices.",
                    max_tokens = 4000,
                    temperature = 0.5
                };
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");
                
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiEndpoint))
                {
                    requestMessage.Headers.Add("x-api-key", apiKey);
                    requestMessage.Headers.Add("anthropic-version", "2023-06-01");
                    requestMessage.Content = content;
                    
                    var response = await client.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();
                    
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<ClaudeResponse>(responseBody);
                    
                    return responseObj?.Content?[0]?.Text ?? "No response from Claude";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error communicating with Claude: {ex.Message}");
                return $"Error: {ex.Message}";
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
            return "Claude (Anthropic)";
        }
        
        /// <summary>
        /// Set the API key for Claude
        /// </summary>
        /// <param name="key">API key</param>
        public void SetApiKey(string key)
        {
            apiKey = key;
            EditorPrefs.SetString("AICodingAssistant_ClaudeApiKey", key);
        }
    }
    
    /// <summary>
    /// Classes to deserialize Claude API responses
    /// </summary>
    [Serializable]
    public class ClaudeResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("role")]
        public string Role { get; set; }
        
        [JsonProperty("content")]
        public ClaudeContent[] Content { get; set; }
        
        [JsonProperty("model")]
        public string Model { get; set; }
        
        [JsonProperty("stop_reason")]
        public string StopReason { get; set; }
    }
    
    [Serializable]
    public class ClaudeContent
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("text")]
        public string Text { get; set; }
    }
} 