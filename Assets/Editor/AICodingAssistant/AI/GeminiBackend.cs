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
    /// Implementation of the AIBackend for Google Gemini
    /// </summary>
    public class GeminiBackend : AIBackend
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models";
        private string apiKey;
        private string model = "gemini-2.0-flash"; // Default model
        
        // Available Gemini models
        private readonly string[] availableModels = new string[]
        {
            "gemini-2.0-flash",
            "gemini-2.0-pro",
            "gemini-2.5-pro-exp-03-25" // Experimental 2.5 model
        };
        
        /// <summary>
        /// Create a new GeminiBackend with the stored API key
        /// </summary>
        public GeminiBackend()
        {
            // Load the API key and model from EditorPrefs
            apiKey = EditorPrefs.GetString("AICodingAssistant_GeminiApiKey", "");
            model = EditorPrefs.GetString("AICodingAssistant_GeminiModel", "gemini-2.0-flash");
        }
        
        /// <summary>
        /// Send a request to the Gemini API
        /// </summary>
        /// <param name="prompt">The prompt to send to the AI</param>
        /// <returns>The AI's response as a string</returns>
        public override async Task<string> SendRequest(string prompt)
        {
            try
            {
                // Create request object with system instruction
                var request = new GeminiRequestWithSystem
                {
                    SystemInstruction = new GeminiInstruction
                    {
                        Parts = new GeminiPart[]
                        {
                            new GeminiPart
                            {
                                Text = "You are an AI assistant for Unity game development. You provide helpful, accurate, and concise answers to questions about Unity and C# programming."
                            }
                        }
                    },
                    Contents = new GeminiContent[]
                    {
                        new GeminiContent 
                        { 
                            Parts = new GeminiPart[]
                            {
                                new GeminiPart
                                {
                                    Text = prompt
                                }
                            }
                        }
                    }
                };
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");
                
                var url = $"{apiEndpoint}/{model}:generateContent?key={apiKey}";
                
                Debug.Log($"Sending request to Gemini API: {url.Replace(apiKey, "API_KEY_HIDDEN")}");
                
                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                
                var responseBody = await response.Content.ReadAsStringAsync();
                Debug.Log($"Raw Gemini response: {responseBody}");
                var responseObj = JsonConvert.DeserializeObject<GeminiResponse>(responseBody);
                
                if (responseObj?.Candidates != null && responseObj.Candidates.Length > 0 &&
                    responseObj.Candidates[0]?.Content?.Parts != null &&
                    responseObj.Candidates[0].Content.Parts.Length > 0)
                {
                    return responseObj.Candidates[0].Content.Parts[0].Text;
                }
                else
                {
                    // Fallback to full response if we can't extract the text
                    return $"Error parsing Gemini response: {responseBody}";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error communicating with Gemini: {ex.Message}");
                if (ex is HttpRequestException httpEx)
                {
                    Debug.LogError($"HTTP Status: {httpEx.StatusCode}");
                }
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
            return "Google Gemini";
        }
        
        /// <summary>
        /// Set the API key for Gemini
        /// </summary>
        /// <param name="key">API key</param>
        public void SetApiKey(string key)
        {
            apiKey = key;
            EditorPrefs.SetString("AICodingAssistant_GeminiApiKey", key);
        }
        
        /// <summary>
        /// Set the model to use
        /// </summary>
        /// <param name="modelName">Model name</param>
        public void SetModel(string modelName)
        {
            if (Array.IndexOf(availableModels, modelName) >= 0)
            {
                model = modelName;
                EditorPrefs.SetString("AICodingAssistant_GeminiModel", modelName);
            }
            else
            {
                Debug.LogWarning($"Unknown Gemini model: {modelName}. Using default model.");
                model = "gemini-2.0-flash";
                EditorPrefs.SetString("AICodingAssistant_GeminiModel", model);
            }
        }
        
        /// <summary>
        /// Get all available models
        /// </summary>
        /// <returns>Array of model names</returns>
        public string[] GetAvailableModels()
        {
            return availableModels;
        }
        
        /// <summary>
        /// Get user-friendly display names for the models
        /// </summary>
        /// <returns>Array of display names</returns>
        public string[] GetModelDisplayNames()
        {
            return new string[]
            {
                "Gemini 2.0 Flash",
                "Gemini 2.0 Pro",
                "Gemini 2.5 Pro (Experimental)"
            };
        }
        
        /// <summary>
        /// Get the model ID from a display name
        /// </summary>
        /// <param name="displayName">Display name</param>
        /// <returns>Model ID</returns>
        public string GetModelIdFromDisplayName(string displayName)
        {
            var displayNames = GetModelDisplayNames();
            var modelIndex = Array.IndexOf(displayNames, displayName);
            
            if (modelIndex >= 0 && modelIndex < availableModels.Length)
            {
                return availableModels[modelIndex];
            }
            
            return "gemini-2.0-flash"; // Default
        }
        
        /// <summary>
        /// Get the display name for the current model
        /// </summary>
        /// <returns>Display name</returns>
        public string GetCurrentModelDisplayName()
        {
            var modelIndex = Array.IndexOf(availableModels, model);
            var displayNames = GetModelDisplayNames();
            
            if (modelIndex >= 0 && modelIndex < displayNames.Length)
            {
                return displayNames[modelIndex];
            }
            
            return "Gemini 2.0 Flash"; // Default
        }
    }
    
    // Classes for Gemini API serialization
    
    [Serializable]
    public class GeminiRequest
    {
        [JsonProperty("contents")]
        public GeminiContent[] Contents { get; set; }
    }
    
    [Serializable]
    public class GeminiRequestWithSystem
    {
        [JsonProperty("system_instruction")]
        public GeminiInstruction SystemInstruction { get; set; }
        
        [JsonProperty("contents")]
        public GeminiContent[] Contents { get; set; }
    }
    
    [Serializable]
    public class GeminiInstruction
    {
        [JsonProperty("parts")]
        public GeminiPart[] Parts { get; set; }
    }
    
    [Serializable]
    public class GeminiContent
    {
        [JsonProperty("parts")]
        public GeminiPart[] Parts { get; set; }
    }
    
    [Serializable]
    public class GeminiPart
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
    
    [Serializable]
    public class GeminiResponse
    {
        [JsonProperty("candidates")]
        public GeminiCandidate[] Candidates { get; set; }
    }
    
    [Serializable]
    public class GeminiCandidate
    {
        [JsonProperty("content")]
        public GeminiContent Content { get; set; }
        
        [JsonProperty("finishReason")]
        public string FinishReason { get; set; }
    }
} 