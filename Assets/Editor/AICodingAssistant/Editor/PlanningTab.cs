using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AICodingAssistant.Scripts;
using System.IO;
using System.Linq;

namespace AICodingAssistant.Editor
{
    /// <summary>
    /// Provides a user interface for planning and executing AI actions
    /// </summary>
    public class PlanningTab
    {
        // UI state
        private Vector2 stepScrollPosition;
        private Vector2 detailScrollPosition;
        private string planDescription = "";
        private string planName = "New Plan";
        private bool showCreatePlan = false;
        private PlanStep selectedStep = null;
        
        // Constructor
        public PlanningTab()
        {
            // Register for planning system events
            PlanningSystem.Instance.OnPlanCreated += HandlePlanCreated;
            PlanningSystem.Instance.OnStepCompleted += HandleStepCompleted;
            PlanningSystem.Instance.OnStepFailed += HandleStepFailed;
        }
        
        // Clean up when this tab is no longer used
        ~PlanningTab()
        {
            // Unregister from planning system events
            if (PlanningSystem.Instance != null)
            {
                PlanningSystem.Instance.OnPlanCreated -= HandlePlanCreated;
                PlanningSystem.Instance.OnStepCompleted -= HandleStepCompleted;
                PlanningSystem.Instance.OnStepFailed -= HandleStepFailed;
            }
        }
        
        // Event handlers
        private void HandlePlanCreated(Plan plan)
        {
            Debug.Log($"Plan created: {plan.Name}");
        }
        
        private void HandleStepCompleted(PlanStep step)
        {
            Debug.Log($"Step completed: {step.Description}");
        }
        
        private void HandleStepFailed(PlanStep step, string error)
        {
            Debug.LogError($"Step failed: {step.Description}. Error: {error}");
        }
        
        // UI Drawing
        public void DrawGUI()
        {
            // Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Planning System", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Create and execute AI-driven plans for your project", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            // Main content
            EditorGUILayout.BeginHorizontal();
            
            // Left panel - Plan Management
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(300));
            DrawPlanManagementPanel();
            EditorGUILayout.EndVertical();
            
            // Right panel - Step Details
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawStepDetailsPanel();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawPlanManagementPanel()
        {
            EditorGUILayout.LabelField("Plans", EditorStyles.boldLabel);
            
            // Create new plan button
            if (GUILayout.Button("Create New Plan"))
            {
                showCreatePlan = !showCreatePlan;
            }
            
            // Create plan form
            if (showCreatePlan)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField("New Plan", EditorStyles.boldLabel);
                
                planName = EditorGUILayout.TextField("Name:", planName);
                
                EditorGUILayout.LabelField("Description:");
                planDescription = EditorGUILayout.TextArea(planDescription, GUILayout.Height(100));
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Cancel"))
                {
                    showCreatePlan = false;
                }
                
                if (GUILayout.Button("Create"))
                {
                    if (!string.IsNullOrEmpty(planName))
                    {
                        PlanningSystem.Instance.CreatePlan(planName, planDescription);
                        showCreatePlan = false;
                        planName = "New Plan";
                        planDescription = "";
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            // Display current plans
            Plan currentPlan = PlanningSystem.Instance.CurrentPlan;
            
            if (currentPlan != null)
            {
                EditorGUILayout.Space(10);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField(currentPlan.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(currentPlan.Description, EditorStyles.wordWrappedLabel);
                
                EditorGUILayout.Space(5);
                
                // Steps list
                EditorGUILayout.LabelField("Steps:", EditorStyles.boldLabel);
                
                stepScrollPosition = EditorGUILayout.BeginScrollView(stepScrollPosition, GUILayout.Height(200));
                
                for (int i = 0; i < currentPlan.Steps.Count; i++)
                {
                    PlanStep step = currentPlan.Steps[i];
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    // Status icon
                    GUIStyle iconStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                    Color originalColor = GUI.color;
                    
                    switch (step.Status)
                    {
                        case PlanStepStatus.Pending:
                            GUI.color = Color.gray;
                            EditorGUILayout.LabelField("⏳", iconStyle, GUILayout.Width(20));
                            break;
                        case PlanStepStatus.InProgress:
                            GUI.color = Color.yellow;
                            EditorGUILayout.LabelField("⚙️", iconStyle, GUILayout.Width(20));
                            break;
                        case PlanStepStatus.Completed:
                            GUI.color = Color.green;
                            EditorGUILayout.LabelField("✓", iconStyle, GUILayout.Width(20));
                            break;
                        case PlanStepStatus.Failed:
                            GUI.color = Color.red;
                            EditorGUILayout.LabelField("✗", iconStyle, GUILayout.Width(20));
                            break;
                    }
                    
                    GUI.color = originalColor;
                    
                    // Step description (clickable)
                    GUIStyle stepStyle = new GUIStyle(EditorStyles.label);
                    if (selectedStep == step)
                    {
                        stepStyle.fontStyle = FontStyle.Bold;
                    }
                    
                    if (GUILayout.Button(step.Description, stepStyle))
                    {
                        selectedStep = step;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space(10);
                
                // Plan control buttons
                EditorGUILayout.BeginHorizontal();
                
                if (!PlanningSystem.Instance.IsExecuting)
                {
                    if (GUILayout.Button("Start Execution"))
                    {
                        PlanningSystem.Instance.ExecutePlan();
                    }
                }
                else
                {
                    if (GUILayout.Button("Pause Execution"))
                    {
                        PlanningSystem.Instance.PauseExecution();
                    }
                }
                
                if (GUILayout.Button("Reset Plan"))
                {
                    PlanningSystem.Instance.ResetPlan();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No active plan. Create a new plan to get started.", MessageType.Info);
            }
        }
        
        private void DrawStepDetailsPanel()
        {
            EditorGUILayout.LabelField("Step Details", EditorStyles.boldLabel);
            
            if (selectedStep != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Display step details
                EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(selectedStep.Description, EditorStyles.wordWrappedLabel);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Type:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(selectedStep.StepType.ToString());
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(selectedStep.Status.ToString());
                
                EditorGUILayout.Space(5);
                
                // Display any output or result
                if (!string.IsNullOrEmpty(selectedStep.Result))
                {
                    EditorGUILayout.LabelField("Result:", EditorStyles.boldLabel);
                    
                    detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition, GUILayout.Height(150));
                    EditorGUILayout.TextArea(selectedStep.Result, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndScrollView();
                }
                
                EditorGUILayout.Space(10);
                
                // Step control buttons
                EditorGUILayout.BeginHorizontal();
                
                // Only show execute button for pending steps
                if (selectedStep.Status == PlanStepStatus.Pending || selectedStep.Status == PlanStepStatus.Failed)
                {
                    if (GUILayout.Button("Execute Step"))
                    {
                        ExecuteSelectedStep();
                    }
                }
                
                // Allow marking steps as completed or failed
                if (selectedStep.Status != PlanStepStatus.Completed)
                {
                    if (GUILayout.Button("Mark as Completed"))
                    {
                        PlanningSystem.Instance.MarkStepCompleted(selectedStep);
                    }
                }
                
                if (selectedStep.Status != PlanStepStatus.Failed)
                {
                    if (GUILayout.Button("Mark as Failed"))
                    {
                        PlanningSystem.Instance.MarkStepFailed(selectedStep, "Manually marked as failed");
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a step to view details", MessageType.Info);
            }
        }
        
        private void ExecuteSelectedStep()
        {
            if (selectedStep == null)
                return;
                
            switch (selectedStep.StepType)
            {
                case PlanStepType.Analysis:
                    // Execute analysis step
                    PlanningSystem.Instance.ExecuteStep(selectedStep);
                    break;
                    
                case PlanStepType.ScriptCreation:
                    // Execute script creation step
                    PlanningSystem.Instance.ExecuteStep(selectedStep);
                    break;
                    
                case PlanStepType.SceneOperation:
                    // Execute scene operation step
                    PlanningSystem.Instance.ExecuteStep(selectedStep);
                    break;
                    
                case PlanStepType.Guidance:
                    // Just mark guidance steps as completed
                    PlanningSystem.Instance.MarkStepCompleted(selectedStep);
                    break;
                    
                default:
                    Debug.LogWarning($"Unsupported step type: {selectedStep.StepType}");
                    break;
            }
        }
    }
} 