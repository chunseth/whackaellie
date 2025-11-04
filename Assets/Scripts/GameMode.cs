using UnityEngine;

/// <summary>
/// Enum defining different game modes
/// </summary>
public enum GameMode
{
    LevelMode,      // Progress through levels with score targets
    SurvivalMode    // Survive as long as possible with limited lives
}

/// <summary>
/// Settings specific to survival mode
/// </summary>
[System.Serializable]
public class SurvivalModeSettings
{
    [Header("Lives")]
    public int startingLives = 3;
    
    [Header("Penalties")]
    public bool loseLiveOnMiss = true;           // Lose life when animal escapes
    public bool loseLiveOnSnakeHit = true;       // Lose life when hitting snake
    
    [Header("Scoring")]
    public bool snakeGivesPoints = false;        // If false, snake gives no points (only penalty)
    public int snakePenaltyPoints = -10;         // Points to lose when hitting snake (if it gives points)
    
    [Header("Spawn Rates")]
    public float startMinSpawnRate = 1.0f;          // Starting minimum spawn rate (animals/sec)
    public float startMaxSpawnRate = 2.0f;          // Starting maximum spawn rate (animals/sec)
    public int maxActiveAnimals = 7;                // Maximum animals on screen at once
    
    [Header("Difficulty Scaling")]
    public bool increaseDifficultyOverTime = true;  // Gradually make it harder
    public float difficultyIncreaseInterval = 30f;  // Every X seconds, increase difficulty
    public float spawnRateIncrease = 0.2f;          // How much to increase spawn rate per interval
    public float maxSpawnRate = 5.0f;               // Maximum spawn rate cap
}

