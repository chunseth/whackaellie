using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Balancing calculator for roguelike point thresholds and wave design
/// This script calculates balanced point goals, spawn rates, and animal compositions
/// accounting for combo multipliers and difficulty progression
/// </summary>
public class RoguelikeBalancer : MonoBehaviour
{
    /// <summary>
    /// Stats for each animal type
    /// </summary>
    [System.Serializable]
    public class AnimalStats
    {
        public AnimalType type;
        public int basePoints;
        public float spawnWeight;        // How likely to spawn (1.0 = normal)
        public float visibleDuration;    // How long they stay up
        public string description;
    }

    [System.Serializable]
    public class ComboTier
    {
        public int tierLevel;            // 0 = no combo, 1-4 = after each boss
        public int hitsToStartCombo;     // How many hits before combo begins
        public float comboMultiplier;    // Multiplier value (e.g., 2.0 = 2x)
        public int comboActiveHits;      // How many hits the multiplier applies to
        public string description;
    }

    [System.Serializable]
    public class WaveData
    {
        public int roundNumber;
        public int waveNumber;           // 1, 2, or 3 (boss)
        public int pointThreshold;
        public float duration;           // Time in seconds
        public int minSpawnRate;         // Min animals per second
        public int maxSpawnRate;         // Max animals per second
        public Dictionary<AnimalType, int> animalCounts;  // Specific animal counts
        public List<AnimalType> allowedAnimals;
        public ComboTier comboTier;
    }

    [Header("Animal Balance")]
    [SerializeField] private AnimalStats[] animalStats = new AnimalStats[]
    {
        new AnimalStats { type = AnimalType.Rabbit, basePoints = 5, spawnWeight = 1.0f, visibleDuration = 1.0f, description = "Fast, low value" },
        new AnimalStats { type = AnimalType.Meerkat, basePoints = 10, spawnWeight = 0.8f, visibleDuration = 1.5f, description = "Medium speed, medium value" },
        new AnimalStats { type = AnimalType.Fox, basePoints = 15, spawnWeight = 0.6f, visibleDuration = 1.2f, description = "Quick, good value" },
        new AnimalStats { type = AnimalType.Badger, basePoints = 25, spawnWeight = 0.4f, visibleDuration = 2.5f, description = "Slow, high value" },
        new AnimalStats { type = AnimalType.Snake, basePoints = -10, spawnWeight = 0.3f, visibleDuration = 1.8f, description = "Penalty, avoid!" }
    };

    [Header("Combo Tiers (Unlocked After Each Boss)")]
    [SerializeField] private ComboTier[] comboTiers = new ComboTier[]
    {
        new ComboTier { tierLevel = 0, hitsToStartCombo = 999, comboMultiplier = 1.0f, comboActiveHits = 0, description = "No Combo" },
        new ComboTier { tierLevel = 1, hitsToStartCombo = 3, comboMultiplier = 2.0f, comboActiveHits = 5, description = "Basic: 3 hits for 2x (5 hits)" },
        new ComboTier { tierLevel = 2, hitsToStartCombo = 2, comboMultiplier = 2.5f, comboActiveHits = 8, description = "Improved: 2 hits for 2.5x (8 hits)" },
        new ComboTier { tierLevel = 3, hitsToStartCombo = 2, comboMultiplier = 3.0f, comboActiveHits = 12, description = "Advanced: 2 hits for 3x (12 hits)" },
        new ComboTier { tierLevel = 4, hitsToStartCombo = 1, comboMultiplier = 3.5f, comboActiveHits = 15, description = "Master: 1 hit for 3.5x (15 hits)" }
    };

    [Header("Calculated Wave Data")]
    [SerializeField] private List<WaveData> allWaves = new List<WaveData>();

    [Header("Debug")]
    [SerializeField] private bool showDetailedLogs = true;

    // Balance Constants
    private const float ACCURACY_ASSUMPTION = 0.75f;  // Assume player hits 75% of spawns
    private const float SCALING_FACTOR = 1.5f;        // How aggressively difficulty scales
    private const float BOSS_MULTIPLIER = 1.3f;       // Boss waves are 30% harder

    private void Start()
    {
        CalculateAllWaveBalancing();
    }

    /// <summary>
    /// Main calculation - generates balanced waves for all 8 rounds
    /// </summary>
    [ContextMenu("Calculate Wave Balancing")]
    public void CalculateAllWaveBalancing()
    {
        allWaves.Clear();

        if (showDetailedLogs)
        {
            Debug.Log("=== ROGUELIKE BALANCING CALCULATOR ===\n");
            PrintAnimalStats();
            PrintComboTiers();
        }

        // Calculate each round
        for (int round = 1; round <= 8; round++)
        {
            CalculateRound(round);
        }

        if (showDetailedLogs)
        {
            Debug.Log("\n=== SUMMARY TABLE ===");
            PrintSummaryTable();
        }
    }

    /// <summary>
    /// Calculates balancing for a single round (3 waves)
    /// </summary>
    private void CalculateRound(int roundNumber)
    {
        // Determine combo tier based on which bosses have been beaten
        int comboTierLevel = Mathf.Min(roundNumber - 1, comboTiers.Length - 1);
        ComboTier currentCombo = comboTiers[comboTierLevel];

        if (showDetailedLogs)
        {
            Debug.Log($"\n=== ROUND {roundNumber} ===");
            Debug.Log($"Combo Tier: {currentCombo.description}");
        }

        // Calculate base point threshold for this round
        float baseThreshold = CalculateBaseThreshold(roundNumber);

        // Wave 1: Easier warm-up
        WaveData wave1 = CalculateWave(roundNumber, 1, baseThreshold * 0.5f, currentCombo);
        allWaves.Add(wave1);

        // Wave 2: Standard difficulty
        WaveData wave2 = CalculateWave(roundNumber, 2, baseThreshold * 1.0f, currentCombo);
        allWaves.Add(wave2);

        // Wave 3 (Boss): Harder challenge
        WaveData wave3 = CalculateWave(roundNumber, 3, baseThreshold * 2.0f, currentCombo, true);
        allWaves.Add(wave3);

        if (showDetailedLogs)
        {
            PrintWave(wave1);
            PrintWave(wave2);
            PrintWave(wave3);
        }
    }

    /// <summary>
    /// Calculates base point threshold for a round (exponential scaling)
    /// </summary>
    private float CalculateBaseThreshold(int roundNumber)
    {
        // Round 1 baseline: Wave 2 = 100 points
        float baseValue = 100f;

        if (roundNumber == 1)
        {
            return baseValue;
        }

        // Exponential scaling: each round multiplies by scaling factor
        float multiplier = Mathf.Pow(SCALING_FACTOR, roundNumber - 1);
        return baseValue * multiplier;
    }

    /// <summary>
    /// Calculates a single wave's balancing
    /// </summary>
    private WaveData CalculateWave(int roundNumber, int waveNumber, float targetPoints, ComboTier combo, bool isBoss = false)
    {
        WaveData wave = new WaveData
        {
            roundNumber = roundNumber,
            waveNumber = waveNumber,
            pointThreshold = Mathf.RoundToInt(targetPoints),
            comboTier = combo,
            animalCounts = new Dictionary<AnimalType, int>(),
            allowedAnimals = new List<AnimalType>()
        };

        // Determine which animals are available this round
        wave.allowedAnimals = GetAllowedAnimals(roundNumber);

        // Calculate duration based on round and wave
        wave.duration = CalculateWaveDuration(roundNumber, waveNumber, isBoss);

        // Calculate spawn rates
        (wave.minSpawnRate, wave.maxSpawnRate) = CalculateSpawnRates(roundNumber, isBoss);

        // Calculate animal composition to reach point threshold
        wave.animalCounts = CalculateAnimalComposition(wave, targetPoints, combo);

        return wave;
    }

    /// <summary>
    /// Determines which animals are available based on round number
    /// </summary>
    private List<AnimalType> GetAllowedAnimals(int roundNumber)
    {
        List<AnimalType> allowed = new List<AnimalType>();

        switch (roundNumber)
        {
            case 1:
                allowed.Add(AnimalType.Rabbit);
                allowed.Add(AnimalType.Meerkat);
                break;
            case 2:
            case 3:
                allowed.Add(AnimalType.Rabbit);
                allowed.Add(AnimalType.Meerkat);
                allowed.Add(AnimalType.Fox);
                allowed.Add(AnimalType.Snake);
                break;
            case 4:
            case 5:
                allowed.Add(AnimalType.Rabbit);
                allowed.Add(AnimalType.Meerkat);
                allowed.Add(AnimalType.Fox);
                allowed.Add(AnimalType.Badger);
                allowed.Add(AnimalType.Snake);
                break;
            default: // Rounds 6-8
                allowed.Add(AnimalType.Meerkat);
                allowed.Add(AnimalType.Fox);
                allowed.Add(AnimalType.Badger);
                allowed.Add(AnimalType.Snake);
                break;
        }

        return allowed;
    }

    /// <summary>
    /// Calculates appropriate wave duration
    /// </summary>
    private float CalculateWaveDuration(int roundNumber, int waveNumber, bool isBoss)
    {
        float baseDuration = 10f;

        // Wave 1 is shorter (warm-up)
        if (waveNumber == 1)
            baseDuration = 8f;

        // Boss waves are longer
        if (isBoss)
            baseDuration = 12f;

        // Later rounds get slightly longer to accommodate difficulty
        baseDuration += (roundNumber - 1) * 1f;

        return baseDuration;
    }

    /// <summary>
    /// Calculates spawn rates (animals per second)
    /// </summary>
    private (int min, int max) CalculateSpawnRates(int roundNumber, bool isBoss)
    {
        int baseMin = 1;
        int baseMax = 3;

        // Scale with round number
        int min = baseMin + (roundNumber - 1) / 2;
        int max = baseMax + (roundNumber - 1);

        // Boss waves spawn faster
        if (isBoss)
        {
            min += 1;
            max += 2;
        }

        // Cap at reasonable limits
        min = Mathf.Min(min, 4);
        max = Mathf.Min(max, 10);

        return (min, max);
    }

    /// <summary>
    /// Calculates how many of each animal to spawn to reach target points
    /// Accounts for combo multiplier and accuracy
    /// </summary>
    private Dictionary<AnimalType, int> CalculateAnimalComposition(WaveData wave, float targetPoints, ComboTier combo)
    {
        Dictionary<AnimalType, int> composition = new Dictionary<AnimalType, int>();

        // Calculate effective points with combo
        float effectiveMultiplier = CalculateAverageComboMultiplier(combo);
        float requiredRawPoints = targetPoints / (effectiveMultiplier * ACCURACY_ASSUMPTION);

        // Distribute points across allowed animals based on weights
        float totalWeight = 0f;
        Dictionary<AnimalType, float> weights = new Dictionary<AnimalType, float>();

        foreach (AnimalType type in wave.allowedAnimals)
        {
            AnimalStats stats = GetAnimalStats(type);
            if (stats.basePoints > 0) // Don't include snakes in primary calculations
            {
                weights[type] = stats.spawnWeight;
                totalWeight += stats.spawnWeight;
            }
        }

        // Calculate counts for each animal type
        foreach (var kvp in weights)
        {
            AnimalType type = kvp.Key;
            float weight = kvp.Value;
            AnimalStats stats = GetAnimalStats(type);

            float pointsForThisType = requiredRawPoints * (weight / totalWeight);
            int count = Mathf.RoundToInt(pointsForThisType / stats.basePoints);

            composition[type] = Mathf.Max(count, 1); // At least 1
        }

        // Add snakes (10-20% of total spawns as penalties)
        if (wave.allowedAnimals.Contains(AnimalType.Snake))
        {
            int totalAnimals = 0;
            foreach (var count in composition.Values)
                totalAnimals += count;

            int snakeCount = Mathf.RoundToInt(totalAnimals * Random.Range(0.1f, 0.2f));
            composition[AnimalType.Snake] = snakeCount;
        }

        return composition;
    }

    /// <summary>
    /// Calculates average combo multiplier assuming player maintains combo
    /// </summary>
    private float CalculateAverageComboMultiplier(ComboTier combo)
    {
        if (combo.hitsToStartCombo >= 999)
            return 1.0f; // No combo

        // Assume player maintains combo for 60% of their hits
        float hitsInCombo = combo.comboActiveHits * 0.6f;
        float hitsOutOfCombo = combo.hitsToStartCombo;
        float totalHits = hitsInCombo + hitsOutOfCombo;

        float weightedMultiplier = (hitsOutOfCombo * 1.0f + hitsInCombo * combo.comboMultiplier) / totalHits;

        return weightedMultiplier;
    }

    /// <summary>
    /// Gets animal stats by type
    /// </summary>
    private AnimalStats GetAnimalStats(AnimalType type)
    {
        foreach (var stats in animalStats)
        {
            if (stats.type == type)
                return stats;
        }
        return animalStats[0]; // Fallback to rabbit
    }

    /// <summary>
    /// Prints animal statistics
    /// </summary>
    private void PrintAnimalStats()
    {
        Debug.Log("=== ANIMAL STATISTICS ===");
        foreach (var stats in animalStats)
        {
            Debug.Log($"{stats.type}: {stats.basePoints}pts | Visible: {stats.visibleDuration}s | Weight: {stats.spawnWeight} | {stats.description}");
        }
    }

    /// <summary>
    /// Prints combo tier information
    /// </summary>
    private void PrintComboTiers()
    {
        Debug.Log("\n=== COMBO TIER PROGRESSION ===");
        for (int i = 1; i < comboTiers.Length; i++)
        {
            ComboTier tier = comboTiers[i];
            Debug.Log($"After Boss {i}: {tier.description}");
        }
    }

    /// <summary>
    /// Prints detailed wave information
    /// </summary>
    private void PrintWave(WaveData wave)
    {
        string waveType = wave.waveNumber == 3 ? "BOSS" : $"W{wave.waveNumber}";
        Debug.Log($"\n[R{wave.roundNumber}{waveType}] Target: {wave.pointThreshold}pts | Duration: {wave.duration}s | Spawn Rate: {wave.minSpawnRate}-{wave.maxSpawnRate}/s");

        int totalAnimals = 0;
        int totalPossiblePoints = 0;

        foreach (var kvp in wave.animalCounts)
        {
            AnimalType type = kvp.Key;
            int count = kvp.Value;
            AnimalStats stats = GetAnimalStats(type);

            totalAnimals += count;
            totalPossiblePoints += count * stats.basePoints;

            Debug.Log($"  {type}: {count}x ({count * stats.basePoints} pts)");
        }

        float avgComboMultiplier = CalculateAverageComboMultiplier(wave.comboTier);
        int expectedPoints = Mathf.RoundToInt(totalPossiblePoints * avgComboMultiplier * ACCURACY_ASSUMPTION);

        Debug.Log($"  Total: {totalAnimals} animals | Raw: {totalPossiblePoints}pts | Expected w/combo: {expectedPoints}pts");
        Debug.Log($"  Avg Combo Mult: {avgComboMultiplier:F2}x | Accuracy: {ACCURACY_ASSUMPTION * 100}%");
    }

    /// <summary>
    /// Prints summary table of all waves
    /// </summary>
    private void PrintSummaryTable()
    {
        Debug.Log("\nRound | Wave | Target | Duration | Animals | Combo Tier");
        Debug.Log("------|------|--------|----------|---------|------------");

        foreach (var wave in allWaves)
        {
            int totalAnimals = 0;
            foreach (var count in wave.animalCounts.Values)
                totalAnimals += count;

            string waveType = wave.waveNumber == 3 ? "BOSS" : $"W{wave.waveNumber}";
            Debug.Log($"  {wave.roundNumber}   | {waveType}  | {wave.pointThreshold,6} | {wave.duration,8}s | {totalAnimals,7} | Tier {wave.comboTier.tierLevel}");
        }
    }

    /// <summary>
    /// Export wave data for use in game
    /// </summary>
    public WaveData GetWave(int roundNumber, int waveNumber)
    {
        return allWaves.Find(w => w.roundNumber == roundNumber && w.waveNumber == waveNumber);
    }

    /// <summary>
    /// Get all waves for a specific round
    /// </summary>
    public List<WaveData> GetRoundWaves(int roundNumber)
    {
        return allWaves.FindAll(w => w.roundNumber == roundNumber);
    }
}

