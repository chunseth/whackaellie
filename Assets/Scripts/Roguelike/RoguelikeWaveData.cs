using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Data structure for a single wave in roguelike mode
/// </summary>
[System.Serializable]
public class RoguelikeWaveData
{
    public int roundNumber;
    public int waveNumber;
    public int pointTarget;
    public float duration;
    public float minSpawnRate;  // Minimum animals per second
    public float maxSpawnRate;  // Maximum animals per second
    public List<AnimalType> allowedAnimals;
    
    // Animal spawn limits for this wave
    public int maxRabbits = 7;
    public int maxMeerkats = 3;
    public int maxSnakes = 2;
    
    // Animal spawn probabilities
    public float meerkatSpawnChance = 0.2f;  // Probability to spawn meerkat (vs rabbit)
    public float snakeSpawnChance = 0.2f;    // Probability to spawn snake (vs rabbit)
    
    public RoguelikeWaveData(int round, int wave, int target, float time, float minRate, float maxRate)
    {
        roundNumber = round;
        waveNumber = wave;
        pointTarget = target;
        duration = time;
        minSpawnRate = minRate;
        maxSpawnRate = maxRate;
        allowedAnimals = new List<AnimalType>();
    }
}

/// <summary>
/// Player upgrade data that persists through a run
/// </summary>
[System.Serializable]
public class PlayerUpgrades
{
    [Header("Base Stats")]
    public int basePointsPerRabbit = 5;
    public int basePointsPerMeerkat = 10;
    public float waveDuration = 8f;
    public float minSpawnRate = 1f;
    public float maxSpawnRate = 3f;
    public int maxActiveAnimals = 7;  // Max animals on screen at once (increases with spawn rate)
    
    [Header("Combo System")]
    public int comboHitsToStart = 3;           // How many hits to trigger combo
    public float comboMultiplierIncrement = 2.0f;  // Multiplier when combo triggers
    
    [Header("Currency")]
    public int currentGold = 0;            // Gold earned this run ($ for shop)
    
    public PlayerUpgrades()
    {
        // Default values set above
    }
    
    /// <summary>
    /// Clone the upgrades for modification
    /// </summary>
    public PlayerUpgrades Clone()
    {
        return new PlayerUpgrades
        {
            basePointsPerRabbit = this.basePointsPerRabbit,
            basePointsPerMeerkat = this.basePointsPerMeerkat,
            waveDuration = this.waveDuration,
            minSpawnRate = this.minSpawnRate,
            maxSpawnRate = this.maxSpawnRate,
            maxActiveAnimals = this.maxActiveAnimals,
            comboHitsToStart = this.comboHitsToStart,
            comboMultiplierIncrement = this.comboMultiplierIncrement,
            currentGold = this.currentGold
        };
    }
}

