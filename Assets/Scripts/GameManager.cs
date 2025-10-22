using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Mode")]
    [SerializeField] private GameMode gameMode = GameMode.LevelMode;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private LivesDisplay livesDisplay;  // Visual lives display for survival mode
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private TextMeshProUGUI instructionsText;  // Text to update per level
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI levelCompleteText;
    [SerializeField] private GameObject nextLevelButton;

    [Header("Level Mode Settings")]
    [SerializeField] private LevelConfiguration levelConfiguration;
    [SerializeField] private int startingLevel = 1;

    [Header("Survival Mode Settings")]
    [SerializeField] private SurvivalModeSettings survivalSettings;

    [Header("Game Settings (Fallback)")]
    [SerializeField] private float gameDuration = 60f;

    private int currentScore = 0;
    private int currentLevel = 1;
    private int currentLives = 3;
    private float timeRemaining;
    private bool isGameActive = false;
    private LevelData activeLevelData;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initialize based on game mode
        if (gameMode == GameMode.LevelMode)
        {
            // Load current level from PlayerPrefs (persists across scene reloads)
            currentLevel = PlayerPrefs.GetInt("CurrentLevel", startingLevel);
            Debug.Log($"Starting Level Mode - Current Level from PlayerPrefs: {currentLevel}");
            LoadLevelData();
        }
        else if (gameMode == GameMode.SurvivalMode)
        {
            // Initialize survival mode
            if (survivalSettings == null)
            {
                survivalSettings = new SurvivalModeSettings();
            }
            currentLives = survivalSettings.startingLives;
            Debug.Log($"Starting Survival Mode - Lives: {currentLives}");
        }
        
        // Set initial time (gameDuration should be set by LoadLevelData in level mode)
        timeRemaining = gameDuration;
        
        UpdateScoreUI();
        UpdateTimerUI();
        UpdateLevelUI();
        UpdateLivesUI();
        UpdateInstructionsText();
        
        // Game starts paused with instructions visible
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(false);
        }
    }
    
    /// <summary>
    /// Loads the current level data from configuration
    /// </summary>
    private void LoadLevelData()
    {
        // Try to load from Resources as fallback for builds
        if (levelConfiguration == null)
        {
            Debug.LogWarning("LevelConfiguration not assigned, attempting to load from Resources...");
            levelConfiguration = Resources.Load<LevelConfiguration>("LevelConfiguration");
            
            if (levelConfiguration != null)
            {
                Debug.Log("Successfully loaded LevelConfiguration from Resources!");
            }
        }
        
        if (levelConfiguration != null)
        {
            activeLevelData = levelConfiguration.GetLevel(currentLevel);
            
            if (activeLevelData != null)
            {
                gameDuration = activeLevelData.gameDuration;
                Debug.Log($"Loaded Level {currentLevel}: {activeLevelData.levelName} | Duration: {gameDuration}s | Target: {activeLevelData.targetScore}");
            }
            else
            {
                Debug.LogWarning($"No level data found for level {currentLevel}. Creating fallback level data...");
                CreateFallbackLevelData();
            }
        }
        else
        {
            Debug.LogError("LevelConfiguration could not be loaded! Creating fallback level data...");
            CreateFallbackLevelData();
        }
    }
    
    /// <summary>
    /// Creates fallback level data when configuration is missing (for builds)
    /// </summary>
    private void CreateFallbackLevelData()
    {
        // Create basic level data based on current level using constructor
        switch (currentLevel)
        {
            case 1:
                activeLevelData = new LevelData(
                    1, 
                    "Bunny Basics",
                    new AnimalType[] { AnimalType.Rabbit },
                    50,   // Lower target since only bunnies (5 pts each)
                    8f
                );
                break;
            case 2:
                activeLevelData = new LevelData(
                    2,
                    "Meerkat Mayhem",
                    new AnimalType[] { AnimalType.Rabbit, AnimalType.Meerkat, AnimalType.Snake },
                    100,
                    12f
                );
                break;
            case 3:
                activeLevelData = new LevelData(
                    3,
                    "Foxy Challenge",
                    new AnimalType[] { AnimalType.Rabbit, AnimalType.Meerkat, AnimalType.Fox, AnimalType.Snake },
                    230,
                    15f
                );
                break;
            case 4:
                activeLevelData = new LevelData(
                    4,
                    "Badger Bash",
                    new AnimalType[] { AnimalType.Meerkat, AnimalType.Fox, AnimalType.Badger, AnimalType.Snake },
                    420,
                    20f
                );
                break;
            default:
                // For any level beyond 4, use all animals
                activeLevelData = new LevelData(
                    currentLevel,
                    $"Level {currentLevel}",
                    new AnimalType[] { AnimalType.Rabbit, AnimalType.Meerkat, AnimalType.Fox, AnimalType.Badger, AnimalType.Snake },
                    200 + (currentLevel * 50),
                    10f
                );
                break;
        }
        
        gameDuration = activeLevelData.gameDuration;
        Debug.Log($"Created fallback Level {currentLevel}: {activeLevelData.levelName} | Duration: {gameDuration}s | Target: {activeLevelData.targetScore}");
    }

    private void Update()
    {
        if (isGameActive)
        {
            // Survival mode doesn't have a timer (endless until lives run out)
            if (gameMode == GameMode.LevelMode)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerUI();

                if (timeRemaining <= 0)
                {
                    EndGame();
                }
            }
            else if (gameMode == GameMode.SurvivalMode)
            {
                // In survival, time counts up to show how long they survived
                timeRemaining += Time.deltaTime;
                UpdateTimerUI();
                
                // Check if player is out of lives
                if (currentLives <= 0)
                {
                    EndGame();
                }
            }
        }
    }

    /// <summary>
    /// Starts the game - called when play button is clicked
    /// </summary>
    public void StartGame()
    {
        isGameActive = true;
        
        // Hide instructions panel
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(false);
        }

        // Reset game state
        currentScore = 0;
        
        if (gameMode == GameMode.LevelMode)
        {
            timeRemaining = gameDuration;
        }
        else if (gameMode == GameMode.SurvivalMode)
        {
            timeRemaining = 0; // Count up in survival mode
            currentLives = survivalSettings != null ? survivalSettings.startingLives : 3;
        }
        
        UpdateScoreUI();
        UpdateTimerUI();
        UpdateLivesUI();

        // Start spawning moles
        MoleSpawner spawner = FindObjectOfType<MoleSpawner>();
        if (spawner != null)
        {
            spawner.StartSpawning();
        }
    }

    /// <summary>
    /// Adds points to the current score
    /// </summary>
    public void AddScore(int points)
    {
        if (isGameActive)
        {
            currentScore += points;
            UpdateScoreUI();
        }
    }


    /// <summary>
    /// Ends the game (time ran out)
    /// </summary>
    private void EndGame()
    {
        isGameActive = false;
        timeRemaining = 0;
        UpdateTimerUI();

        // Stop spawning moles
        MoleSpawner spawner = FindObjectOfType<MoleSpawner>();
        if (spawner != null)
        {
            spawner.StopSpawning();
        }

        // Check if player reached target score
        bool levelCompleted = activeLevelData != null && currentScore >= activeLevelData.targetScore;
        
        // Check if there's a next level (works with or without levelConfiguration)
        int totalLevels = 4; // Default fallback
        if (levelConfiguration != null)
        {
            totalLevels = levelConfiguration.GetTotalLevels();
        }
        bool hasNextLevel = currentLevel < totalLevels;

        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (levelCompleteText != null)
            {
                if (levelCompleted)
                {
                    levelCompleteText.text = "Level Complete!";
                }
                else
                {
                    levelCompleteText.text = "You Lose!";
                }
            }
            
            if (gameOverScoreText != null)
            {
                if (activeLevelData != null)
                {
                    string scoreMessage = $"Final Score: {currentScore}";
                    if (!levelCompleted)
                    {
                        scoreMessage += $"\nTarget: {activeLevelData.targetScore}";
                    }
                    gameOverScoreText.text = scoreMessage;
                }
                else
                {
                    gameOverScoreText.text = "Final Score: " + currentScore.ToString();
                }
            }
            
            // Show next level button only if level was completed
            if (nextLevelButton != null)
            {
                nextLevelButton.SetActive(levelCompleted && hasNextLevel);
            }
        }
    }

    /// <summary>
    /// Updates the score text UI
    /// </summary>
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            if (activeLevelData != null)
            {
                scoreText.text = $"Score: {currentScore} / {activeLevelData.targetScore}";
            }
            else
            {
                scoreText.text = "Score: " + currentScore.ToString();
            }
        }
    }

    /// <summary>
    /// Updates the timer text UI
    /// </summary>
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            if (gameMode == GameMode.LevelMode)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(0, timeRemaining));
                timerText.text = "Time: " + seconds.ToString();
            }
            else if (gameMode == GameMode.SurvivalMode)
            {
                int seconds = Mathf.FloorToInt(timeRemaining);
                timerText.text = "Time: " + seconds.ToString() + "s";
            }
        }
    }
    
    /// <summary>
    /// Updates the level text UI
    /// </summary>
    private void UpdateLevelUI()
    {
        if (levelText != null)
        {
            if (gameMode == GameMode.LevelMode)
            {
                if (activeLevelData != null)
                {
                    levelText.text = $"Level {currentLevel}: {activeLevelData.levelName}";
                }
                else
                {
                    levelText.text = $"Level {currentLevel}";
                }
            }
            else if (gameMode == GameMode.SurvivalMode)
            {
                levelText.text = "Survival Mode";
            }
        }
    }
    
    /// <summary>
    /// Updates the lives display UI (for survival mode)
    /// </summary>
    private void UpdateLivesUI()
    {
        if (livesDisplay != null && gameMode == GameMode.SurvivalMode)
        {
            livesDisplay.UpdateLivesDisplay(currentLives);
        }
    }
    
    /// <summary>
    /// Updates the instructions panel text based on current level
    /// </summary>
    private void UpdateInstructionsText()
    {
        if (instructionsText != null && gameMode == GameMode.LevelMode)
        {
            switch (currentLevel)
            {
                case 1:
                    instructionsText.text = "Rabbits have taken over your yard!";
                    break;
                case 2:
                    instructionsText.text = "Be careful of the snakes...";
                    break;
                case 3:
                    instructionsText.text = "Get them foxes!";
                    break;
                case 4:
                    instructionsText.text = "Now there are badgers?!";
                    break;
                default:
                    instructionsText.text = "Ready to play?";
                    break;
            }
        }
    }

    /// <summary>
    /// Restarts the current level - called by Play Again button
    /// </summary>
    public void PlayAgain()
    {
        // Keep the current level when replaying
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Returns to main menu - called by Main Menu button
    /// </summary>
    public void ReturnToMainMenu()
    {
        // Reset to level 1 when returning to main menu
        PlayerPrefs.SetInt("CurrentLevel", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenuScene");
    }
    
    /// <summary>
    /// Resets progress back to level 1 (useful for testing or "New Game")
    /// </summary>
    public void ResetProgress()
    {
        PlayerPrefs.SetInt("CurrentLevel", 1);
        PlayerPrefs.Save();
        currentLevel = 1;
        LoadLevelData();
        UpdateLevelUI();
    }

    /// <summary>
    /// Loads the next level
    /// </summary>
    public void LoadNextLevel()
    {
        int totalLevels = 4; // Default fallback to 4 levels
        
        if (levelConfiguration != null)
        {
            totalLevels = levelConfiguration.GetTotalLevels();
        }
        
        if (currentLevel < totalLevels)
        {
            currentLevel++;
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);
            PlayerPrefs.Save();
            Debug.Log($"Loading next level: {currentLevel}");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.Log("Already at max level!");
        }
    }
    
    /// <summary>
    /// Gets the current level number
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    /// <summary>
    /// Gets the current level data
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        return activeLevelData;
    }

    /// <summary>
    /// Lose a life (survival mode only)
    /// </summary>
    public void LoseLife()
    {
        if (gameMode == GameMode.SurvivalMode && isGameActive)
        {
            currentLives--;
            UpdateLivesUI();
            
            Debug.Log($"Lost a life! Lives remaining: {currentLives}");
            
            if (currentLives <= 0)
            {
                EndGame();
            }
        }
    }
    
    /// <summary>
    /// Get current game mode
    /// </summary>
    public GameMode GetGameMode()
    {
        return gameMode;
    }
    
    /// <summary>
    /// Get survival mode settings
    /// </summary>
    public SurvivalModeSettings GetSurvivalSettings()
    {
        return survivalSettings;
    }

    /// <summary>
    /// Returns whether the game is currently active
    /// </summary>
    public bool IsGameActive()
    {
        return isGameActive;
    }
}

