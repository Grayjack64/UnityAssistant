using System;

namespace AICodingAssistant.Editor
{
    [Serializable]
    public class ChatMessage
    {
        public bool IsUser { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsNew { get; set; }
        public bool IsSystemMessage { get; set; }
    }
}