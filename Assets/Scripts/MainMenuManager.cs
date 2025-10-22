using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    /// <summary>
    /// Loads the GameScene (Level Mode) when the play button is clicked
    /// Resets to level 1 for a fresh start
    /// </summary>
    public void PlayGame()
    {
        // Reset progress to level 1 when starting from main menu
        PlayerPrefs.SetInt("CurrentLevel", 1);
        PlayerPrefs.Save();
        Debug.Log("Loading GameScene - Level reset to 1");
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Loads the SurvivalScene when the survival button is clicked
    /// </summary>
    public void PlaySurvival()
    {
        SceneManager.LoadScene("SurvivalScene");
    }

    /// <summary>
    /// Loads the BalatroScene (Roguelike Mode) when the roguelike button is clicked
    /// </summary>
    public void PlayRoguelike()
    {
        Debug.Log("Loading BalatroScene - Roguelike Mode");
        SceneManager.LoadScene("BalatroScene");
    }

    /// <summary>
    /// Quits the application (works in build only)
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}

