using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using AICodingAssistant.Editor;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Manages codebase context for the AI Coding Assistant
    /// </summary>
    public class CodebaseContext
    {
        private Dictionary<string, string> fileContents = new Dictionary<string, string>();
        private Dictionary<string, Dictionary<string, List<int>>> symbolDefinitions = new Dictionary<string, Dictionary<string, List<int>>>();
        private Dictionary<string, Dictionary<string, List<int>>> symbolReferences = new Dictionary<string, Dictionary<string, List<int>>>();
        private Dictionary<string, List<string>> fileImports = new Dictionary<string, List<string>>();
        private HashSet<string> analyzedFiles = new HashSet<string>();
        
        // File type filters
        private readonly string[] codeFileExtensions = { ".cs", ".js", ".shader", ".compute" };
        
        // Symbol pattern matching
        private readonly Regex classRegex = new Regex(@"class\s+(\w+)", RegexOptions.Compiled);
        private readonly Regex methodRegex = new Regex(@"(public|private|protected|internal|static)?\s*\w+\s+(\w+)\s*\(", RegexOptions.Compiled);
        private readonly Regex propertyRegex = new Regex(@"(public|private|protected|internal|static)?\s*\w+\s+(\w+)\s*{\s*get", RegexOptions.Compiled);
        private readonly Regex fieldRegex = new Regex(@"(public|private|protected|internal|static)?\s*\w+\s+(\w+)\s*;", RegexOptions.Compiled);
        private readonly Regex namespaceRegex = new Regex(@"namespace\s+([^{]+)", RegexOptions.Compiled);
        private readonly Regex usingRegex = new Regex(@"using\s+([^;]+);", RegexOptions.Compiled);
        
        /// <summary>
        /// Initialize the codebase context
        /// </summary>
        public async Task Initialize()
        {
            // Clear any existing data
            fileContents.Clear();
            symbolDefinitions.Clear();
            symbolReferences.Clear();
            fileImports.Clear();
            analyzedFiles.Clear();
            
            // Start analysis
            await ScanProjectFiles();
            
            Debug.Log($"Codebase context initialized: {fileContents.Count} files analyzed");
        }
        
        /// <summary>
        /// Scan all project files and build context
        /// </summary>
        private async Task ScanProjectFiles()
        {
            string assetsPath = Application.dataPath;
            string[] allFiles = Directory.GetFiles(assetsPath, "*.*", SearchOption.AllDirectories)
                .Where(f => codeFileExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray();
            
            int totalFiles = allFiles.Length;
            int filesProcessed = 0;
            
            foreach (string filePath in allFiles)
            {
                await Task.Run(() => AnalyzeFile(filePath));
                
                filesProcessed++;
                if (filesProcessed % 50 == 0)
                {
                    Debug.Log($"Codebase analysis progress: {filesProcessed}/{totalFiles} files");
                    
                    // Yield to prevent UI freezing during long analysis
                    await Task.Delay(1);
                }
            }
        }
        
        /// <summary>
        /// Analyze a single file for context building
        /// </summary>
        private void AnalyzeFile(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                string relativePath = GetRelativePath(filePath);
                
                // Store file content
                fileContents[relativePath] = content;
                analyzedFiles.Add(relativePath);
                
                // Extract symbols
                ExtractSymbols(relativePath, content);
                
                // Extract imports
                ExtractImports(relativePath, content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing file {filePath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Extract symbols (classes, methods, properties) from file content
        /// </summary>
        private void ExtractSymbols(string filePath, string content)
        {
            Dictionary<string, List<int>> symbols = new Dictionary<string, List<int>>();
            
            // Split by lines for line tracking
            string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            
            // Track current namespace
            string currentNamespace = string.Empty;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // Check for namespace
                var namespaceMatch = namespaceRegex.Match(line);
                if (namespaceMatch.Success)
                {
                    currentNamespace = namespaceMatch.Groups[1].Value.Trim();
                }
                
                // Check for class definition
                var classMatch = classRegex.Match(line);
                if (classMatch.Success)
                {
                    string className = classMatch.Groups[1].Value;
                    string fullClassName = !string.IsNullOrEmpty(currentNamespace) 
                        ? $"{currentNamespace}.{className}" 
                        : className;
                    
                    if (!symbols.ContainsKey(fullClassName))
                    {
                        symbols[fullClassName] = new List<int>();
                    }
                    symbols[fullClassName].Add(i + 1); // 1-based line numbers
                }
                
                // Check for method definition
                var methodMatch = methodRegex.Match(line);
                if (methodMatch.Success && methodMatch.Groups.Count > 2)
                {
                    string methodName = methodMatch.Groups[2].Value;
                    
                    if (!symbols.ContainsKey(methodName))
                    {
                        symbols[methodName] = new List<int>();
                    }
                    symbols[methodName].Add(i + 1);
                }
                
                // Check for property definition
                var propertyMatch = propertyRegex.Match(line);
                if (propertyMatch.Success && propertyMatch.Groups.Count > 2)
                {
                    string propertyName = propertyMatch.Groups[2].Value;
                    
                    if (!symbols.ContainsKey(propertyName))
                    {
                        symbols[propertyName] = new List<int>();
                    }
                    symbols[propertyName].Add(i + 1);
                }
                
                // Check for field definition
                var fieldMatch = fieldRegex.Match(line);
                if (fieldMatch.Success && fieldMatch.Groups.Count > 2)
                {
                    string fieldName = fieldMatch.Groups[2].Value;
                    
                    if (!symbols.ContainsKey(fieldName))
                    {
                        symbols[fieldName] = new List<int>();
                    }
                    symbols[fieldName].Add(i + 1);
                }
            }
            
            symbolDefinitions[filePath] = symbols;
            
            // TODO: Add symbol reference tracking in future enhancements
        }
        
        /// <summary>
        /// Extract namespace imports from file content
        /// </summary>
        private void ExtractImports(string filePath, string content)
        {
            List<string> imports = new List<string>();
            
            var matches = usingRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count > 1)
                {
                    imports.Add(match.Groups[1].Value.Trim());
                }
            }
            
            fileImports[filePath] = imports;
        }
        
        /// <summary>
        /// Convert absolute path to project-relative path
        /// </summary>
        private string GetRelativePath(string absolutePath)
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            return absolutePath.Replace(projectPath, "").TrimStart('/', '\\');
        }
        
        /// <summary>
        /// Search the codebase for a query
        /// </summary>
        /// <param name="query">The search query</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of search results with file paths and line numbers</returns>
        public List<SearchResult> Search(string query, int maxResults = 20)
        {
            List<SearchResult> results = new List<SearchResult>();
            
            // Case-insensitive search
            query = query.ToLowerInvariant();
            
            foreach (var file in fileContents)
            {
                string content = file.Value.ToLowerInvariant();
                if (content.Contains(query))
                {
                    // Find all line occurrences
                    string[] lines = file.Value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].ToLowerInvariant().Contains(query))
                        {
                            float relevanceScore = CalculateRelevanceScore(lines[i], query);
                            results.Add(new SearchResult(
                                file.Key,
                                i + 1,
                                lines[i].Trim(),
                                relevanceScore
                            ));
                            
                            if (results.Count >= maxResults)
                            {
                                break;
                            }
                        }
                    }
                }
                
                if (results.Count >= maxResults)
                {
                    break;
                }
            }
            
            // Sort by relevance score
            return results.OrderByDescending(r => r.RelevanceScore).ToList();
        }
        
        /// <summary>
        /// Find symbol definition in the codebase
        /// </summary>
        /// <param name="symbolName">Name of the symbol to find</param>
        /// <returns>List of files and lines where the symbol is defined</returns>
        public List<SearchResult> FindSymbol(string symbolName)
        {
            List<SearchResult> results = new List<SearchResult>();
            
            foreach (var fileSymbols in symbolDefinitions)
            {
                string filePath = fileSymbols.Key;
                var symbols = fileSymbols.Value;
                
                if (symbols.ContainsKey(symbolName))
                {
                    foreach (int lineNumber in symbols[symbolName])
                    {
                        // Get the specific line content
                        string line = GetLineContent(filePath, lineNumber);
                        
                        results.Add(new SearchResult(
                            filePath,
                            lineNumber,
                            line ?? $"Definition of {symbolName}",
                            100f // Exact symbol match has highest score
                        ));
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Get the content of a specific file at a specific line
        /// </summary>
        /// <param name="filePath">Relative path of the file</param>
        /// <param name="lineNumber">Line number (1-based)</param>
        /// <returns>The content of the line, or null if not found</returns>
        public string GetLineContent(string filePath, int lineNumber)
        {
            if (fileContents.ContainsKey(filePath))
            {
                string[] lines = fileContents[filePath].Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                
                if (lineNumber > 0 && lineNumber <= lines.Length)
                {
                    return lines[lineNumber - 1];
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get a range of lines from a file
        /// </summary>
        /// <param name="filePath">Relative path of the file</param>
        /// <param name="startLine">Start line number (1-based, inclusive)</param>
        /// <param name="endLine">End line number (1-based, inclusive)</param>
        /// <returns>The content of the lines as a single string</returns>
        public string GetFileContent(string filePath, int startLine = 1, int endLine = int.MaxValue)
        {
            if (fileContents.ContainsKey(filePath))
            {
                string[] lines = fileContents[filePath].Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                
                startLine = Math.Max(1, startLine);
                endLine = Math.Min(lines.Length, endLine);
                
                if (startLine <= endLine)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = startLine - 1; i < endLine; i++)
                    {
                        sb.AppendLine(lines[i]);
                    }
                    return sb.ToString();
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get all project files that have been analyzed
        /// </summary>
        public List<string> GetAnalyzedFiles()
        {
            return analyzedFiles.ToList();
        }
        
        /// <summary>
        /// Calculate a relevance score for search results
        /// </summary>
        private float CalculateRelevanceScore(string line, string query)
        {
            float score = 10f; // Base score
            
            // Exact match gets higher score
            if (line.ToLowerInvariant().Contains($" {query} "))
            {
                score += 50f;
            }
            
            // Match at the beginning of a word
            if (Regex.IsMatch(line.ToLowerInvariant(), $"\\b{Regex.Escape(query)}"))
            {
                score += 30f;
            }
            
            // Count occurrences
            int count = Regex.Matches(line.ToLowerInvariant(), Regex.Escape(query)).Count;
            score += count * 5f;
            
            return score;
        }
    }
} 