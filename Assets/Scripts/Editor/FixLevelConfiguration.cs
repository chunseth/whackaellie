using UnityEngine;
using UnityEditor;

/// <summary>
/// Utility to fix LevelConfiguration ScriptableObject issues
/// </summary>
public class FixLevelConfiguration : EditorWindow
{
    [MenuItem("Tools/WhackAEllie/Recreate LevelConfiguration Asset")]
    public static void RecreateLevelConfiguration()
    {
        // Delete old broken asset if it exists
        string path = "Assets/Resources/LevelConfiguration.asset";
        if (AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path) != null)
        {
            if (EditorUtility.DisplayDialog("Delete Old Asset?",
                "This will delete the existing LevelConfiguration asset and create a new one. Continue?",
                "Yes, Recreate",
                "Cancel"))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log("Deleted old LevelConfiguration asset");
            }
            else
            {
                return;
            }
        }
        
        // Ensure Resources folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
            Debug.Log("Created Resources folder");
        }
        
        // Create new LevelConfiguration instance
        LevelConfiguration config = ScriptableObject.CreateInstance<LevelConfiguration>();
        
        // Initialize with default levels
        config.InitializeDefaultLevels();
        
        // Save it
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select it
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        
        Debug.Log($"Created new LevelConfiguration at {path} with 4 levels!");
        EditorUtility.DisplayDialog("Success!",
            "Created new LevelConfiguration with 4 default levels.\nThe asset is now selected in the Project window.",
            "OK");
    }
    
    [MenuItem("Tools/WhackAEllie/Verify LevelConfiguration")]
    public static void VerifyLevelConfiguration()
    {
        LevelConfiguration config = Resources.Load<LevelConfiguration>("LevelConfiguration");
        
        if (config == null)
        {
            Debug.LogError("LevelConfiguration not found in Resources folder!");
            EditorUtility.DisplayDialog("Not Found",
                "LevelConfiguration not found in Assets/Resources/\n\nUse 'Recreate LevelConfiguration Asset' to create one.",
                "OK");
            return;
        }
        
        Debug.Log($"LevelConfiguration found! Total levels: {config.GetTotalLevels()}");
        
        if (config.levels != null && config.levels.Length > 0)
        {
            for (int i = 0; i < config.levels.Length; i++)
            {
                LevelData level = config.levels[i];
                if (level != null)
                {
                    Debug.Log($"Level {level.levelNumber}: {level.levelName} | Animals: {level.allowedAnimals?.Length ?? 0} | Target: {level.targetScore} | Duration: {level.gameDuration}s");
                }
            }
            
            EditorUtility.DisplayDialog("Verification Success!",
                $"LevelConfiguration is valid!\n\nTotal Levels: {config.GetTotalLevels()}\n\nCheck Console for details.",
                "OK");
        }
        else
        {
            Debug.LogWarning("LevelConfiguration exists but has no levels! Use 'Initialize Default Levels' button in Inspector.");
            EditorUtility.DisplayDialog("Warning",
                "LevelConfiguration exists but has no levels configured.\n\nSelect the asset and click 'Initialize Default Levels'.",
                "OK");
        }
        
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }
}

