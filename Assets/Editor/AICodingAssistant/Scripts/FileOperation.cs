using Newtonsoft.Json;
using System.Collections.Generic;

namespace AICodingAssistant.Scripts
{
    // A simple data structure to hold the file operations planned by the local AI.
    public class FileOperation
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    // A helper class to represent the root of the JSON object.
    public class OperationPlan
    {
        [JsonProperty("operations")]
        public List<FileOperation> Operations { get; set; }
    }
}
