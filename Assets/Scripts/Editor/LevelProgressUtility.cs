using UnityEngine;
using UnityEditor;

/// <summary>
/// Utility tools for managing level progress during development
/// </summary>
public class LevelProgressUtility
{
    [MenuItem("Tools/WhackAEllie/Reset Level Progress to 1")]
    public static void ResetLevelProgress()
    {
        PlayerPrefs.SetInt("CurrentLevel", 1);
        PlayerPrefs.Save();
        Debug.Log("Level progress reset to Level 1");
    }
    
    [MenuItem("Tools/WhackAEllie/Set Level to 2")]
    public static void SetLevelTo2()
    {
        PlayerPrefs.SetInt("CurrentLevel", 2);
        PlayerPrefs.Save();
        Debug.Log("Level set to 2");
    }
    
    [MenuItem("Tools/WhackAEllie/Set Level to 3")]
    public static void SetLevelTo3()
    {
        PlayerPrefs.SetInt("CurrentLevel", 3);
        PlayerPrefs.Save();
        Debug.Log("Level set to 3");
    }
    
    [MenuItem("Tools/WhackAEllie/Set Level to 4")]
    public static void SetLevelTo4()
    {
        PlayerPrefs.SetInt("CurrentLevel", 4);
        PlayerPrefs.Save();
        Debug.Log("Level set to 4");
    }
    
    [MenuItem("Tools/WhackAEllie/Show Current Level")]
    public static void ShowCurrentLevel()
    {
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        Debug.Log($"Current level in PlayerPrefs: {currentLevel}");
        EditorUtility.DisplayDialog("Current Level", $"Saved level: {currentLevel}", "OK");
    }
}

