using UnityEngine;
using UnityEditor;
using System.IO;
using AICodingAssistant.Scripts;
using AICodingAssistant.Data;
using AICodingAssistant.AI;

namespace AICodingAssistant.Editor
{
    [InitializeOnLoad]
    public static class PlanExecutor
    {
        private static readonly string tempPlanPath = "Temp/ai_assistant_plan.json";

        static PlanExecutor()
        {
            EditorApplication.delayCall += ResumePlanExecution;
        }

        private static async void ResumePlanExecution()
        {
            if (File.Exists(tempPlanPath))
            {
                Debug.Log("[PlanExecutor] Found pending plan. Resuming execution...");
                string planJson = File.ReadAllText(tempPlanPath);
                
                File.Delete(tempPlanPath);

                var settings = GetPluginSettings();
                if (settings == null)
                {
                    Debug.LogError("[PlanExecutor] Could not find PluginSettings to resume execution.");
                    return;
                }

                var mainBackend = AIBackendFactory.CreateBackend(settings);
                var consoleMonitor = new EnhancedConsoleMonitor();

                AIOrchestrator orchestrator = new AIOrchestrator(mainBackend, consoleMonitor);

                string finalMessage = await orchestrator.ExecutePostCompilePlan(planJson);

                // This is the new feedback loop!
                AICodingAssistantWindow.AddSystemMessageToChat(finalMessage);
            }
        }

        private static PluginSettings GetPluginSettings()
        {
            var guids = AssetDatabase.FindAssets("t:PluginSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<PluginSettings>(path);
            }
            return null;
        }
    }
}