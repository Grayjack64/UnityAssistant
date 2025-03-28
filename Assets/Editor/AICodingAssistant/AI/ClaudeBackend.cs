using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        private readonly string messagesEndpoint = "https://api.anthropic.com/v1/messages";
        private readonly string modelsEndpoint = "https://api.anthropic.com/v1/models";
        private string apiKey;
        private string model = "claude-3-opus-20240229";  // Default model
        
        // Cache of available models to avoid excessive API calls
        private List<ClaudeModel> availableModels = new List<ClaudeModel>();
        private DateTime lastModelsFetchTime = DateTime.MinValue;
        private readonly TimeSpan modelsCacheTimeout = TimeSpan.FromHours(1); // Refresh model list every hour
        
        /// <summary>
        /// Create a new ClaudeBackend with the stored API key and model
        /// </summary>
        public ClaudeBackend()
        {
            // Load the API key and model from EditorPrefs
            apiKey = EditorPrefs.GetString("AICodingAssistant_ClaudeApiKey", "");
            model = EditorPrefs.GetString("AICodingAssistant_ClaudeModel", "claude-3-opus-20240229");
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
                
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, messagesEndpoint))
                {
                    requestMessage.Headers.Add("x-api-key", apiKey);
                    requestMessage.Headers.Add("anthropic-version", "2023-06-01");
                    requestMessage.Content = content;
                    
                    var response = await client.SendAsync(requestMessage);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Debug.LogError($"Claude API error: {response.StatusCode} - {errorContent}");
                        return $"Error communicating with Claude: {response.StatusCode} - {errorContent}";
                    }
                    
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
        /// Fetch the list of available models from the Anthropic API
        /// </summary>
        /// <returns>List of available models</returns>
        public async Task<List<ClaudeModel>> ListModels()
        {
            // Return cached models if still valid
            if (availableModels.Count > 0 && DateTime.Now - lastModelsFetchTime < modelsCacheTimeout)
            {
                Debug.Log($"Using cached Claude models ({availableModels.Count} models)");
                return availableModels;
            }
            
            // Clear existing models
            availableModels.Clear();
            
            if (!IsConfigured())
            {
                Debug.LogWarning("Cannot fetch Claude models: API key not configured");
                // Add some default models so the UI isn't empty
                availableModels.Add(new ClaudeModel { Id = "claude-3-opus-20240229", DisplayName = "Claude 3 Opus" });
                availableModels.Add(new ClaudeModel { Id = "claude-3-sonnet-20240229", DisplayName = "Claude 3 Sonnet" });
                availableModels.Add(new ClaudeModel { Id = "claude-3-haiku-20240307", DisplayName = "Claude 3 Haiku" });
                return availableModels;
            }
            
            try
            {
                Debug.Log($"Requesting models from Anthropic API endpoint: {modelsEndpoint}");
                
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, modelsEndpoint))
                {
                    requestMessage.Headers.Add("x-api-key", apiKey);
                    requestMessage.Headers.Add("anthropic-version", "2023-06-01");
                    
                    // Log the request headers for debugging
                    Debug.Log("Request headers:");
                    foreach (var header in requestMessage.Headers)
                    {
                        Debug.Log($"  {header.Key}: {(header.Key.ToLower() == "x-api-key" ? "[REDACTED]" : string.Join(", ", header.Value))}");
                    }
                    
                    var response = await client.SendAsync(requestMessage);
                    Debug.Log($"Response status code: {response.StatusCode}");
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Debug.LogError($"Claude API error fetching models: {response.StatusCode} - {errorContent}");
                        
                        // Add default models on error
                        availableModels.Add(new ClaudeModel { Id = "claude-3-opus-20240229", DisplayName = "Claude 3 Opus" });
                        availableModels.Add(new ClaudeModel { Id = "claude-3-sonnet-20240229", DisplayName = "Claude 3 Sonnet" });
                        availableModels.Add(new ClaudeModel { Id = "claude-3-haiku-20240307", DisplayName = "Claude 3 Haiku" });
                        
                        return availableModels;
                    }
                    
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Response body preview: {(responseBody.Length > 100 ? responseBody.Substring(0, 100) + "..." : responseBody)}");
                    
                    try
                    {
                        var modelsResponse = JsonConvert.DeserializeObject<ClaudeModelsResponse>(responseBody);
                        
                        if (modelsResponse?.Data != null)
                        {
                            Debug.Log($"Successfully parsed {modelsResponse.Data.Count} models from response");
                            availableModels.AddRange(modelsResponse.Data);
                            lastModelsFetchTime = DateTime.Now;
                            
                            // Log the models we received
                            foreach (var model in modelsResponse.Data)
                            {
                                Debug.Log($"Retrieved model: {model.DisplayName} (ID: {model.Id})");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Response was successful but contained no models data");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Debug.LogError($"Error parsing JSON response: {jsonEx.Message}");
                        Debug.LogError($"Response content: {responseBody}");
                        throw;
                    }
                    
                    // If for some reason we got an empty list, add default models
                    if (availableModels.Count == 0)
                    {
                        Debug.LogWarning("No models returned from API, using defaults");
                        availableModels.Add(new ClaudeModel { Id = "claude-3-opus-20240229", DisplayName = "Claude 3 Opus" });
                        availableModels.Add(new ClaudeModel { Id = "claude-3-sonnet-20240229", DisplayName = "Claude 3 Sonnet" });
                        availableModels.Add(new ClaudeModel { Id = "claude-3-haiku-20240307", DisplayName = "Claude 3 Haiku" });
                    }
                    
                    return availableModels;
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.LogError($"HTTP request error fetching Claude models: {httpEx.Message}");
                
                // Add default models on error
                availableModels.Add(new ClaudeModel { Id = "claude-3-opus-20240229", DisplayName = "Claude 3 Opus" });
                availableModels.Add(new ClaudeModel { Id = "claude-3-sonnet-20240229", DisplayName = "Claude 3 Sonnet" });
                availableModels.Add(new ClaudeModel { Id = "claude-3-haiku-20240307", DisplayName = "Claude 3 Haiku" });
                
                return availableModels;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching Claude models: {ex.GetType().Name} - {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
                
                // Add default models on error
                availableModels.Add(new ClaudeModel { Id = "claude-3-opus-20240229", DisplayName = "Claude 3 Opus" });
                availableModels.Add(new ClaudeModel { Id = "claude-3-sonnet-20240229", DisplayName = "Claude 3 Sonnet" });
                availableModels.Add(new ClaudeModel { Id = "claude-3-haiku-20240307", DisplayName = "Claude 3 Haiku" });
                
                return availableModels;
            }
        }
        
        /// <summary>
        /// Get a list of model names (display names) for use in the UI
        /// </summary>
        /// <returns>Array of model display names</returns>
        public async Task<string[]> GetModelDisplayNames()
        {
            var models = await ListModels();
            string[] displayNames = new string[models.Count];
            
            for (int i = 0; i < models.Count; i++)
            {
                displayNames[i] = models[i].DisplayName;
            }
            
            return displayNames;
        }
        
        /// <summary>
        /// Get the model ID for a given display name
        /// </summary>
        /// <param name="displayName">The display name of the model</param>
        /// <returns>The model ID or the default model if not found</returns>
        public async Task<string> GetModelIdFromDisplayName(string displayName)
        {
            var models = await ListModels();
            foreach (var model in models)
            {
                if (model.DisplayName == displayName)
                {
                    return model.Id;
                }
            }
            
            // Return default if not found
            return "claude-3-opus-20240229";
        }
        
        /// <summary>
        /// Get the display name for the current model
        /// </summary>
        /// <returns>The display name of the current model or a default if not found</returns>
        public async Task<string> GetCurrentModelDisplayName()
        {
            var models = await ListModels();
            foreach (var claudeModel in models)
            {
                if (claudeModel.Id == model)
                {
                    return claudeModel.DisplayName;
                }
            }
            
            // Return something reasonable if not found
            return "Claude 3 Opus";
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
            
            // Clear cached models when API key changes
            availableModels.Clear();
            lastModelsFetchTime = DateTime.MinValue;
        }
        
        /// <summary>
        /// Set the model for Claude
        /// </summary>
        /// <param name="modelId">Model ID</param>
        public void SetModel(string modelId)
        {
            model = modelId;
            EditorPrefs.SetString("AICodingAssistant_ClaudeModel", modelId);
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
    
    /// <summary>
    /// Model information from Claude API
    /// </summary>
    [Serializable]
    public class ClaudeModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }
        
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
    }
    
    /// <summary>
    /// Response for the models listing endpoint
    /// </summary>
    [Serializable]
    public class ClaudeModelsResponse
    {
        [JsonProperty("data")]
        public List<ClaudeModel> Data { get; set; }
        
        [JsonProperty("has_more")]
        public bool HasMore { get; set; }
        
        [JsonProperty("first_id")]
        public string FirstId { get; set; }
        
        [JsonProperty("last_id")]
        public string LastId { get; set; }
    }
} 