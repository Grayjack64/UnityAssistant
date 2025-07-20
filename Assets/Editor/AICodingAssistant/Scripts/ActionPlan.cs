using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Represents a single tool call in the AI's plan.
    /// </summary>
    public class ToolCall
    {
        [JsonProperty("tool")]
        public string Tool { get; set; }

        [JsonProperty("arguments")]
        public JObject Arguments { get; set; } // Use JObject for flexible argument parsing

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents the root of the AI's JSON plan.
    /// </summary>
    public class ActionPlan
    {
        [JsonProperty("plan")]
        public List<ToolCall> Plan { get; set; }
    }
}
