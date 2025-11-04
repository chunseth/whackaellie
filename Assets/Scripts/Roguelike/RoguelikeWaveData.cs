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
    public int basePointsPerFox = 15;
    public int basePointsPerBadger = 25;
    public float waveDuration = 10f;
    public float minSpawnRate = 1f;
    public float maxSpawnRate = 3f;
    public int maxActiveAnimals = 7;  // Max animals on screen at once (increases with spawn rate)
    
    [Header("Animal Conversion")]
    public float rabbitToMeerkatConversion = 0f;  // % chance to convert rabbit spawn to meerkat (0.0 - 1.0)
    public float meerkatToFoxConversion = 0f;     // % chance to convert meerkat spawn to fox (0.0 - 1.0)
    public float foxToBadgerConversion = 0f;      // % chance to convert fox spawn to badger (0.0 - 1.0)
    
    [Header("Combo System")]
    public int comboHitsToStart = 3;           // How many hits to trigger combo
    public float comboMultiplierIncrement = 2.0f;  // Multiplier when combo triggers
    public float goldPerCombo = 2.0f;          // Gold earned per combo (reduced by ComboStarter upgrade)
    public bool hasComboMissShield = false;    // If true, missing reduces combo by 1 instead of resetting
    public float perfectHitMultiplier = 1.0f;  // Multiplier for hitting within 0.4s (1.0=none, 1.5/2.0/2.5 with upgrades)
    public float critChance = 0f;              // Chance to deal 2x damage (0.0 - 1.0)
    public float timeAddedPerCombo = 0f;       // Seconds added to wave timer when combo triggers
    public float comboInterestRate = 0f;       // Multiplier increase per combo completed (0/0.5/0.75/1.0)
    public float comboInterestAccumulated = 0f; // Current accumulated interest for this wave (resets each wave)
    
    [Header("Currency")]
    public int currentGold = 0;            // Gold earned this run ($ for shop)
    public float fractionalGold = 0f;      // Accumulated fractional gold (for partial goldPerCombo values)
    
    [Header("Tier System")]
    // Dictionary to track tier levels for each upgrade type
    public Dictionary<UpgradeType, int> upgradeTiers = new Dictionary<UpgradeType, int>();
    
    [Header("Locked Upgrades")]
    // Set of upgrades that are locked out due to mutually exclusive choices
    public System.Collections.Generic.HashSet<UpgradeType> lockedUpgrades = new System.Collections.Generic.HashSet<UpgradeType>();
    
    public PlayerUpgrades()
    {
        // Default values set above
        // Initialize upgrade tiers dictionary
        upgradeTiers = new Dictionary<UpgradeType, int>();
        lockedUpgrades = new System.Collections.Generic.HashSet<UpgradeType>();
    }
    
    /// <summary>
    /// Get the current tier level for an upgrade (0 if not purchased yet)
    /// </summary>
    public int GetTierLevel(UpgradeType upgradeType)
    {
        if (upgradeTiers.ContainsKey(upgradeType))
        {
            return upgradeTiers[upgradeType];
        }
        return 0;
    }
    
    /// <summary>
    /// Increment the tier level for an upgrade
    /// </summary>
    public void IncrementTier(UpgradeType upgradeType)
    {
        if (upgradeTiers.ContainsKey(upgradeType))
        {
            upgradeTiers[upgradeType]++;
        }
        else
        {
            upgradeTiers[upgradeType] = 1;
        }
    }
    
    /// <summary>
    /// Check if an upgrade is locked out
    /// </summary>
    public bool IsUpgradeLocked(UpgradeType upgradeType)
    {
        return lockedUpgrades.Contains(upgradeType);
    }
    
    /// <summary>
    /// Lock out an upgrade (due to mutually exclusive choice)
    /// </summary>
    public void LockUpgrade(UpgradeType upgradeType)
    {
        lockedUpgrades.Add(upgradeType);
    }
    
    /// <summary>
    /// Clone the upgrades for modification
    /// </summary>
    public PlayerUpgrades Clone()
    {
        var clone = new PlayerUpgrades
        {
            basePointsPerRabbit = this.basePointsPerRabbit,
            basePointsPerMeerkat = this.basePointsPerMeerkat,
            basePointsPerFox = this.basePointsPerFox,
            basePointsPerBadger = this.basePointsPerBadger,
            waveDuration = this.waveDuration,
            minSpawnRate = this.minSpawnRate,
            maxSpawnRate = this.maxSpawnRate,
            maxActiveAnimals = this.maxActiveAnimals,
            rabbitToMeerkatConversion = this.rabbitToMeerkatConversion,
            meerkatToFoxConversion = this.meerkatToFoxConversion,
            foxToBadgerConversion = this.foxToBadgerConversion,
            comboHitsToStart = this.comboHitsToStart,
            comboMultiplierIncrement = this.comboMultiplierIncrement,
            goldPerCombo = this.goldPerCombo,
            hasComboMissShield = this.hasComboMissShield,
            perfectHitMultiplier = this.perfectHitMultiplier,
            critChance = this.critChance,
            timeAddedPerCombo = this.timeAddedPerCombo,
            comboInterestRate = this.comboInterestRate,
            comboInterestAccumulated = this.comboInterestAccumulated,
            currentGold = this.currentGold,
            fractionalGold = this.fractionalGold,
            upgradeTiers = new Dictionary<UpgradeType, int>(this.upgradeTiers),
            lockedUpgrades = new System.Collections.Generic.HashSet<UpgradeType>(this.lockedUpgrades)
        };
        return clone;
    }
    
    /// <summary>
    /// Award gold with proper fractional handling
    /// </summary>
    public int AwardGold(float amount)
    {
        fractionalGold += amount;
        int wholeGold = Mathf.FloorToInt(fractionalGold);
        fractionalGold -= wholeGold;
        currentGold += wholeGold;
        return wholeGold;
    }
}

