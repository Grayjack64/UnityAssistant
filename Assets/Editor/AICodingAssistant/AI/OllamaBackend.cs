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
    /// Implementation of the AIBackend for local LLMs via Ollama
    /// </summary>
    public class OllamaBackend : AIBackend
    {
        private static readonly HttpClient client = new HttpClient();
        private string baseUrl;
        private string modelName;
        
        /// <summary>
        /// Create a new OllamaBackend with default settings
        /// </summary>
        public OllamaBackend()
        {
            // Default values
            baseUrl = "http://localhost:11434";
            modelName = "llama2"; // Default model
            
            // Try to load saved settings
            if (EditorPrefs.HasKey("OllamaURL"))
            {
                baseUrl = EditorPrefs.GetString("OllamaURL");
            }
            
            if (EditorPrefs.HasKey("OllamaModel"))
            {
                modelName = EditorPrefs.GetString("OllamaModel");
            }
        }
        
        /// <summary>
        /// Send a request to the Ollama API
        /// </summary>
        /// <param name="prompt">The prompt to send to the AI</param>
        /// <returns>The AI's response</returns>
        public override async Task<AIResponse> SendRequest(string prompt)
        {
            try
            {
                var request = new
                {
                    model = modelName,
                    prompt = prompt,
                    stream = false
                };
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");
                
                var response = await client.PostAsync($"{baseUrl}/api/generate", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Ollama API error: {response.StatusCode} - {errorContent}");
                    return AIResponse.CreateError($"Error communicating with Ollama: {response.StatusCode} - {errorContent}");
                }
                
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<OllamaResponse>(responseBody);
                
                string responseText = responseObj?.Response ?? "No response from Ollama";
                return AIResponse.CreateSuccess(responseText);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error communicating with Ollama: {ex.Message}");
                return AIResponse.CreateError($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if this backend is properly configured
        /// </summary>
        /// <returns>True if the backend is ready to use</returns>
        public override bool IsConfigured()
        {
            // Since this is a local LLM, we just check if we have a model name
            return !string.IsNullOrEmpty(modelName);
        }
        
        /// <summary>
        /// Get the name of this backend
        /// </summary>
        /// <returns>Backend name</returns>
        public override string GetName()
        {
            return "Local LLM (Ollama)";
        }
        
        /// <summary>
        /// Set the Ollama model to use
        /// </summary>
        /// <param name="model">Model name (e.g., "llama2", "codellama")</param>
        public void SetModel(string model)
        {
            modelName = model;
            EditorPrefs.SetString("OllamaModel", model);
        }
        
        /// <summary>
        /// Set the Ollama server URL
        /// </summary>
        /// <param name="url">Server URL (e.g., "http://localhost:11434")</param>
        public void SetServerUrl(string url)
        {
            baseUrl = url;
            EditorPrefs.SetString("OllamaURL", url);
        }
    }
    
    /// <summary>
    /// Class to deserialize Ollama API responses
    /// </summary>
    [Serializable]
    public class OllamaResponse
    {
        [JsonProperty("response")]
        public string Response { get; set; }
        
        [JsonProperty("model")]
        public string Model { get; set; }
        
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
        
        [JsonProperty("done")]
        public bool Done { get; set; }
    }
} 