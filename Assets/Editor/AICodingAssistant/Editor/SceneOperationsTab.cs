using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AICodingAssistant.Planning;

namespace AICodingAssistant.Editor
{
    /// <summary>
    /// Handles the Scene Operations tab in the AI Coding Assistant Window
    /// </summary>
    public class SceneOperationsTab
    {
        private Vector2 hierarchyScrollPosition;
        private Vector2 detailsScrollPosition;
        private Vector2 operationsScrollPosition;
        private string selectedGameObjectPath;
        private List<string> sceneHierarchy = new List<string>();
        private bool showComponents = false;
        private List<string> componentsList = new List<string>();
        private string selectedComponent;
        private Dictionary<string, string> componentDetails;
        private string newGameObjectName = "NewGameObject";
        private string primitiveTypeSelection = "Cube";
        private string positionX = "0", positionY = "0", positionZ = "0";
        private string rotationX = "0", rotationY = "0", rotationZ = "0";
        private string scaleX = "1", scaleY = "1", scaleZ = "1";
        private string newComponentName = "";
        private string fieldName = "", fieldValue = "";
        private bool isLocalTransform = true;
        private List<string> materials = new List<string>();
        private List<string> prefabs = new List<string>();
        private int selectedMaterialIndex = 0;
        private string selectedMaterialPath = "";
        private string selectedPrefabPath = "";
        private bool needsRefresh = true;
        private string statusMessage = "";
        private bool showStatusMessage = false;
        private double statusMessageTime = 0;
        private GUIStyle statusStyle;
        private bool isStatusError = false;
        private bool hierarchyChanged = false;

        // Primitive type options for dropdown
        private readonly string[] primitiveTypes = new[] 
        { 
            "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" 
        };

        /// <summary>
        /// Constructor to set up the event handlers
        /// </summary>
        public SceneOperationsTab()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        /// <summary>
        /// Callback for when the hierarchy changes
        /// </summary>
        private void OnHierarchyChanged()
        {
            hierarchyChanged = true;
        }

        /// <summary>
        /// Clean up event handlers when this object is destroyed
        /// </summary>
        ~SceneOperationsTab()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        /// <summary>
        /// Draws the Scene Operations tab UI
        /// </summary>
        public void Draw()
        {
            InitializeStyles();
            
            if (needsRefresh)
            {
                RefreshSceneHierarchy();
                needsRefresh = false;
            }

            EditorGUILayout.BeginHorizontal();
            
            // Left panel - Scene Hierarchy
            DrawHierarchyPanel();
            
            // Middle panel - Inspector-like details
            DrawDetailsPanel();
            
            // Right panel - Operations
            DrawOperationsPanel();
            
            EditorGUILayout.EndHorizontal();
            
            // Draw status message if active
            if (showStatusMessage)
            {
                double timeSinceMessage = EditorApplication.timeSinceStartup - statusMessageTime;
                if (timeSinceMessage < 5.0) // Show for 5 seconds
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(statusMessage, statusStyle);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    showStatusMessage = false;
                }
            }
            
            // Auto-refresh when scene changes
            if (Event.current.type == EventType.Layout)
            {
                if (hierarchyChanged)
                {
                    needsRefresh = true;
                    hierarchyChanged = false;
                }
            }
        }

        /// <summary>
        /// Initializes GUI styles
        /// </summary>
        private void InitializeStyles()
        {
            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(EditorStyles.label);
                statusStyle.wordWrap = true;
                statusStyle.richText = true;
            }
        }

        /// <summary>
        /// Refreshes the scene hierarchy data
        /// </summary>
        public void RefreshSceneHierarchy()
        {
            sceneHierarchy = SceneManipulationService.GetSceneHierarchy();
            sceneHierarchy.Sort();
            
            // Refresh asset lists
            var materialsResult = SceneManipulationService.GetAllMaterials();
            materials = materialsResult.Success ? materialsResult.Value : new List<string>();
            
            var prefabsResult = SceneManipulationService.GetAllPrefabs();
            prefabs = prefabsResult.Success ? prefabsResult.Value : new List<string>();
        }

        /// <summary>
        /// Draws the scene hierarchy panel (left side)
        /// </summary>
        private void DrawHierarchyPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250), GUILayout.ExpandHeight(true));
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scene Hierarchy", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                RefreshSceneHierarchy();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            hierarchyScrollPosition = EditorGUILayout.BeginScrollView(hierarchyScrollPosition);
            
            foreach (string path in sceneHierarchy)
            {
                bool isSelected = path == selectedGameObjectPath;
                
                GUIStyle style = new GUIStyle(EditorStyles.foldout);
                if (isSelected)
                {
                    style.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.0f, 0.6f, 1.0f) : new Color(0.0f, 0.3f, 0.8f);
                    style.onNormal.textColor = style.normal.textColor;
                }
                
                // Calculate indentation based on path depth
                int depth = path.Split('/').Length - 1;
                
                // Get the name of the GameObject (last part of the path)
                string name = path.Contains('/') ? path.Substring(path.LastIndexOf('/') + 1) : path;
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(depth * 15); // Indentation
                
                if (GUILayout.Toggle(isSelected, name, style))
                {
                    if (!isSelected)
                    {
                        // Selection changed
                        selectedGameObjectPath = path;
                        showComponents = false;
                        componentsList.Clear();
                        selectedComponent = null;
                        componentDetails = null;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the details panel (middle)
        /// </summary>
        private void DrawDetailsPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300), GUILayout.ExpandHeight(true));
            
            // Header
            EditorGUILayout.LabelField("Object Details", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            if (!string.IsNullOrEmpty(selectedGameObjectPath))
            {
                detailsScrollPosition = EditorGUILayout.BeginScrollView(detailsScrollPosition);
                
                // Show GameObject path
                EditorGUILayout.LabelField("Path:", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(selectedGameObjectPath, EditorStyles.textField, 
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                
                EditorGUILayout.Space();
                
                // Display components
                EditorGUILayout.BeginHorizontal();
                showComponents = EditorGUILayout.Foldout(showComponents, "Components", true);
                
                if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                {
                    var componentsResult = SceneManipulationService.GetComponents(selectedGameObjectPath);
                    if (componentsResult.Success)
                    {
                        componentsList = componentsResult.Value;
                        showComponents = true;
                    }
                    else
                    {
                        componentsList.Clear();
                        ShowStatus(componentsResult.Message, true);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (showComponents)
                {
                    if (componentsList.Count == 0)
                    {
                        var componentsResult = SceneManipulationService.GetComponents(selectedGameObjectPath);
                        if (componentsResult.Success)
                        {
                            componentsList = componentsResult.Value;
                        }
                    }
                    
                    EditorGUI.indentLevel++;
                    
                    foreach (string componentName in componentsList)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        bool isComponentSelected = componentName == selectedComponent;
                        
                        if (GUILayout.Toggle(isComponentSelected, componentName, EditorStyles.foldout))
                        {
                            if (!isComponentSelected)
                            {
                                selectedComponent = componentName;
                                var detailsResult = SceneManipulationService.GetComponentDetails(
                                    selectedGameObjectPath, componentName);
                                    
                                if (detailsResult.Success)
                                {
                                    componentDetails = detailsResult.Value;
                                }
                                else
                                {
                                    componentDetails = null;
                                    ShowStatus(detailsResult.Message, true);
                                }
                            }
                        }
                        else if (isComponentSelected)
                        {
                            selectedComponent = null;
                            componentDetails = null;
                        }
                        
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            if (EditorUtility.DisplayDialog("Remove Component", 
                                $"Are you sure you want to remove the {componentName} component?", 
                                "Yes", "No"))
                            {
                                var removeResult = SceneManipulationService.RemoveComponent(
                                    selectedGameObjectPath, componentName);
                                    
                                ShowStatus(removeResult.Message, !removeResult.Success);
                                
                                if (removeResult.Success)
                                {
                                    // Refresh component list
                                    var componentsResult = SceneManipulationService.GetComponents(selectedGameObjectPath);
                                    if (componentsResult.Success)
                                    {
                                        componentsList = componentsResult.Value;
                                    }
                                    
                                    // Clear selection if the removed component was selected
                                    if (componentName == selectedComponent)
                                    {
                                        selectedComponent = null;
                                        componentDetails = null;
                                    }
                                }
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // Show component details if selected
                        if (componentName == selectedComponent && componentDetails != null)
                        {
                            EditorGUI.indentLevel++;
                            
                            foreach (var property in componentDetails)
                            {
                                EditorGUILayout.BeginHorizontal();
                                
                                EditorGUILayout.LabelField(property.Key, GUILayout.Width(150));
                                EditorGUILayout.SelectableLabel(property.Value, EditorStyles.textField, 
                                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                                
                                EditorGUILayout.EndHorizontal();
                            }
                            
                            EditorGUI.indentLevel--;
                            
                            EditorGUILayout.Space();
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a GameObject from the hierarchy", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the operations panel (right side)
        /// </summary>
        private void DrawOperationsPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            EditorGUILayout.LabelField("Scene Operations", EditorStyles.boldLabel);
            
            operationsScrollPosition = EditorGUILayout.BeginScrollView(operationsScrollPosition);
            
            // Create GameObject section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Create GameObjects", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // Create empty GameObject
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
            newGameObjectName = EditorGUILayout.TextField(newGameObjectName);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Empty"))
            {
                var result = SceneManipulationService.CreateEmptyGameObject(
                    newGameObjectName, selectedGameObjectPath);
                    
                ShowStatus(result.Message, !result.Success);
                
                if (result.Success)
                {
                    selectedGameObjectPath = result.Value;
                    needsRefresh = true;
                }
            }
            
            EditorGUILayout.Space();
            
            primitiveTypeSelection = EditorGUILayout.Popup(
                Array.IndexOf(primitiveTypes, primitiveTypeSelection), 
                primitiveTypes).ToString();
                
            if (int.TryParse(primitiveTypeSelection, out int index) && index >= 0 && index < primitiveTypes.Length)
            {
                primitiveTypeSelection = primitiveTypes[index];
            }
            
            if (GUILayout.Button("Create Primitive"))
            {
                PrimitiveType primitiveType = (PrimitiveType)Enum.Parse(
                    typeof(PrimitiveType), primitiveTypeSelection);
                    
                var result = SceneManipulationService.CreatePrimitiveGameObject(
                    primitiveType, newGameObjectName, selectedGameObjectPath);
                    
                ShowStatus(result.Message, !result.Success);
                
                if (result.Success)
                {
                    selectedGameObjectPath = result.Value;
                    needsRefresh = true;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Instantiate prefab section
            if (prefabs.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Instantiate Prefab", EditorStyles.miniBoldLabel);
                
                var prefabNames = prefabs.Select(p => p.Substring(p.LastIndexOf('/') + 1)).ToArray();
                int prefabIndex = prefabs.IndexOf(selectedPrefabPath);
                prefabIndex = prefabIndex < 0 ? 0 : prefabIndex;
                
                int newPrefabIndex = EditorGUILayout.Popup("Prefab:", prefabIndex, prefabNames);
                if (newPrefabIndex != prefabIndex && newPrefabIndex >= 0 && newPrefabIndex < prefabs.Count)
                {
                    selectedPrefabPath = prefabs[newPrefabIndex];
                }
                
                if (GUILayout.Button("Instantiate Prefab"))
                {
                    if (!string.IsNullOrEmpty(selectedPrefabPath))
                    {
                        var result = SceneManipulationService.InstantiatePrefab(
                            selectedPrefabPath, selectedGameObjectPath);
                            
                        ShowStatus(result.Message, !result.Success);
                        
                        if (result.Success)
                        {
                            selectedGameObjectPath = result.Value;
                            needsRefresh = true;
                        }
                    }
                    else
                    {
                        ShowStatus("No prefab selected", true);
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            
            // Only show these operations if a GameObject is selected
            if (!string.IsNullOrEmpty(selectedGameObjectPath))
            {
                // Transform operations
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Transform Operations", EditorStyles.boldLabel);
                
                isLocalTransform = EditorGUILayout.Toggle("Local Transform", isLocalTransform);
                
                EditorGUILayout.Space();
                
                // Position
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Position:", GUILayout.Width(60));
                
                EditorGUILayout.LabelField("X:", GUILayout.Width(15));
                positionX = EditorGUILayout.TextField(positionX, GUILayout.Width(50));
                
                EditorGUILayout.LabelField("Y:", GUILayout.Width(15));
                positionY = EditorGUILayout.TextField(positionY, GUILayout.Width(50));
                
                EditorGUILayout.LabelField("Z:", GUILayout.Width(15));
                positionZ = EditorGUILayout.TextField(positionZ, GUILayout.Width(50));
                
                if (GUILayout.Button("Set", GUILayout.Width(50)))
                {
                    if (float.TryParse(positionX, out float x) && 
                        float.TryParse(positionY, out float y) && 
                        float.TryParse(positionZ, out float z))
                    {
                        var result = SceneManipulationService.SetPosition(
                            selectedGameObjectPath, x, y, z, isLocalTransform);
                            
                        ShowStatus(result.Message, !result.Success);
                    }
                    else
                    {
                        ShowStatus("Invalid position values", true);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Rotation
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Rotation:", GUILayout.Width(60));
                
                EditorGUILayout.LabelField("X:", GUILayout.Width(15));
                rotationX = EditorGUILayout.TextField(rotationX, GUILayout.Width(50));
                
                EditorGUILayout.LabelField("Y:", GUILayout.Width(15));
                rotationY = EditorGUILayout.TextField(rotationY, GUILayout.Width(50));
                
                EditorGUILayout.LabelField("Z:", GUILayout.Width(15));
                rotationZ = EditorGUILayout.TextField(rotationZ, GUILayout.Width(50));
                
                if (GUILayout.Button("Set", GUILayout.Width(50)))
                {
                    if (float.TryParse(rotationX, out float x) && 
                        float.TryParse(rotationY, out float y) && 
                        float.TryParse(rotationZ, out float z))
                    {
                        var result = SceneManipulationService.SetRotation(
                            selectedGameObjectPath, x, y, z, isLocalTransform);
                            
                        ShowStatus(result.Message, !result.Success);
                    }
                    else
                    {
                        ShowStatus("Invalid rotation values", true);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Scale
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Scale:", GUILayout.Width(60));
                
                EditorGUILayout.LabelField("X:", GUILayout.Width(15));
                scaleX = EditorGUILayout.TextField(scaleX, GUILayout.Width(50));
                
                EditorGUILayout.LabelField("Y:", GUILayout.Width(15));
                scaleY = EditorGUILayout.TextField(scaleY, GUILayout.Width(50));
                
                EditorGUILayout.LabelField("Z:", GUILayout.Width(15));
                scaleZ = EditorGUILayout.TextField(scaleZ, GUILayout.Width(50));
                
                if (GUILayout.Button("Set", GUILayout.Width(50)))
                {
                    if (float.TryParse(scaleX, out float x) && 
                        float.TryParse(scaleY, out float y) && 
                        float.TryParse(scaleZ, out float z))
                    {
                        var result = SceneManipulationService.SetScale(
                            selectedGameObjectPath, x, y, z);
                            
                        ShowStatus(result.Message, !result.Success);
                    }
                    else
                    {
                        ShowStatus("Invalid scale values", true);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                
                // Component operations
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Component Operations", EditorStyles.boldLabel);
                
                EditorGUILayout.Space();
                
                // Add component
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Add Component:", GUILayout.Width(100));
                newComponentName = EditorGUILayout.TextField(newComponentName);
                
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    if (!string.IsNullOrEmpty(newComponentName))
                    {
                        var result = SceneManipulationService.AddComponent(
                            selectedGameObjectPath, newComponentName);
                            
                        ShowStatus(result.Message, !result.Success);
                        
                        if (result.Success)
                        {
                            // Refresh components list
                            var componentsResult = SceneManipulationService.GetComponents(selectedGameObjectPath);
                            if (componentsResult.Success)
                            {
                                componentsList = componentsResult.Value;
                                showComponents = true;
                            }
                        }
                    }
                    else
                    {
                        ShowStatus("Component name is required", true);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Edit component field (only if a component is selected)
                if (!string.IsNullOrEmpty(selectedComponent))
                {
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.LabelField("Edit Component Field:", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Field Name:", GUILayout.Width(80));
                    fieldName = EditorGUILayout.TextField(fieldName);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Field Value:", GUILayout.Width(80));
                    fieldValue = EditorGUILayout.TextField(fieldValue);
                    EditorGUILayout.EndHorizontal();
                    
                    if (GUILayout.Button("Set Field Value"))
                    {
                        if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(fieldValue))
                        {
                            var result = SceneManipulationService.SetComponentField(
                                selectedGameObjectPath, selectedComponent, fieldName, fieldValue);
                                
                            ShowStatus(result.Message, !result.Success);
                            
                            if (result.Success)
                            {
                                // Refresh component details
                                var detailsResult = SceneManipulationService.GetComponentDetails(
                                    selectedGameObjectPath, selectedComponent);
                                    
                                if (detailsResult.Success)
                                {
                                    componentDetails = detailsResult.Value;
                                }
                            }
                        }
                        else
                        {
                            ShowStatus("Field name and value are required", true);
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
                
                // Material operations
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Material Operations", EditorStyles.boldLabel);
                
                EditorGUILayout.Space();
                
                if (materials.Count > 0)
                {
                    var materialNames = materials.Select(m => m.Substring(m.LastIndexOf('/') + 1)).ToArray();
                    int materialIndex = materials.IndexOf(selectedMaterialPath);
                    materialIndex = materialIndex < 0 ? 0 : materialIndex;
                    
                    int newMaterialIndex = EditorGUILayout.Popup("Material:", materialIndex, materialNames);
                    if (newMaterialIndex != materialIndex && newMaterialIndex >= 0 && newMaterialIndex < materials.Count)
                    {
                        selectedMaterialPath = materials[newMaterialIndex];
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Material Index:", GUILayout.Width(100));
                    string materialIndexStr = EditorGUILayout.TextField(selectedMaterialIndex.ToString(), GUILayout.Width(50));
                    
                    if (int.TryParse(materialIndexStr, out int parsedIndex) && parsedIndex >= 0)
                    {
                        selectedMaterialIndex = parsedIndex;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    if (GUILayout.Button("Apply Material"))
                    {
                        if (!string.IsNullOrEmpty(selectedMaterialPath))
                        {
                            var result = SceneManipulationService.SetMaterial(
                                selectedGameObjectPath, selectedMaterialPath, selectedMaterialIndex);
                                
                            ShowStatus(result.Message, !result.Success);
                        }
                        else
                        {
                            ShowStatus("No material selected", true);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No materials found in the project", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
                
                // Delete GameObject operation
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Delete GameObject", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Delete GameObject", 
                        $"Are you sure you want to delete the GameObject at '{selectedGameObjectPath}'?", 
                        "Yes", "No"))
                    {
                        var result = SceneManipulationService.DeleteGameObject(selectedGameObjectPath);
                        
                        ShowStatus(result.Message, !result.Success);
                        
                        if (result.Success)
                        {
                            selectedGameObjectPath = null;
                            showComponents = false;
                            componentsList.Clear();
                            selectedComponent = null;
                            componentDetails = null;
                            needsRefresh = true;
                        }
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Shows a status message
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="isError">Whether the message represents an error</param>
        private void ShowStatus(string message, bool isError)
        {
            statusMessage = isError ? $"<color=red>{message}</color>" : $"<color=green>{message}</color>";
            showStatusMessage = true;
            statusMessageTime = EditorApplication.timeSinceStartup;
            isStatusError = isError;
        }
    }
} 