using System;

namespace AICodingAssistant.Editor
{
    /// <summary>
    /// Represents a search result within the codebase
    /// </summary>
    [Serializable]
    public class SearchResult
    {
        /// <summary>
        /// The path to the file containing the search result
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// The line number where the search result was found
        /// </summary>
        public int LineNumber { get; set; }
        
        /// <summary>
        /// The content of the line containing the search result
        /// </summary>
        public string Line { get; set; }
        
        /// <summary>
        /// The relevance score for this search result (0-100)
        /// </summary>
        public float RelevanceScore { get; set; }
        
        /// <summary>
        /// The context around the search result (if available)
        /// </summary>
        public string Context { get; set; }
        
        /// <summary>
        /// Creates a new search result instance
        /// </summary>
        public SearchResult() { }
        
        /// <summary>
        /// Creates a new search result with the specified properties
        /// </summary>
        public SearchResult(string filePath, int lineNumber, string line, float relevanceScore = 0)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
            Line = line;
            RelevanceScore = relevanceScore;
        }
        
        /// <summary>
        /// Creates a new search result with context
        /// </summary>
        public SearchResult(string filePath, int lineNumber, string line, string context, float relevanceScore = 0) 
            : this(filePath, lineNumber, line, relevanceScore)
        {
            Context = context;
        }
        
        /// <summary>
        /// Creates a new search result with the specified properties (compatibility constructor)
        /// </summary>
        public SearchResult(string filePath, int lineNumber, string line, int score)
            : this(filePath, lineNumber, line, (float)score)
        {
        }
    }
} 