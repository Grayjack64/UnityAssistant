using System;

namespace AICodingAssistant.AI
{
    /// <summary>
    /// Represents a response from an AI backend
    /// </summary>
    public class AIResponse
    {
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The response message from the AI
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Error message, if any
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Create a successful response
        /// </summary>
        /// <param name="message">The AI's response</param>
        /// <returns>A success response</returns>
        public static AIResponse CreateSuccess(string message)
        {
            return new AIResponse
            {
                Success = true,
                Message = message,
                ErrorMessage = null
            };
        }
        
        /// <summary>
        /// Create a failed response
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <returns>A failure response</returns>
        public static AIResponse CreateError(string errorMessage)
        {
            return new AIResponse
            {
                Success = false,
                Message = null,
                ErrorMessage = errorMessage
            };
        }
    }
} 