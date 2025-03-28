using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace AICodingAssistant.Scripts
{
    /// <summary>
    /// Utility for manipulating Unity scenes, GameObjects and components.
    /// Allows the AI to directly interact with scene content.
    /// </summary>
    public static class SceneManipulationUtility
    {
        #region Scene Operations

        /// <summary>
        /// Gets the currently active scene
        /// </summary>
        /// <returns>Name of the active scene</returns>
        public static string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Gets the path of the currently active scene
        /// </summary>
        /// <returns>Path of the active scene</returns>
        public static string GetActiveScenePath()
        {
            return SceneManager.GetActiveScene().path;
        }

        /// <summary>
        /// Saves the current scene
        /// </summary>
        /// <returns>True if successful</returns>
        public static bool SaveActiveScene()
        {
            try
            {
                return EditorSceneManager.SaveOpenScenes();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving scene: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a list of all GameObjects in the current scene
        /// </summary>
        /// <returns>List of GameObject names and their hierarchical paths</returns>
        public static List<string> GetAllGameObjectsInScene()
        {
            List<string> result = new List<string>();
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            
            foreach (GameObject obj in allObjects)
            {
                string path = GetGameObjectPath(obj);
                result.Add(path);
            }
            
            return result;
        }

        /// <summary>
        /// Gets the hierarchical path of a GameObject
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        #endregion

        #region GameObject Operations

        /// <summary>
        /// Creates a new GameObject in the scene
        /// </summary>
        /// <param name="name">Name for the new GameObject</param>
        /// <param name="parentPath">Optional path to parent GameObject</param>
        /// <returns>Path to the created GameObject</returns>
        public static string CreateGameObject(string name, string parentPath = null)
        {
            try
            {
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parent = FindGameObjectByPath(parentPath);
                    if (parent == null)
                    {
                        Debug.LogError($"Parent GameObject not found: {parentPath}");
                        return null;
                    }
                }

                GameObject newObject = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(newObject, $"Create {name} GameObject");
                
                if (parent != null)
                {
                    newObject.transform.SetParent(parent.transform);
                    newObject.transform.localPosition = Vector3.zero;
                    newObject.transform.localRotation = Quaternion.identity;
                    newObject.transform.localScale = Vector3.one;
                }
                
                Selection.activeGameObject = newObject;
                return GetGameObjectPath(newObject);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating GameObject: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a primitive GameObject (cube, sphere, etc.)
        /// </summary>
        /// <param name="primitiveType">Type of primitive to create</param>
        /// <param name="name">Name for the new GameObject</param>
        /// <param name="parentPath">Optional path to parent GameObject</param>
        /// <returns>Path to the created GameObject</returns>
        public static string CreatePrimitive(PrimitiveType primitiveType, string name, string parentPath = null)
        {
            try
            {
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parent = FindGameObjectByPath(parentPath);
                    if (parent == null)
                    {
                        Debug.LogError($"Parent GameObject not found: {parentPath}");
                        return null;
                    }
                }

                GameObject newObject = GameObject.CreatePrimitive(primitiveType);
                newObject.name = name;
                Undo.RegisterCreatedObjectUndo(newObject, $"Create {name} {primitiveType}");
                
                if (parent != null)
                {
                    newObject.transform.SetParent(parent.transform);
                    newObject.transform.localPosition = Vector3.zero;
                    newObject.transform.localRotation = Quaternion.identity;
                    newObject.transform.localScale = Vector3.one;
                }
                
                Selection.activeGameObject = newObject;
                return GetGameObjectPath(newObject);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating primitive: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds a GameObject by its hierarchical path
        /// </summary>
        /// <param name="path">Path to the GameObject (e.g. "Parent/Child/GrandChild")</param>
        /// <returns>The GameObject if found, null otherwise</returns>
        public static GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
                
            string[] pathParts = path.Split('/');
            
            // Find all root GameObjects
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            
            // Find the root object with the matching name
            GameObject currentObject = Array.Find(rootObjects, obj => obj.name == pathParts[0]);
            if (currentObject == null)
                return null;
                
            // Traverse the hierarchy
            for (int i = 1; i < pathParts.Length; i++)
            {
                Transform child = currentObject.transform.Find(pathParts[i]);
                if (child == null)
                    return null;
                    
                currentObject = child.gameObject;
            }
            
            return currentObject;
        }

        /// <summary>
        /// Renames a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="newName">New name for the GameObject</param>
        /// <returns>Updated path to the GameObject</returns>
        public static string RenameGameObject(string path, string newName)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return null;
                }
                
                Undo.RecordObject(obj, $"Rename {obj.name} to {newName}");
                obj.name = newName;
                
                return GetGameObjectPath(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error renaming GameObject: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <returns>True if successful</returns>
        public static bool DeleteGameObject(string path)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return false;
                }
                
                Undo.DestroyObjectImmediate(obj);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deleting GameObject: {ex.Message}");
                return false;
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
        /// <returns>True if successful</returns>
        public static bool SetPosition(string path, float x, float y, float z, bool isLocal = false)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return false;
                }
                
                Undo.RecordObject(obj.transform, $"Set {(isLocal ? "Local" : "World")} Position");
                
                if (isLocal)
                    obj.transform.localPosition = new Vector3(x, y, z);
                else
                    obj.transform.position = new Vector3(x, y, z);
                    
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting position: {ex.Message}");
                return false;
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
        /// <returns>True if successful</returns>
        public static bool SetRotation(string path, float x, float y, float z, bool isLocal = false)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return false;
                }
                
                Undo.RecordObject(obj.transform, $"Set {(isLocal ? "Local" : "World")} Rotation");
                
                if (isLocal)
                    obj.transform.localEulerAngles = new Vector3(x, y, z);
                else
                    obj.transform.eulerAngles = new Vector3(x, y, z);
                    
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting rotation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the scale of a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="x">X scale</param>
        /// <param name="y">Y scale</param>
        /// <param name="z">Z scale</param>
        /// <returns>True if successful</returns>
        public static bool SetScale(string path, float x, float y, float z)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return false;
                }
                
                Undo.RecordObject(obj.transform, "Set Scale");
                obj.transform.localScale = new Vector3(x, y, z);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting scale: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Component Operations

        /// <summary>
        /// Gets all components on a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <returns>List of component names</returns>
        public static List<string> GetComponents(string path)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return null;
                }
                
                Component[] components = obj.GetComponents<Component>();
                List<string> result = new List<string>();
                
                foreach (Component component in components)
                {
                    result.Add(component.GetType().Name);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting components: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets component details including field values
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="componentName">Name of the component</param>
        /// <returns>Dictionary of field names and their values</returns>
        public static Dictionary<string, string> GetComponentDetails(string path, string componentName)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return null;
                }
                
                Component component = FindComponentByName(obj, componentName);
                if (component == null)
                {
                    Debug.LogError($"Component {componentName} not found on {path}");
                    return null;
                }
                
                Dictionary<string, string> result = new Dictionary<string, string>();
                Type type = component.GetType();
                
                // Get all public properties
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo property in properties)
                {
                    if (property.CanRead && property.GetIndexParameters().Length == 0)
                    {
                        try
                        {
                            object value = property.GetValue(component);
                            result[property.Name] = value?.ToString() ?? "null";
                        }
                        catch
                        {
                            // Skip properties that cannot be read
                        }
                    }
                }
                
                // Get all public fields
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        object value = field.GetValue(component);
                        result[field.Name] = value?.ToString() ?? "null";
                    }
                    catch
                    {
                        // Skip fields that cannot be read
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting component details: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a component to a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="componentName">Name of the component to add</param>
        /// <returns>True if successful</returns>
        public static bool AddComponent(string path, string componentName)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return false;
                }
                
                // Find the component type by name
                Type componentType = GetTypeByName(componentName);
                if (componentType == null)
                {
                    Debug.LogError($"Component type {componentName} not found");
                    return false;
                }
                
                // Check if the component can be added to a GameObject
                if (!componentType.IsSubclassOf(typeof(Component)))
                {
                    Debug.LogError($"{componentName} is not a Component");
                    return false;
                }
                
                // Add the component
                Component component = Undo.AddComponent(obj, componentType);
                if (component != null)
                {
                    Debug.Log($"Added {componentName} to {path}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding component: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes a component from a GameObject
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="componentName">Name of the component to remove</param>
        /// <returns>True if successful</returns>
        public static bool RemoveComponent(string path, string componentName)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return false;
                }
                
                Component component = FindComponentByName(obj, componentName);
                if (component == null)
                {
                    Debug.LogError($"Component {componentName} not found on {path}");
                    return false;
                }
                
                Undo.DestroyObjectImmediate(component);
                Debug.Log($"Removed {componentName} from {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error removing component: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets a field value on a component
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="componentName">Name of the component</param>
        /// <param name="fieldName">Name of the field to set</param>
        /// <param name="value">Value to set (as string)</param>
        /// <returns>True if successful</returns>
        public static bool SetComponentField(string path, string componentName, string fieldName, string value)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return false;
                }
                
                Component component = FindComponentByName(obj, componentName);
                if (component == null)
                {
                    Debug.LogError($"Component {componentName} not found on {path}");
                    return false;
                }
                
                Undo.RecordObject(component, $"Set {fieldName} on {componentName}");
                
                // Try setting property first
                PropertyInfo property = component.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanWrite)
                {
                    object convertedValue = ConvertToType(value, property.PropertyType);
                    property.SetValue(component, convertedValue);
                    return true;
                }
                
                // Then try setting field
                FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    object convertedValue = ConvertToType(value, field.FieldType);
                    field.SetValue(component, convertedValue);
                    return true;
                }
                
                Debug.LogError($"Field or property {fieldName} not found on component {componentName}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting component field: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Asset Operations

        /// <summary>
        /// Gets all materials in the project
        /// </summary>
        /// <returns>List of material paths</returns>
        public static List<string> GetAllMaterials()
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:Material");
                List<string> paths = new List<string>();
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    paths.Add(path);
                }
                
                return paths;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error finding materials: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all textures in the project
        /// </summary>
        /// <returns>List of texture paths</returns>
        public static List<string> GetAllTextures()
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture");
                List<string> paths = new List<string>();
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    paths.Add(path);
                }
                
                return paths;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error finding textures: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all prefabs in the project
        /// </summary>
        /// <returns>List of prefab paths</returns>
        public static List<string> GetAllPrefabs()
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab");
                List<string> paths = new List<string>();
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    paths.Add(path);
                }
                
                return paths;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error finding prefabs: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the material on a GameObject with a Renderer
        /// </summary>
        /// <param name="path">Path to the GameObject</param>
        /// <param name="materialPath">Path to the material asset</param>
        /// <param name="materialIndex">Index of the material to set (for multi-material renderers)</param>
        /// <returns>True if successful</returns>
        public static bool SetMaterial(string path, string materialPath, int materialIndex = 0)
        {
            try
            {
                GameObject obj = FindGameObjectByPath(path);
                if (obj == null)
                {
                    Debug.LogError($"GameObject not found: {path}");
                    return false;
                }
                
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer == null)
                {
                    Debug.LogError($"No Renderer component found on {path}");
                    return false;
                }
                
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    Debug.LogError($"Material not found at {materialPath}");
                    return false;
                }
                
                Undo.RecordObject(renderer, "Set Material");
                
                // For single material renderers or first material
                if (materialIndex == 0 && renderer.sharedMaterials.Length == 1)
                {
                    renderer.sharedMaterial = material;
                }
                // For multi-material renderers
                else
                {
                    if (materialIndex < 0 || materialIndex >= renderer.sharedMaterials.Length)
                    {
                        Debug.LogError($"Material index {materialIndex} out of range (0-{renderer.sharedMaterials.Length - 1})");
                        return false;
                    }
                    
                    Material[] materials = renderer.sharedMaterials;
                    materials[materialIndex] = material;
                    renderer.sharedMaterials = materials;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting material: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Instantiates a prefab in the scene
        /// </summary>
        /// <param name="prefabPath">Path to the prefab asset</param>
        /// <param name="parentPath">Optional path to parent GameObject</param>
        /// <returns>Path to the instantiated GameObject</returns>
        public static string InstantiatePrefab(string prefabPath, string parentPath = null)
        {
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"Prefab not found at {prefabPath}");
                    return null;
                }
                
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parent = FindGameObjectByPath(parentPath);
                    if (parent == null)
                    {
                        Debug.LogError($"Parent GameObject not found: {parentPath}");
                        return null;
                    }
                }
                
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, $"Instantiate Prefab {prefab.name}");
                
                if (parent != null)
                {
                    instance.transform.SetParent(parent.transform);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                }
                
                Selection.activeGameObject = instance;
                return GetGameObjectPath(instance);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error instantiating prefab: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Finds a component by name on a GameObject
        /// </summary>
        private static Component FindComponentByName(GameObject obj, string componentName)
        {
            Component[] components = obj.GetComponents<Component>();
            
            // Try exact match first
            foreach (Component component in components)
            {
                if (component.GetType().Name == componentName)
                    return component;
            }
            
            // Then try case-insensitive match
            foreach (Component component in components)
            {
                if (component.GetType().Name.Equals(componentName, StringComparison.OrdinalIgnoreCase))
                    return component;
            }
            
            return null;
        }

        /// <summary>
        /// Gets a Type by name
        /// </summary>
        private static Type GetTypeByName(string typeName)
        {
            // Try to find the type in all assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }
            
            // Alternative search for common Unity components
            if (typeName == "MeshRenderer" || typeName.Equals("meshrenderer", StringComparison.OrdinalIgnoreCase))
                return typeof(MeshRenderer);
                
            if (typeName == "Light" || typeName.Equals("light", StringComparison.OrdinalIgnoreCase))
                return typeof(Light);
                
            if (typeName == "Rigidbody" || typeName.Equals("rigidbody", StringComparison.OrdinalIgnoreCase))
                return typeof(Rigidbody);
                
            if (typeName == "Collider" || typeName.Equals("collider", StringComparison.OrdinalIgnoreCase))
                return typeof(Collider);
                
            if (typeName == "BoxCollider" || typeName.Equals("boxcollider", StringComparison.OrdinalIgnoreCase))
                return typeof(BoxCollider);
                
            if (typeName == "SphereCollider" || typeName.Equals("spherecollider", StringComparison.OrdinalIgnoreCase))
                return typeof(SphereCollider);
                
            if (typeName == "MeshFilter" || typeName.Equals("meshfilter", StringComparison.OrdinalIgnoreCase))
                return typeof(MeshFilter);
                
            if (typeName == "AudioSource" || typeName.Equals("audiosource", StringComparison.OrdinalIgnoreCase))
                return typeof(AudioSource);
                
            // Add more types as needed
            
            return null;
        }

        /// <summary>
        /// Converts a string value to the specified type
        /// </summary>
        private static object ConvertToType(string value, Type targetType)
        {
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                return bool.Parse(value);
            }
            else if (targetType == typeof(int) || targetType == typeof(int?))
            {
                return int.Parse(value);
            }
            else if (targetType == typeof(float) || targetType == typeof(float?))
            {
                return float.Parse(value);
            }
            else if (targetType == typeof(string))
            {
                return value;
            }
            else if (targetType == typeof(Vector3) || targetType == typeof(Vector3?))
            {
                // Format expected: "x,y,z"
                string[] parts = value.Trim().Split(',');
                if (parts.Length == 3)
                {
                    return new Vector3(
                        float.Parse(parts[0].Trim()),
                        float.Parse(parts[1].Trim()),
                        float.Parse(parts[2].Trim())
                    );
                }
            }
            else if (targetType == typeof(Vector2) || targetType == typeof(Vector2?))
            {
                // Format expected: "x,y"
                string[] parts = value.Trim().Split(',');
                if (parts.Length == 2)
                {
                    return new Vector2(
                        float.Parse(parts[0].Trim()),
                        float.Parse(parts[1].Trim())
                    );
                }
            }
            else if (targetType == typeof(Color) || targetType == typeof(Color?))
            {
                // Format expected: "r,g,b,a" or "r,g,b"
                string[] parts = value.Trim().Split(',');
                if (parts.Length >= 3)
                {
                    float r = float.Parse(parts[0].Trim());
                    float g = float.Parse(parts[1].Trim());
                    float b = float.Parse(parts[2].Trim());
                    float a = parts.Length > 3 ? float.Parse(parts[3].Trim()) : 1.0f;
                    return new Color(r, g, b, a);
                }
            }
            else if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value);
            }
            
            // Default: try direct conversion
            return Convert.ChangeType(value, targetType);
        }

        #endregion
    }
} 