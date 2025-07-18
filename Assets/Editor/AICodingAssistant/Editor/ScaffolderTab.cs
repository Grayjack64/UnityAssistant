using UnityEngine;
using UnityEditor;
using AICodingAssistant.Scripts;

namespace AICodingAssistant.Editor
{
    /// <summary>
    /// Handles the UI for the Project Scaffolder, which generates an entire GDD structure.
    /// </summary>
    public class ScaffolderTab
    {
        // --- User Input Fields ---
        private string projectName = "My New Game";
        private string projectGenre = "3D Action RPG";
        private string corePillars = "Fast-Paced Combat, Deep Story, High Replayability";
        private string gameplayLoop = "The player explores dungeons, defeats monsters to gather loot, returns to town to craft better gear, and then tackles harder dungeons.";
        
        // --- References ---
        private AICodingAssistantWindow aiWindow;
        private ProjectScaffolderManager scaffolderManager;

        // --- UI State ---
        private Vector2 scrollPosition;

        public ScaffolderTab(AICodingAssistantWindow window)
        {
            this.aiWindow = window;
            this.scaffolderManager = new ProjectScaffolderManager(window.CurrentBackend);
        }

        public void Draw()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Project Scaffolder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define your game's high-level concept, and the AI will generate the foundational GDD structure and files for all core systems.", MessageType.Info);
            
            EditorGUILayout.Space();

            // --- Input Form ---
            EditorGUILayout.LabelField("Project Definition", EditorStyles.boldLabel);
            projectName = EditorGUILayout.TextField("Project Name", projectName);
            projectGenre = EditorGUILayout.TextField("Genre", projectGenre);
            
            EditorGUILayout.LabelField("Core Pillars (comma-separated)");
            corePillars = EditorGUILayout.TextArea(corePillars, GUILayout.Height(40));

            EditorGUILayout.LabelField("Core Gameplay Loop");
            gameplayLoop = EditorGUILayout.TextArea(gameplayLoop, GUILayout.Height(80));

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Project Scaffold", GUILayout.Height(40)))
            {
                if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(projectGenre) || string.IsNullOrEmpty(corePillars) || string.IsNullOrEmpty(gameplayLoop))
                {
                    EditorUtility.DisplayDialog("Missing Information", "Please fill out all fields to generate the project scaffold.", "OK");
                }
                else
                {
                    // Call the manager to start the scaffolding process
                    _ = scaffolderManager.ScaffoldProject(projectName, projectGenre, corePillars, gameplayLoop);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
