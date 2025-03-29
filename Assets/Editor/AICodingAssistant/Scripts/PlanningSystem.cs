using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Represents a step within a plan
    /// </summary>
    public enum PlanStepType
    {
        Analysis,
        ScriptCreation,
        SceneOperation,
        Guidance
    }
    
    /// <summary>
    /// Represents the status of a plan step
    /// </summary>
    public enum PlanStepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
    
    /// <summary>
    /// Represents a step in a plan
    /// </summary>
    public class PlanStep
    {
        /// <summary>
        /// Unique identifier for the step
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Description of the step
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Type of the step
        /// </summary>
        public PlanStepType StepType { get; set; }
        
        /// <summary>
        /// Status of the step
        /// </summary>
        public PlanStepStatus Status { get; set; }
        
        /// <summary>
        /// Result or output from the step execution
        /// </summary>
        public string Result { get; set; }
        
        /// <summary>
        /// Additional data specific to the step type
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }
        
        /// <summary>
        /// Constructor for a plan step
        /// </summary>
        /// <param name="description">Description of what the step does</param>
        /// <param name="type">Type of the step</param>
        public PlanStep(string description, PlanStepType type)
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8);
            Description = description;
            StepType = type;
            Status = PlanStepStatus.Pending;
            Result = "";
            Metadata = new Dictionary<string, string>();
        }
    }
    
    /// <summary>
    /// Represents a plan that contains steps
    /// </summary>
    public class Plan
    {
        /// <summary>
        /// Name of the plan
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the plan
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// List of steps in the plan
        /// </summary>
        public List<PlanStep> Steps { get; private set; }
        
        /// <summary>
        /// Constructor for a plan
        /// </summary>
        /// <param name="name">Name of the plan</param>
        /// <param name="description">Description of what the plan achieves</param>
        public Plan(string name, string description)
        {
            Name = name;
            Description = description;
            Steps = new List<PlanStep>();
        }
        
        /// <summary>
        /// Add a step to the plan
        /// </summary>
        /// <param name="step">The step to add</param>
        public void AddStep(PlanStep step)
        {
            Steps.Add(step);
        }
        
        /// <summary>
        /// Get the next pending step in the plan
        /// </summary>
        /// <returns>The next pending step, or null if no pending steps exist</returns>
        public PlanStep GetNextPendingStep()
        {
            return Steps.FirstOrDefault(s => s.Status == PlanStepStatus.Pending);
        }
    }
    
    /// <summary>
    /// System for planning and executing AI-assisted tasks
    /// </summary>
    public class PlanningSystem
    {
        private static PlanningSystem _instance;
        
        /// <summary>
        /// Singleton instance of the planning system
        /// </summary>
        public static PlanningSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PlanningSystem();
                }
                
                return _instance;
            }
        }
        
        /// <summary>
        /// Current active plan
        /// </summary>
        public Plan CurrentPlan { get; private set; }
        
        /// <summary>
        /// Flag indicating if the system is currently executing a plan
        /// </summary>
        public bool IsExecuting { get; private set; }
        
        /// <summary>
        /// Event raised when a plan is created
        /// </summary>
        public event Action<Plan> OnPlanCreated;
        
        /// <summary>
        /// Event raised when a plan step is completed
        /// </summary>
        public event Action<PlanStep> OnStepCompleted;
        
        /// <summary>
        /// Event raised when a plan step fails
        /// </summary>
        public event Action<PlanStep, string> OnStepFailed;
        
        /// <summary>
        /// Create a new plan
        /// </summary>
        /// <param name="name">Name of the plan</param>
        /// <param name="description">Description of what the plan should achieve</param>
        /// <returns>The created plan</returns>
        public Plan CreatePlan(string name, string description)
        {
            // Create new plan
            CurrentPlan = new Plan(name, description);
            
            // Trigger event
            OnPlanCreated?.Invoke(CurrentPlan);
            
            Debug.Log($"Plan created: {name}");
            
            return CurrentPlan;
        }
        
        /// <summary>
        /// Add a step to the current plan
        /// </summary>
        /// <param name="description">Description of the step</param>
        /// <param name="stepType">Type of the step</param>
        /// <returns>The created step</returns>
        public PlanStep AddStep(string description, PlanStepType stepType)
        {
            if (CurrentPlan == null)
            {
                Debug.LogError("Cannot add step: No active plan");
                return null;
            }
            
            PlanStep step = new PlanStep(description, stepType);
            CurrentPlan.AddStep(step);
            
            Debug.Log($"Added step to plan: {description}");
            
            return step;
        }
        
        /// <summary>
        /// Start executing the current plan
        /// </summary>
        public void ExecutePlan()
        {
            if (CurrentPlan == null)
            {
                Debug.LogError("Cannot execute plan: No active plan");
                return;
            }
            
            if (IsExecuting)
            {
                Debug.LogWarning("Plan execution is already in progress");
                return;
            }
            
            IsExecuting = true;
            
            // Start with the first pending step
            ExecuteNextPendingStep();
        }
        
        /// <summary>
        /// Pause the execution of the current plan
        /// </summary>
        public void PauseExecution()
        {
            if (!IsExecuting)
            {
                Debug.LogWarning("No plan execution is in progress");
                return;
            }
            
            IsExecuting = false;
            Debug.Log("Plan execution paused");
        }
        
        /// <summary>
        /// Reset the current plan
        /// </summary>
        public void ResetPlan()
        {
            if (CurrentPlan == null)
            {
                Debug.LogWarning("No active plan to reset");
                return;
            }
            
            // Reset all steps to pending
            foreach (var step in CurrentPlan.Steps)
            {
                step.Status = PlanStepStatus.Pending;
                step.Result = "";
            }
            
            IsExecuting = false;
            Debug.Log("Plan has been reset");
        }
        
        /// <summary>
        /// Execute a specific step
        /// </summary>
        /// <param name="step">The step to execute</param>
        public void ExecuteStep(PlanStep step)
        {
            if (step == null)
            {
                Debug.LogError("Cannot execute step: Step is null");
                return;
            }
            
            // Check if the step is already completed or failed
            if (step.Status == PlanStepStatus.Completed || step.Status == PlanStepStatus.Failed)
            {
                Debug.LogWarning($"Step is already in state {step.Status}");
                return;
            }
            
            Debug.Log($"Executing step: {step.Description}");
            
            // Mark step as in progress
            step.Status = PlanStepStatus.InProgress;
            
            // Execute step based on type
            try
            {
                switch (step.StepType)
                {
                    case PlanStepType.Analysis:
                        ExecuteAnalysisStep(step);
                        break;
                        
                    case PlanStepType.ScriptCreation:
                        ExecuteScriptCreationStep(step);
                        break;
                        
                    case PlanStepType.SceneOperation:
                        ExecuteSceneOperationStep(step);
                        break;
                        
                    case PlanStepType.Guidance:
                        // Guidance steps are just informational
                        MarkStepCompleted(step);
                        break;
                        
                    default:
                        throw new NotImplementedException($"Step type {step.StepType} is not implemented");
                }
            }
            catch (Exception ex)
            {
                MarkStepFailed(step, $"Error executing step: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Execute the next pending step in the plan
        /// </summary>
        private void ExecuteNextPendingStep()
        {
            if (CurrentPlan == null || !IsExecuting)
            {
                return;
            }
            
            PlanStep nextStep = CurrentPlan.GetNextPendingStep();
            
            if (nextStep != null)
            {
                ExecuteStep(nextStep);
            }
            else
            {
                Debug.Log("No more pending steps to execute");
                IsExecuting = false;
            }
        }
        
        /// <summary>
        /// Mark a step as completed
        /// </summary>
        /// <param name="step">The step to mark as completed</param>
        public void MarkStepCompleted(PlanStep step)
        {
            if (step == null)
            {
                Debug.LogError("Cannot mark step as completed: Step is null");
                return;
            }
            
            step.Status = PlanStepStatus.Completed;
            
            // Trigger event
            OnStepCompleted?.Invoke(step);
            
            Debug.Log($"Step completed: {step.Description}");
            
            // If we're executing a plan, continue to the next step
            if (IsExecuting)
            {
                ExecuteNextPendingStep();
            }
        }
        
        /// <summary>
        /// Mark a step as failed
        /// </summary>
        /// <param name="step">The step to mark as failed</param>
        /// <param name="error">The error message</param>
        public void MarkStepFailed(PlanStep step, string error)
        {
            if (step == null)
            {
                Debug.LogError("Cannot mark step as failed: Step is null");
                return;
            }
            
            step.Status = PlanStepStatus.Failed;
            step.Result = error;
            
            // Trigger event
            OnStepFailed?.Invoke(step, error);
            
            Debug.LogError($"Step failed: {step.Description}. Error: {error}");
            
            // Pause execution on failure
            IsExecuting = false;
        }
        
        #region Step Execution Implementations
        
        /// <summary>
        /// Execute an analysis step
        /// </summary>
        /// <param name="step">The step to execute</param>
        private void ExecuteAnalysisStep(PlanStep step)
        {
            // For analysis steps, we would typically:
            // 1. Look at project structure
            // 2. Identify relevant scripts
            // 3. Provide recommendations
            
            // This is a placeholder implementation
            step.Result = "Analysis completed: " + GetProjectStructure();
            
            // Mark step as completed
            MarkStepCompleted(step);
        }
        
        /// <summary>
        /// Execute a script creation step
        /// </summary>
        /// <param name="step">The step to execute</param>
        private void ExecuteScriptCreationStep(PlanStep step)
        {
            // For script creation steps, we would typically:
            // 1. Determine the script content
            // 2. Decide where to create the script
            // 3. Create the script file
            
            // This is a placeholder implementation
            step.Result = "Script creation would happen here";
            
            // Mark step as completed
            MarkStepCompleted(step);
        }
        
        /// <summary>
        /// Execute a scene operation step
        /// </summary>
        /// <param name="step">The step to execute</param>
        private void ExecuteSceneOperationStep(PlanStep step)
        {
            // For scene operation steps, we would typically:
            // 1. Determine what scene changes are needed
            // 2. Create, modify, or delete game objects
            // 3. Configure components
            
            // This is a placeholder implementation
            step.Result = "Scene operation would happen here";
            
            // Mark step as completed
            MarkStepCompleted(step);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get a string representation of the project structure
        /// </summary>
        /// <returns>A string containing a tree representation of the project</returns>
        public string GetProjectStructure()
        {
            string projectRoot = Application.dataPath;
            string projectStructure = "Project Structure:\n";
            
            try
            {
                projectStructure += GetDirectoryTree(projectRoot, 0);
            }
            catch (Exception ex)
            {
                projectStructure += $"Error getting project structure: {ex.Message}";
            }
            
            return projectStructure;
        }
        
        /// <summary>
        /// Get a tree representation of a directory
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <param name="depth">Current depth in the tree</param>
        /// <returns>A string containing a tree representation of the directory</returns>
        private string GetDirectoryTree(string directoryPath, int depth)
        {
            if (depth > 3) // Limit recursion depth to prevent too much output
            {
                return "    " + new string(' ', depth * 2) + "...\n";
            }
            
            string result = "";
            string indent = new string(' ', depth * 2);
            
            // Get directory name
            string directoryName = new DirectoryInfo(directoryPath).Name;
            result += indent + "📁 " + directoryName + "\n";
            
            try
            {
                // Add subdirectories
                foreach (string subdirectory in Directory.GetDirectories(directoryPath))
                {
                    // Skip some common directories that aren't relevant to code structure
                    if (Path.GetFileName(subdirectory) == "Library" || 
                        Path.GetFileName(subdirectory) == "Temp" ||
                        Path.GetFileName(subdirectory) == "obj")
                    {
                        continue;
                    }
                    
                    result += GetDirectoryTree(subdirectory, depth + 1);
                }
                
                // Add files (just count them to avoid too much output)
                var files = Directory.GetFiles(directoryPath);
                if (files.Length > 0)
                {
                    int scriptCount = files.Count(f => Path.GetExtension(f) == ".cs");
                    int sceneCount = files.Count(f => Path.GetExtension(f) == ".unity");
                    int prefabCount = files.Count(f => Path.GetExtension(f) == ".prefab");
                    int otherCount = files.Length - scriptCount - sceneCount - prefabCount;
                    
                    if (scriptCount > 0)
                    {
                        result += indent + "  " + "📄 " + scriptCount + " C# scripts\n";
                    }
                    
                    if (sceneCount > 0)
                    {
                        result += indent + "  " + "🎬 " + sceneCount + " scenes\n";
                    }
                    
                    if (prefabCount > 0)
                    {
                        result += indent + "  " + "🧩 " + prefabCount + " prefabs\n";
                    }
                    
                    if (otherCount > 0)
                    {
                        result += indent + "  " + "📄 " + otherCount + " other files\n";
                    }
                }
            }
            catch (Exception ex)
            {
                result += indent + "  " + "Error: " + ex.Message + "\n";
            }
            
            return result;
        }
        
        /// <summary>
        /// Verify if a file exists at the specified path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if the file exists, false otherwise</returns>
        public bool VerifyFileExists(string path)
        {
            return File.Exists(path);
        }
        
        /// <summary>
        /// Verify if a directory exists at the specified path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if the directory exists, false otherwise</returns>
        public bool VerifyDirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
        
        /// <summary>
        /// Verify if a game object exists in the scene with the specified name
        /// </summary>
        /// <param name="name">Name of the game object</param>
        /// <returns>True if the game object exists, false otherwise</returns>
        public bool VerifyGameObjectExists(string name)
        {
            return UnityEngine.GameObject.Find(name) != null;
        }
        
        #endregion
    }
}