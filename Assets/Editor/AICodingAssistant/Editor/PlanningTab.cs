using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AICodingAssistant.Scripts;
using AICodingAssistant.AI;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AICodingAssistant.Planning;

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
        
        // Reference to AI window for generating plans
        private AICodingAssistantWindow aiWindow;
        
        // Constructor
        public PlanningTab(AICodingAssistantWindow window = null)
        {
            aiWindow = window;
            
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
                
                EditorGUILayout.LabelField("Description (Natural Language Instructions):");
                EditorGUILayout.HelpBox("Describe what you want to accomplish, and the AI will create a plan with steps.", MessageType.Info);
                planDescription = EditorGUILayout.TextArea(planDescription, GUILayout.Height(100));
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Cancel"))
                {
                    showCreatePlan = false;
                }
                
                if (GUILayout.Button("Create Manually"))
                {
                    if (!string.IsNullOrEmpty(planName) && !string.IsNullOrEmpty(planDescription))
                    {
                        var plan = PlanningSystem.Instance.CreatePlan(planName, planDescription);
                        showCreatePlan = false;
                        planName = "New Plan";
                        planDescription = "";
                    }
                }
                
                if (GUILayout.Button("Generate AI Plan"))
                {
                    if (!string.IsNullOrEmpty(planName) && !string.IsNullOrEmpty(planDescription))
                    {
                        GenerateAIPlan(planName, planDescription);
                        showCreatePlan = false;
                        planName = "New Plan";
                        planDescription = "";
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Missing Information", 
                            "Please provide both a name and description for your plan.", "OK");
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
            
            // Always try to use AI-powered execution if we have an AI window reference
            if (aiWindow != null)
            {
                ExecuteStepWithAI(selectedStep);
                return;
            }
                
            // Fallback to basic execution if no AI window is available
            switch (selectedStep.StepType)
            {
                case PlanStepType.Analysis:
                    PlanningSystem.Instance.ExecuteStep(selectedStep);
                    break;
                    
                case PlanStepType.ScriptCreation:
                    PlanningSystem.Instance.ExecuteStep(selectedStep);
                    break;
                    
                case PlanStepType.SceneOperation:
                    PlanningSystem.Instance.ExecuteStep(selectedStep);
                    break;
                    
                case PlanStepType.Guidance:
                    PlanningSystem.Instance.MarkStepCompleted(selectedStep);
                    break;
                    
                default:
                    Debug.LogWarning($"Unsupported step type: {selectedStep.StepType}");
                    break;
            }
        }
        
        /// <summary>
        /// Execute a step using the AI
        /// </summary>
        /// <param name="step">The step to execute</param>
        private async void ExecuteStepWithAI(PlanStep step)
        {
            if (step == null || aiWindow == null)
                return;
            
            try
            {
                // Show progress dialog
                EditorUtility.DisplayProgressBar("Executing Step", $"The AI is working on: {step.Description}", 0.5f);
                
                // Add system message to indicate AI is executing this step automatically
                aiWindow.AddSystemMessage($"🤖 AI is automatically executing plan step: {step.Description}", true);
                
                // Use AI to execute the step using a callback to the AI window
                await PlanningSystem.Instance.AskAIToExecuteStep(step, SendAIRequest);
                
                // If this step was completed successfully, automatically proceed to the next step
                if (step.Status == PlanStepStatus.Completed && PlanningSystem.Instance.IsExecuting)
                {
                    PlanStep nextStep = PlanningSystem.Instance.CurrentPlan.GetNextPendingStep();
                    if (nextStep != null)
                    {
                        selectedStep = nextStep;
                        ExecuteStepWithAI(nextStep);
                    }
                    else
                    {
                        aiWindow.AddSystemMessage("✅ All plan steps have been executed successfully!", true);
                    }
                }
                
                // Refresh the UI
                EditorWindow.GetWindow<AICodingAssistantWindow>().Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing step with AI: {ex.Message}");
                aiWindow.AddSystemMessage($"❌ Error executing step: {ex.Message}", true);
                EditorUtility.DisplayDialog("Step Execution Failed", 
                    $"Failed to execute step: {ex.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        
        /// <summary>
        /// Send a request to the AI backend via the AI window
        /// </summary>
        /// <param name="prompt">The prompt to send</param>
        /// <returns>AI's response</returns>
        private async Task<string> SendAIRequest(string prompt)
        {
            if (aiWindow == null)
            {
                throw new InvalidOperationException("AI Window reference is null");
            }
            
            try
            {
                // Add the prompt to the chat as a special message
                aiWindow.AddSystemMessage($"🔄 Executing plan step: {selectedStep.Description}", true);
                
                // Send to the AI backend and get response
                AIResponse response = await aiWindow.GetCurrentBackend().SendRequest(prompt);
                
                if (response.Success)
                {
                    // Notify the user about the completion
                    aiWindow.AddSystemMessage($"✅ AI completed step: {selectedStep.Description}", true);
                    return response.Message;
                }
                else
                {
                    // Notify about the failure
                    string errorMsg = $"❌ AI failed to execute step: {response.ErrorMessage}";
                    aiWindow.AddSystemMessage(errorMsg, true);
                    throw new Exception(errorMsg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending AI request: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Generate a plan from natural language instructions using AI
        /// </summary>
        private async void GenerateAIPlan(string name, string description)
        {
            // Create the plan first (empty)
            var plan = PlanningSystem.Instance.CreatePlan(name, description);
            
            // Show a progress dialog
            EditorUtility.DisplayProgressBar("Generating Plan", "Analyzing instructions and creating plan steps...", 0.5f);
            
            try
            {
                // If we have a reference to the AI window, use it to process the plan generation
                if (aiWindow != null)
                {
                    // Format a special prompt for the AI
                    string prompt = BuildPlanGenerationPrompt(description);
                    
                    // Send the prompt to the AI
                    await aiWindow.ProcessPlanGenerationRequest(prompt, plan);
                }
                else
                {
                    // Fallback to a simpler approach if we don't have the AI window reference
                    // Add some default steps based on common project tasks
                    PlanningSystem.Instance.AddStep("Analyze project structure and requirements", PlanStepType.Analysis);
                    PlanningSystem.Instance.AddStep("Create necessary script files", PlanStepType.ScriptCreation);
                    PlanningSystem.Instance.AddStep("Set up scene objects and components", PlanStepType.SceneOperation);
                    PlanningSystem.Instance.AddStep("Test and verify implementation", PlanStepType.Guidance);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating AI plan: {ex.Message}");
                EditorUtility.DisplayDialog("Error Generating Plan", 
                    $"Failed to generate plan: {ex.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        
        /// <summary>
        /// Build a prompt for the AI to generate a plan
        /// </summary>
        private string BuildPlanGenerationPrompt(string description)
        {
            return $@"I need you to create a step-by-step plan for a Unity project task. 
            
Task description: {description}

Please analyze this task and break it down into clear, actionable steps. For each step, specify:
1. A clear description of what needs to be done
2. The type of step (Analysis, ScriptCreation, SceneOperation, or Guidance)
3. Any dependencies on previous steps

Format your response as a JSON array of steps like this:
```json
[
  {{
    ""description"": ""Step description here"",
    ""type"": ""Analysis"" 
  }},
  {{
    ""description"": ""Another step description"",
    ""type"": ""ScriptCreation""
  }}
]
```

Only include the JSON array in your response, no other text.";
        }
    }
} 