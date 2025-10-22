using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to find GameObjects with missing script references
/// </summary>
public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/WhackAEllie/Find Missing Scripts in Scene")]
    public static void FindMissingScriptsInScene()
    {
        GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
        int missingCount = 0;
        
        foreach (GameObject go in gameObjects)
        {
            Component[] components = go.GetComponents<Component>();
            
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                    Debug.LogError($"Missing script on GameObject: {GetGameObjectPath(go)}", go);
                }
            }
        }
        
        if (missingCount > 0)
        {
            Debug.LogWarning($"Found {missingCount} missing script(s) in scene!");
            EditorUtility.DisplayDialog("Missing Scripts Found", 
                $"Found {missingCount} missing script reference(s).\nCheck the Console for details.", 
                "OK");
        }
        else
        {
            Debug.Log("No missing scripts found!");
            EditorUtility.DisplayDialog("All Clear", 
                "No missing script references found in the scene.", 
                "OK");
        }
    }
    
    [MenuItem("Tools/WhackAEllie/Remove All Missing Scripts")]
    public static void RemoveMissingScripts()
    {
        GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
        int removedCount = 0;
        
        foreach (GameObject go in gameObjects)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (count > 0)
            {
                removedCount += count;
                Debug.Log($"Removed {count} missing script(s) from: {GetGameObjectPath(go)}", go);
            }
        }
        
        if (removedCount > 0)
        {
            Debug.Log($"Removed {removedCount} missing script(s) total!");
            EditorUtility.DisplayDialog("Cleanup Complete", 
                $"Removed {removedCount} missing script reference(s).", 
                "OK");
        }
        else
        {
            Debug.Log("No missing scripts to remove!");
        }
    }
    
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
}

