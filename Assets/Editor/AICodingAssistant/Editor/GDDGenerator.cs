using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace AICodingAssistant.Editor
{
    /// <summary>
    /// This editor script creates the standardized Game Design Document (GDD) structure,
    /// including directories, Markdown templates, and JSON knowledge base files.
    /// </summary>
    public static class GDDGenerator
    {
        [MenuItem("AI Coding Assistant/Generate GDD Structure")]
        public static void GenerateGDD()
        {
            string rootPath = "Assets/GameDesignDocument";

            if (Directory.Exists(rootPath))
            {
                if (!EditorUtility.DisplayDialog("GDD Structure Exists",
                    "The GameDesignDocument folder already exists. Overwriting may lose data. Are you sure you want to proceed?",
                    "Yes, Overwrite", "No, Cancel"))
                {
                    Debug.Log("GDD generation cancelled by user.");
                    return;
                }
            }

            Debug.Log("Starting GDD structure generation...");

            // --- Create Directories ---
            Directory.CreateDirectory(rootPath);
            Directory.CreateDirectory(Path.Combine(rootPath, "02_Game_Mechanics"));
            Directory.CreateDirectory(Path.Combine(rootPath, "03_Game_Systems"));
            Directory.CreateDirectory(Path.Combine(rootPath, "05_Content"));
            Debug.Log("Created all necessary directories.");

            // --- Create Files and Templates ---
            CreateFile(Path.Combine(rootPath, "00_Overview.md"), GetOverviewTemplate());
            CreateFile(Path.Combine(rootPath, "01_Core_Gameplay_Loop.md"), GetCoreGameplayLoopTemplate());
            CreateFile(Path.Combine(rootPath, "04_User_Interface.md"), GetUITemplate());
            CreateFile(Path.Combine(rootPath, "06_Technical_Specifications.md"), GetTechSpecsTemplate());
            CreateFile(Path.Combine(rootPath, "07_Asset_Index.md"), GetAssetIndexTemplate());
            CreateFile(Path.Combine(rootPath, "08_Feature_Requests.md"), GetFeatureRequestTemplate());
            CreateFile(Path.Combine(rootPath, "09_AI_Integration_Guide.md"), GetAIIntegrationGuideTemplate());

            // --- Create Sub-directory Templates ---
            CreateFile(Path.Combine(rootPath, "02_Game_Mechanics", "_template.md"), GetMechanicTemplate());
            CreateFile(Path.Combine(rootPath, "03_Game_Systems", "_template.md"), GetSystemTemplate());
            
            // --- Create JSON Files ---
            CreateFile(Path.Combine(rootPath, "ai_knowledgebase.json"), GetKnowledgeBaseTemplate());
            CreateFile(Path.Combine(rootPath, "validation_schema.json"), GetValidationSchemaTemplate());

            Debug.Log("GDD structure generation complete!");
            AssetDatabase.Refresh();
        }

        private static void CreateFile(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
                Debug.Log($"Created file: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create file {path}. Reason: {e.Message}");
            }
        }

        // --- Template Content Methods ---

        private static string GetOverviewTemplate() =>
@"# Project Overview

## High Concept
*A one-sentence pitch for the game.*

## Genre
*Primary and secondary genres.*

## Target Audience
*Who is this game for?*

## Core Pillars
*3-5 key design principles that guide all development decisions.*
";

        private static string GetCoreGameplayLoopTemplate() =>
@"# Core Gameplay Loop

## Loop Summary
*Describe the primary sequence of actions the player will repeat.*

## Detailed Steps
1.  **Phase 1:** Description of the first phase.
2.  **Phase 2:** Description of the second phase.
3.  **Phase 3:** Description of the third phase.

## Player Motivation
*What drives the player to continue the loop?*
";

        private static string GetSystemTemplate() =>
@"# System: [System Name]

## Purpose
*One-sentence description of what this system does.*

## Integration Points
-   **Input Dependencies**: What this system needs from other systems.
-   **Output Provided**: What this system provides to other systems.
-   **Events Triggered**: List of events this system can trigger.

## Data Schema
```json
{
  ""requiredFields"": [""field1"", ""field2""],
  ""optionalFields"": [""field3""],
  ""validationRules"": [""field1 > 0""]
}
```

## AI Implementation Guide
-   **When to use**: Trigger conditions for this system.
-   **Common patterns**: Code snippets and patterns to follow.
-   **Anti-patterns**: What to avoid when implementing.
-   **Test scenarios**: Specific test cases to validate implementation.

## Implementation Checklist
- [ ] Core component created
- [ ] Event system integrated
- [ ] Unit tests written
- [ ] Documentation updated
";
        
        private static string GetMechanicTemplate() =>
@"# Mechanic: [Mechanic Name]

## Description
*A detailed explanation of how this mechanic works from the player's perspective.*

## Core Logic
*Breakdown of the rules and calculations that govern this mechanic.*

## Controls
*How does the player interact with this mechanic?*

## AI Implementation Guide
-   **Related Components**: List of scripts that should be used or created.
-   **API Methods**: Key functions the AI should call or implement.
";

        private static string GetUITemplate() => @"# User Interface Design";
        private static string GetTechSpecsTemplate() => @"# Technical Specifications";
        private static string GetAssetIndexTemplate() => @"# Asset Index";
        private static string GetFeatureRequestTemplate() =>
@"# Feature Request: [Feature Name]

## User Story
As a [user type], I want [functionality] so that [benefit].

## Affected Systems
-   **System A**: [how it's affected]
-   **System B**: [integration points]

## Implementation Approach
1.  [Step 1]
2.  [Step 2]

## AI Context
-   **Similar existing features**: [references]
-   **Required components**: [list]
-   **Integration points**: [specific areas]
";
        private static string GetAIIntegrationGuideTemplate() => @"# AI Integration Guide";

        private static string GetKnowledgeBaseTemplate()
        {
            // Using StringBuilder for cleaner multi-line JSON construction
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"lastUpdated\": \"2025-07-18T19:45:00Z\",");
            sb.AppendLine("  \"projectInfo\": {");
            sb.AppendLine("    \"name\": \"YourProjectName\",");
            sb.AppendLine("    \"version\": \"0.1.0\",");
            sb.AppendLine("    \"gameEngine\": \"Unity 6000.2.0b9\",");
            sb.AppendLine("    \"targetPlatform\": [\"PC\"]");
            sb.AppendLine("  },");
            sb.AppendLine("  \"systems\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"name\": \"Health System\",");
            sb.AppendLine("      \"description\": \"Manages health, damage, and death states for game entities.\",");
            sb.AppendLine("      \"keyData\": [\"HP\", \"MaxHP\", \"IsDead\"],");
            sb.AppendLine("      \"relatedComponents\": [\"HealthComponent\", \"DamageDealerComponent\"],");
            sb.AppendLine("      \"gddPath\": \"GameDesignDocument/03_Game_Systems/03_01_Health_System.md\",");
            sb.AppendLine("      \"apiMethods\": [\"TakeDamage(int)\", \"Heal(int)\", \"Die()\"],");
            sb.AppendLine("      \"events\": [\"OnDamage\", \"OnDeath\", \"OnHeal\"],");
            sb.AppendLine("      \"dependencies\": [],");
            sb.AppendLine("      \"dependents\": [\"Combat System\", \"UI System\"]");
            sb.AppendLine("    }");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"dataStructures\": {},");
            sb.AppendLine("  \"codePatterns\": {");
            sb.AppendLine("    \"componentCreation\": \"Always inherit from MonoBehaviour, use [SerializeField] for inspector fields\",");
            sb.AppendLine("    \"eventHandling\": \"Use UnityEvents for loose coupling between systems\",");
            sb.AppendLine("    \"dataAccess\": \"Use ScriptableObjects for game data, avoid singletons\"");
            sb.AppendLine("  },");
            sb.AppendLine("  \"dependencyGraph\": {");
            sb.AppendLine("    \"nodes\": [\"HealthSystem\", \"CombatSystem\", \"UISystem\"],");
            sb.AppendLine("    \"edges\": [");
            sb.AppendLine("      {\"from\": \"CombatSystem\", \"to\": \"HealthSystem\", \"type\": \"uses\"},");
            sb.AppendLine("      {\"from\": \"UISystem\", \"to\": \"HealthSystem\", \"type\": \"observes\"}");
            sb.AppendLine("    ]");
            sb.AppendLine("  },");
            sb.AppendLine("  \"codeGenerationRules\": {},");
            sb.AppendLine("  \"testingFramework\": {},");
            sb.AppendLine("  \"versionControl\": {}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GetValidationSchemaTemplate()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"$schema\": \"http://json-schema.org/draft-07/schema#\",");
            sb.AppendLine("  \"title\": \"AI Knowledge Base Schema\",");
            sb.AppendLine("  \"description\": \"Defines the structure for the ai_knowledgebase.json file.\",");
            sb.AppendLine("  \"type\": \"object\",");
            sb.AppendLine("  \"properties\": {");
            sb.AppendLine("    \"lastUpdated\": { \"type\": \"string\", \"format\": \"date-time\" }");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
