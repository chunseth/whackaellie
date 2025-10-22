using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for LevelConfiguration to make setup easier
/// </summary>
[CustomEditor(typeof(LevelConfiguration))]
public class LevelConfigurationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LevelConfiguration config = (LevelConfiguration)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Initialize Default 4 Levels", GUILayout.Height(30)))
        {
            Undo.RecordObject(config, "Initialize Default Levels");
            config.InitializeDefaultLevels();
            EditorUtility.SetDirty(config);
            Debug.Log("Initialized 4 default levels!");
        }
        
        EditorGUILayout.Space();
        
        if (config.levels != null && config.levels.Length > 0)
        {
            EditorGUILayout.LabelField($"Total Levels: {config.GetTotalLevels()}", EditorStyles.helpBox);
            
            EditorGUILayout.Space();
            
            for (int i = 0; i < config.levels.Length; i++)
            {
                LevelData level = config.levels[i];
                if (level != null && level.allowedAnimals != null)
                {
                    string animalList = string.Join(", ", level.allowedAnimals);
                    EditorGUILayout.HelpBox(
                        $"Level {level.levelNumber}: {level.levelName}\n" +
                        $"Animals: {animalList}\n" +
                        $"Target: {level.targetScore} pts | Duration: {level.gameDuration}s",
                        MessageType.Info
                    );
                }
            }
        }
    }
}

