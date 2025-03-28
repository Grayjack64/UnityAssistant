using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Service class that provides scene manipulation operations for the AI Coding Assistant.
    /// Acts as an intermediary between the AI and the low-level scene manipulation utilities.
    /// </summary>
    public static class SceneManipulationService
    {
        #region Scene Information

        /// <summary>
        /// Gets information about the current scene
        /// </summary>
        /// <returns>Dictionary with scene information</returns>
        public static Dictionary<string, string> GetSceneInfo()
        {
            Dictionary<string, string> sceneInfo = new Dictionary<string, string>
            {
                { "SceneName", SceneManipulationUtility.GetActiveSceneName() },
                { "ScenePath", SceneManipulationUtility.GetActiveScenePath() },
                { "GameObjectCount", UnityEngine.Object.FindObjectsOfType<GameObject>().Length.ToString() }
            };
            
            return sceneInfo;
        }

        /// <summary>
        /// Gets the hierarchy of GameObjects in the current scene
        /// </summary>
        /// <returns>List of GameObject paths representing the scene hierarchy</returns>
        public static List<string> GetSceneHierarchy()
        {
            return SceneManipulationUtility.GetAllGameObjectsInScene();
        }

        #endregion

        #region GameObject Operations

        /// <summary>
        /// Creates a new empty GameObject in the scene
        /// </summary>
        /// <param name="name">Name for the new GameObject</param>
        /// <param name="parentPath">Optional path to parent GameObject</param>
        /// <returns>Result with success status and path to the created GameObject</returns>
        public static OperationResult<string> CreateEmptyGameObject(string name, string parentPath = null)
        {
            try
            {
                string path = SceneManipulationUtility.CreateGameObject(name, parentPath);
                if (string.IsNullOrEmpty(path))
                {
                    return OperationResult<string>.CreateFailure($"Failed to create GameObject '{name}'");
                }
                
                return OperationResult<string>.CreateSuccess(path, $"Created GameObject '{name}' at '{path}'");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.CreateFailure($"Error creating GameObject: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a primitive GameObject (cube, sphere, etc.)
        /// </summary>
        /// <param name="primitiveType">Type of primitive to create</param>
        /// <param name="name">Name for the new GameObject</param>
        /// <param name="parentPath">Optional path to parent GameObject</param>
        /// <returns>Result with success status and path to the created GameObject</returns>
        public static OperationResult<string> CreatePrimitiveGameObject(PrimitiveType primitiveType, string name, string parentPath = null)
        {
            try
            {
                string path = SceneManipulationUtility.CreatePrimitive(primitiveType, name, parentPath);
                if (string.IsNullOrEmpty(path))
                {
                    return OperationResult<string>.CreateFailure($"Failed to create primitive '{name}'");
                }
                
                return OperationResult<string>.CreateSuccess(path, $"Created {primitiveType} '{name}' at '{path}'");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.CreateFailure($"Error creating primitive: {ex.Message}");
            }
        }

        /// <summary>
        /// Renames a GameObject in the scene
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="newName">New name for the GameObject</param>
        /// <returns>Result with success status and updated path</returns>
        public static OperationResult<string> RenameGameObject(string path, string newName)
        {
            try
            {
                string newPath = SceneManipulationUtility.RenameGameObject(path, newName);
                if (string.IsNullOrEmpty(newPath))
                {
                    return OperationResult<string>.CreateFailure($"Failed to rename GameObject at '{path}'");
                }
                
                return OperationResult<string>.CreateSuccess(newPath, $"Renamed GameObject from '{path}' to '{newPath}'");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.CreateFailure($"Error renaming GameObject: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a GameObject from the scene
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <returns>Result with success status</returns>
        public static OperationResult<bool> DeleteGameObject(string path)
        {
            try
            {
                bool success = SceneManipulationUtility.DeleteGameObject(path);
                if (!success)
                {
                    return OperationResult<bool>.CreateFailure($"Failed to delete GameObject at '{path}'");
                }
                
                return OperationResult<bool>.CreateSuccess(true, $"Deleted GameObject at '{path}'");
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.CreateFailure($"Error deleting GameObject: {ex.Message}");
            }
        }

        /// <summary>
        /// Instantiates a prefab in the scene
        /// </summary>
        /// <param name="prefabPath">Path to the prefab asset</param>
        /// <param name="parentPath">Optional path to parent GameObject</param>
        /// <returns>Result with success status and path to the instantiated GameObject</returns>
        public static OperationResult<string> InstantiatePrefab(string prefabPath, string parentPath = null)
        {
            try
            {
                string path = SceneManipulationUtility.InstantiatePrefab(prefabPath, parentPath);
                if (string.IsNullOrEmpty(path))
                {
                    return OperationResult<string>.CreateFailure($"Failed to instantiate prefab from '{prefabPath}'");
                }
                
                return OperationResult<string>.CreateSuccess(path, $"Instantiated prefab from '{prefabPath}' at '{path}'");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.CreateFailure($"Error instantiating prefab: {ex.Message}");
            }
        }

        #endregion

        #region Transform Operations

        /// <summary>
        /// Sets the position of a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="z">Z position</param>
        /// <param name="isLocal">Whether to set local or world position</param>
        /// <returns>Result with success status</returns>
        public static OperationResult<bool> SetPosition(string path, float x, float y, float z, bool isLocal = false)
        {
            try
            {
                bool success = SceneManipulationUtility.SetPosition(path, x, y, z, isLocal);
                if (!success)
                {
                    return OperationResult<bool>.CreateFailure($"Failed to set position of GameObject at '{path}'");
                }
                
                string posType = isLocal ? "local" : "world";
                return OperationResult<bool>.CreateSuccess(true, 
                    $"Set {posType} position of '{path}' to ({x}, {y}, {z})");
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.CreateFailure($"Error setting position: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the rotation of a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="x">X rotation (in degrees)</param>
        /// <param name="y">Y rotation (in degrees)</param>
        /// <param name="z">Z rotation (in degrees)</param>
        /// <param name="isLocal">Whether to set local or world rotation</param>
        /// <returns>Result with success status</returns>
        public static OperationResult<bool> SetRotation(string path, float x, float y, float z, bool isLocal = false)
        {
            try
            {
                bool success = SceneManipulationUtility.SetRotation(path, x, y, z, isLocal);
                if (!success)
                {
                    return OperationResult<bool>.CreateFailure($"Failed to set rotation of GameObject at '{path}'");
                }
                
                string rotType = isLocal ? "local" : "world";
                return OperationResult<bool>.CreateSuccess(true, 
                    $"Set {rotType} rotation of '{path}' to ({x}, {y}, {z})");
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.CreateFailure($"Error setting rotation: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the scale of a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="x">X scale</param>
        /// <param name="y">Y scale</param>
        /// <param name="z">Z scale</param>
        /// <returns>Result with success status</returns>
        public static OperationResult<bool> SetScale(string path, float x, float y, float z)
        {
            try
            {
                bool success = SceneManipulationUtility.SetScale(path, x, y, z);
                if (!success)
                {
                    return OperationResult<bool>.CreateFailure($"Failed to set scale of GameObject at '{path}'");
                }
                
                return OperationResult<bool>.CreateSuccess(true, 
                    $"Set scale of '{path}' to ({x}, {y}, {z})");
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.CreateFailure($"Error setting scale: {ex.Message}");
            }
        }

        #endregion

        #region Component Operations

        /// <summary>
        /// Gets all components on a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <returns>Result with success status and list of component names</returns>
        public static OperationResult<List<string>> GetComponents(string path)
        {
            try
            {
                List<string> components = SceneManipulationUtility.GetComponents(path);
                if (components == null)
                {
                    return OperationResult<List<string>>.CreateFailure($"Failed to get components of GameObject at '{path}'");
                }
                
                return OperationResult<List<string>>.CreateSuccess(components, 
                    $"Found {components.Count} components on '{path}'");
            }
            catch (Exception ex)
            {
                return OperationResult<List<string>>.CreateFailure($"Error getting components: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets details of a specific component on a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="componentName">Name of the component</param>
        /// <returns>Result with success status and component details</returns>
        public static OperationResult<Dictionary<string, string>> GetComponentDetails(string path, string componentName)
        {
            try
            {
                Dictionary<string, string> details = SceneManipulationUtility.GetComponentDetails(path, componentName);
                if (details == null)
                {
                    return OperationResult<Dictionary<string, string>>.CreateFailure(
                        $"Failed to get details of component '{componentName}' on GameObject at '{path}'");
                }
                
                return OperationResult<Dictionary<string, string>>.CreateSuccess(details, 
                    $"Retrieved {details.Count} properties/fields from '{componentName}' on '{path}'");
            }
            catch (Exception ex)
            {
                return OperationResult<Dictionary<string, string>>.CreateFailure($"Error getting component details: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a component to a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="componentName">Name of the component to add</param>
        /// <returns>Result with success status</returns>
        public static OperationResult<bool> AddComponent(string path, string componentName)
        {
            try
            {
                bool success = SceneManipulationUtility.AddComponent(path, componentName);
                if (!success)
                {
                    return OperationResult<bool>.CreateFailure(
                        $"Failed to add component '{componentName}' to GameObject at '{path}'");
                }
                
                return OperationResult<bool>.CreateSuccess(true, 
                    $"Added component '{componentName}' to '{path}'");
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.CreateFailure($"Error adding component: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes a component from a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="componentName">Name of the component to remove</param>
        /// <returns>Result with success status</returns>
        public static OperationResult<bool> RemoveComponent(string path, string componentName)
        {
            try
            {
                bool success = SceneManipulationUtility.RemoveComponent(path, componentName);
                if (!success)
                {
                    return OperationResult<bool>.CreateFailure(
                        $"Failed to remove component '{componentName}' from GameObject at '{path}'");
                }
                
                return OperationResult<bool>.CreateSuccess(true, 
                    $"Removed component '{componentName}' from '{path}'");
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.CreateFailure($"Error removing component: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a field or property value on a component
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="componentName">Name of the component</param>
        /// <param name="fieldName">Name of the field/property to set</param>
        /// <param name="value">Value to set (as string)</param>
        /// <returns>Result with success status</returns>
        public static OperationResult<bool> SetComponentField(string path, string componentName, string fieldName, string value)
        {
            try
            {
                bool success = SceneManipulationUtility.SetComponentField(path, componentName, fieldName, value);
                if (!success)
                {
                    return OperationResult<bool>.CreateFailure(
                        $"Failed to set field '{fieldName}' on component '{componentName}' of GameObject at '{path}'");
                }
                
                return OperationResult<bool>.CreateSuccess(true, 
                    $"Set '{fieldName}' to '{value}' on component '{componentName}' of '{path}'");
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.CreateFailure($"Error setting component field: {ex.Message}");
            }
        }

        #endregion

        #region Asset Operations

        /// <summary>
        /// Gets all materials in the project
        /// </summary>
        /// <returns>Result with success status and list of material paths</returns>
        public static OperationResult<List<string>> GetAllMaterials()
        {
            try
            {
                List<string> materials = SceneManipulationUtility.GetAllMaterials();
                if (materials == null)
                {
                    return OperationResult<List<string>>.CreateFailure("Failed to get materials from project");
                }
                
                return OperationResult<List<string>>.CreateSuccess(materials, 
                    $"Found {materials.Count} materials in the project");
            }
            catch (Exception ex)
            {
                return OperationResult<List<string>>.CreateFailure($"Error getting materials: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all textures in the project
        /// </summary>
        /// <returns>Result with success status and list of texture paths</returns>
        public static OperationResult<List<string>> GetAllTextures()
        {
            try
            {
                List<string> textures = SceneManipulationUtility.GetAllTextures();
                if (textures == null)
                {
                    return OperationResult<List<string>>.CreateFailure("Failed to get textures from project");
                }
                
                return OperationResult<List<string>>.CreateSuccess(textures, 
                    $"Found {textures.Count} textures in the project");
            }
            catch (Exception ex)
            {
                return OperationResult<List<string>>.CreateFailure($"Error getting textures: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all prefabs in the project
        /// </summary>
        /// <returns>Result with success status and list of prefab paths</returns>
        public static OperationResult<List<string>> GetAllPrefabs()
        {
            try
            {
                List<string> prefabs = SceneManipulationUtility.GetAllPrefabs();
                if (prefabs == null)
                {
                    return OperationResult<List<string>>.CreateFailure("Failed to get prefabs from project");
                }
                
                return OperationResult<List<string>>.CreateSuccess(prefabs, 
                    $"Found {prefabs.Count} prefabs in the project");
            }
            catch (Exception ex)
            {
                return OperationResult<List<string>>.CreateFailure($"Error getting prefabs: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a material on a GameObject with a renderer
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="materialPath">Path to the material asset</param>
        /// <param name="materialIndex">Index of the material to set (for multi-material renderers)</param>
        /// <returns>Result with success status</returns>
        public static OperationResult<bool> SetMaterial(string path, string materialPath, int materialIndex = 0)
        {
            try
            {
                bool success = SceneManipulationUtility.SetMaterial(path, materialPath, materialIndex);
                if (!success)
                {
                    return OperationResult<bool>.CreateFailure(
                        $"Failed to set material '{materialPath}' on renderer of GameObject at '{path}'");
                }
                
                return OperationResult<bool>.CreateSuccess(true, 
                    $"Set material '{materialPath}' on renderer of '{path}' at index {materialIndex}");
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.CreateFailure($"Error setting material: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Generic class to represent the result of an operation
    /// </summary>
    public class OperationResult<T>
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; private set; }
        
        /// <summary>
        /// Result value (if successful)
        /// </summary>
        public T Value { get; private set; }
        
        /// <summary>
        /// Message describing the result
        /// </summary>
        public string Message { get; private set; }
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static OperationResult<T> CreateSuccess(T value, string message)
        {
            return new OperationResult<T> { Success = true, Value = value, Message = message };
        }
        
        /// <summary>
        /// Creates a failure result
        /// </summary>
        public static OperationResult<T> CreateFailure(string message)
        {
            return new OperationResult<T> { Success = false, Message = message };
        }
    }
} 