using UnityEngine;

/// <summary>
/// Types of upgrades available in the shop
/// </summary>
public enum UpgradeType
{
    RabbitPoints1,      // +1 rabbit points
    RabbitPoints3,      // +3 rabbit points
    MeerkatPoints1,     // +1 meerkat points
    MeerkatPoints3,     // +3 meerkat points
    SpawnRate,          // +1.0/s spawn rate
    RoundLength,        // +2s round length
    ComboStarter,       // -1 combo starter
    ComboMultiplier,    // +0.5x combo multiplier
    RabbitToMeerkat,    // Convert % of rabbits to meerkats (15% per tier)
    MeerkatToFox,       // Convert % of meerkats to foxes (15% per tier)
    FoxToBadger,        // Convert % of foxes to badgers (15% per tier)
    ComboMissShield,    // Miss reduces combo by 1 instead of resetting
    PerfectHitBonus,    // 1.5x points for hits within 0.4s of animal appearing
    CritChance,         // +5% crit chance per tier for 2x points (max 25%)
    TimePerCombo,       // Adds time when combo triggers, but +1 combo starter (0.5s/1s/2s per tier)
    ComboInterest       // Each combo increases multiplier (0.5x/0.75x/1.0x per combo)
}

/// <summary>
/// Holds all data for a single upgrade type
/// </summary>
[System.Serializable]
public class UpgradeDefinition
{
    [Header("Basic Info")]
    public UpgradeType upgradeType;
    public string displayName;
    [TextArea(3, 6)]
    public string description;
    
    [Header("Cost & Visual")]
    public int baseCost;  // Base cost in Round 1
    public Sprite icon;
    
    [Header("Price Scaling")]
    [Tooltip("Cost multiplier per round. Round 1: base cost, Round 2: base × this, Round 3: base × this², etc.")]
    public float priceScalingPerRound = 1.5f;  // Default: 1.5x cost increase per round
    
    [Header("Tier System")]
    [Tooltip("Maximum number of tiers for this upgrade. Set to -1 for unlimited (for points upgrades).")]
    public int maxTiers = 1;
    
    [Tooltip("Is this a points upgrade? If true, displays 'X + Y' text instead of tier images and allows unlimited purchases.")]
    public bool isPointsUpgrade = false;
    
    [Header("Spawn Rate Per Wave")]
    [Tooltip("Spawn weight for each wave (0-14). Higher values = more likely to appear. Set to 0 to disable this upgrade from appearing in that wave.")]
    public float[] spawnWeightPerWave = new float[15] { 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f };  // Weight for each of the 15 waves (5 rounds × 3 waves)
    
    /// <summary>
    /// Get spawn weight for a specific wave (0-based index)
    /// </summary>
    public float GetSpawnWeightForWave(int waveIndex)
    {
        if (waveIndex >= 0 && waveIndex < spawnWeightPerWave.Length)
        {
            return spawnWeightPerWave[waveIndex];
        }
        else if (waveIndex >= spawnWeightPerWave.Length && spawnWeightPerWave.Length > 0)
        {
            // If wave exceeds array size, use the last element's weight
            return spawnWeightPerWave[spawnWeightPerWave.Length - 1];
        }
        return 0f;  // Invalid wave or empty array
    }
    
    [Header("Special Conditions")]
    public bool limitedPerWave = false;  // Can only be purchased once per wave
    public bool requiresMinValue = false;  // Requires a minimum value to purchase
    public int minRequiredValue = 0;  // The minimum value needed
    
    [Header("Mutually Exclusive Upgrades")]
    [Tooltip("List of upgrade types that become locked out if this upgrade is purchased. Creates strategic choice between upgrade paths.")]
    public UpgradeType[] locksOutUpgrades;  // Upgrades that can't be purchased if this one is bought
    
    /// <summary>
    /// Get the cost for this upgrade based on current round
    /// </summary>
    public int GetCostForRound(int roundNumber)
    {
        if (roundNumber <= 1)
        {
            return baseCost;
        }
        
        // Apply exponential scaling: baseCost × (priceScalingPerRound ^ (roundNumber - 1))
        float scaledCost = baseCost * Mathf.Pow(priceScalingPerRound, roundNumber - 1);
        return Mathf.RoundToInt(scaledCost);
    }
}

/// <summary>
/// Database of all available upgrades in the game
/// </summary>
[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "WhackAEllie/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    [Header("All Available Upgrades")]
    public UpgradeDefinition[] upgrades;
    
    /// <summary>
    /// Get upgrade definition by type
    /// </summary>
    public UpgradeDefinition GetUpgrade(UpgradeType type)
    {
        foreach (var upgrade in upgrades)
        {
            if (upgrade.upgradeType == type)
            {
                return upgrade;
            }
        }
        
        Debug.LogWarning($"[UpgradeDatabase] Upgrade type {type} not found in database!");
        return null;
    }
    
    /// <summary>
    /// Get a random selection of upgrade types based on their spawn weights for a specific wave
    /// </summary>
    public UpgradeType[] GetRandomUpgrades(int count, int waveNumber, PlayerUpgrades playerUpgrades, System.Random random = null)
    {
        if (random == null)
        {
            random = new System.Random();
        }
        
        // Convert wave number (1-based) to wave index (0-based)
        int waveIndex = waveNumber - 1;
        
        // Filter out upgrades with 0 weight, maxed out upgrades, AND locked upgrades
        var availableUpgrades = new System.Collections.Generic.List<UpgradeDefinition>();
        foreach (var upgrade in upgrades)
        {
            float weight = upgrade.GetSpawnWeightForWave(waveIndex);
            
            // Skip if weight is 0
            if (weight <= 0) continue;
            
            // Skip if upgrade is locked out by previous purchase
            if (playerUpgrades != null && playerUpgrades.IsUpgradeLocked(upgrade.upgradeType))
            {
                Debug.Log($"[UpgradeDatabase] Excluding {upgrade.upgradeType} from shop - locked out by mutually exclusive upgrade");
                continue;
            }
            
            // Skip if upgrade is maxed out (unless it's a points upgrade with unlimited tiers)
            if (!upgrade.isPointsUpgrade && upgrade.maxTiers > 0)
            {
                int currentTier = playerUpgrades != null ? playerUpgrades.GetTierLevel(upgrade.upgradeType) : 0;
                if (currentTier >= upgrade.maxTiers)
                {
                    Debug.Log($"[UpgradeDatabase] Excluding {upgrade.upgradeType} from shop - maxed at tier {currentTier}/{upgrade.maxTiers}");
                    continue;
                }
            }
            
            availableUpgrades.Add(upgrade);
        }
        
        if (availableUpgrades.Count == 0)
        {
            Debug.LogWarning($"[UpgradeDatabase] No upgrades with spawn weight > 0 for wave {waveNumber}!");
            return new UpgradeType[0];
        }
        
        // Step 1: Separate guaranteed upgrades (weight >= 100) from random pool
        var guaranteedUpgrades = new System.Collections.Generic.List<UpgradeDefinition>();
        var randomPoolUpgrades = new System.Collections.Generic.List<UpgradeDefinition>();
        
        foreach (var upgrade in availableUpgrades)
        {
            float weight = upgrade.GetSpawnWeightForWave(waveIndex);
            if (weight >= 100f)
            {
                guaranteedUpgrades.Add(upgrade);
                Debug.Log($"[UpgradeDatabase] Guaranteed upgrade: {upgrade.upgradeType} (weight: {weight})");
            }
            else
            {
                randomPoolUpgrades.Add(upgrade);
            }
        }
        
        // Step 2: Add guaranteed upgrades first
        var selectedUpgrades = new System.Collections.Generic.List<UpgradeType>();
        foreach (var upgrade in guaranteedUpgrades)
        {
            selectedUpgrades.Add(upgrade.upgradeType);
        }
        
        // Step 3: Fill remaining slots with weighted random selection
        int slotsRemaining = count - selectedUpgrades.Count;
        int actualCount = UnityEngine.Mathf.Min(slotsRemaining, randomPoolUpgrades.Count);
        
        // Perform weighted random selection without replacement
        var remainingUpgrades = new System.Collections.Generic.List<UpgradeDefinition>(randomPoolUpgrades);
        
        for (int i = 0; i < actualCount; i++)
        {
            // Calculate total weight of remaining upgrades for this wave
            float totalWeight = 0f;
            foreach (var upgrade in remainingUpgrades)
            {
                totalWeight += upgrade.GetSpawnWeightForWave(waveIndex);
            }
            
            // Pick a random value within the total weight [0, totalWeight)
            float randomValue = (float)(random.NextDouble() * totalWeight);
            
            // Find which upgrade corresponds to this random value
            // Each upgrade occupies a range proportional to its weight
            float cumulativeWeight = 0f;
            UpgradeDefinition selectedUpgrade = null;
            
            foreach (var upgrade in remainingUpgrades)
            {
                float upgradeWeight = upgrade.GetSpawnWeightForWave(waveIndex);
                cumulativeWeight += upgradeWeight;
                
                // Use < instead of <= to avoid boundary bias
                // Random value is in [0, totalWeight), cumulative ranges are [0, w1), [w1, w1+w2), etc.
                if (randomValue < cumulativeWeight)
                {
                    selectedUpgrade = upgrade;
                    break;
                }
            }
            
            // Add selected upgrade and remove it from remaining pool
            if (selectedUpgrade != null)
            {
                selectedUpgrades.Add(selectedUpgrade.upgradeType);
                remainingUpgrades.Remove(selectedUpgrade);
            }
            else
            {
                // Fallback: if no upgrade selected (shouldn't happen), pick the last one
                Debug.LogWarning($"[UpgradeDatabase] No upgrade selected in weighted random (randomValue={randomValue}, totalWeight={totalWeight}). Using fallback.");
                if (remainingUpgrades.Count > 0)
                {
                    selectedUpgrade = remainingUpgrades[remainingUpgrades.Count - 1];
                    selectedUpgrades.Add(selectedUpgrade.upgradeType);
                    remainingUpgrades.Remove(selectedUpgrade);
                }
            }
        }
        
        // Sort selected upgrades by spawn weight (descending) so highest priority appears first
        selectedUpgrades.Sort((a, b) =>
        {
            float weightA = GetUpgrade(a)?.GetSpawnWeightForWave(waveIndex) ?? 0f;
            float weightB = GetUpgrade(b)?.GetSpawnWeightForWave(waveIndex) ?? 0f;
            return weightB.CompareTo(weightA); // Descending order (highest weight first)
        });
        
        return selectedUpgrades.ToArray();
    }
    
    /// <summary>
    /// Check if an upgrade can be afforded
    /// </summary>
    public bool CanAfford(UpgradeType type, int currentGold, PlayerUpgrades playerUpgrades, bool purchasedThisWave, int currentRound)
    {
        UpgradeDefinition upgrade = GetUpgrade(type);
        if (upgrade == null)
        {
            Debug.LogWarning($"[UpgradeDatabase] Upgrade {type} not found in database!");
            return false;
        }
        
        // Check if upgrade is locked out by mutually exclusive purchase
        if (playerUpgrades != null && playerUpgrades.IsUpgradeLocked(type))
        {
            Debug.Log($"[UpgradeDatabase] {type} not affordable - Locked out by mutually exclusive upgrade");
            return false;
        }
        
        // Check tier limit (skip if unlimited tiers, indicated by maxTiers = -1 or isPointsUpgrade)
        int currentTier = playerUpgrades.GetTierLevel(type);
        if (!upgrade.isPointsUpgrade && upgrade.maxTiers > 0 && currentTier >= upgrade.maxTiers)
        {
            Debug.Log($"[UpgradeDatabase] {type} not affordable - Max tier reached ({currentTier}/{upgrade.maxTiers})");
            return false;
        }
        
        // Check gold cost (scaled by round)
        int cost = upgrade.GetCostForRound(currentRound);
        if (currentGold < cost)
        {
            Debug.Log($"[UpgradeDatabase] {type} not affordable - Need ${cost}, have ${currentGold}");
            return false;
        }
        
        // Check limited per wave
        if (upgrade.limitedPerWave && purchasedThisWave)
        {
            Debug.Log($"[UpgradeDatabase] {type} not affordable - Limited per wave and already purchased this wave");
            return false;
        }
        
        // Check minimum value requirement (e.g., combo starter can't go below 1)
        if (upgrade.requiresMinValue)
        {
            if (type == UpgradeType.ComboStarter && playerUpgrades.comboHitsToStart <= upgrade.minRequiredValue)
            {
                Debug.Log($"[UpgradeDatabase] {type} not affordable - Combo starter at minimum ({playerUpgrades.comboHitsToStart} <= {upgrade.minRequiredValue})");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Apply the upgrade effect to player stats
    /// </summary>
    public void ApplyUpgradeEffect(UpgradeType type, RoguelikeManager roguelikeManager)
    {
        switch (type)
        {
            case UpgradeType.RabbitPoints1:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.basePointsPerRabbit += 1;
                });
                break;
                
            case UpgradeType.RabbitPoints3:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.basePointsPerRabbit += 3;
                });
                break;
                
            case UpgradeType.MeerkatPoints1:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.basePointsPerMeerkat += 1;
                });
                break;
                
            case UpgradeType.MeerkatPoints3:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.basePointsPerMeerkat += 3;
                });
                break;
                
            case UpgradeType.SpawnRate:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.minSpawnRate += 1.0f;
                    upgrades.maxSpawnRate += 1.0f;
                    upgrades.maxActiveAnimals += 2;
                });
                break;
                
            case UpgradeType.RoundLength:
                roguelikeManager.ApplyUpgrade(upgrades => upgrades.waveDuration += 2f);
                break;
                
            case UpgradeType.ComboStarter:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.comboHitsToStart -= 1;
                    upgrades.goldPerCombo -= 0.5f;  // Tradeoff: faster combos but less gold
                });
                break;
                
            case UpgradeType.ComboMultiplier:
                roguelikeManager.ApplyUpgrade(upgrades => upgrades.comboMultiplierIncrement += 0.5f);
                break;
                
            case UpgradeType.RabbitToMeerkat:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.rabbitToMeerkatConversion += 0.15f; // +15% per tier
                });
                break;
                
            case UpgradeType.MeerkatToFox:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.meerkatToFoxConversion += 0.15f; // +15% per tier
                });
                break;
                
            case UpgradeType.FoxToBadger:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.foxToBadgerConversion += 0.15f; // +15% per tier
                });
                break;
                
            case UpgradeType.ComboMissShield:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.hasComboMissShield = true; // Grant combo miss shield
                });
                break;
                
            case UpgradeType.PerfectHitBonus:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.perfectHitMultiplier += 0.5f; // +0.5x per tier (1.0 → 1.5 → 2.0 → 2.5)
                });
                break;
                
            case UpgradeType.CritChance:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    upgrades.critChance += 0.05f; // +5% crit chance per tier
                });
                break;
                
            case UpgradeType.TimePerCombo:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    // Tier 1: +0.5s, Tier 2: +1.0s, Tier 3: +2.0s
                    int currentTier = upgrades.GetTierLevel(UpgradeType.TimePerCombo);
                    if (currentTier == 0)
                    {
                        upgrades.timeAddedPerCombo = 0.5f;
                    }
                    else if (currentTier == 1)
                    {
                        upgrades.timeAddedPerCombo = 1.0f;
                    }
                    else if (currentTier == 2)
                    {
                        upgrades.timeAddedPerCombo = 2.0f;
                    }
                    
                    // Tradeoff: +1 combo starter (makes combos harder)
                    upgrades.comboHitsToStart += 1;
                });
                break;
                
            case UpgradeType.ComboInterest:
                roguelikeManager.ApplyUpgrade(upgrades =>
                {
                    // Tier 1: +0.5x, Tier 2: +0.75x, Tier 3: +1.0x per combo
                    int currentTier = upgrades.GetTierLevel(UpgradeType.ComboInterest);
                    if (currentTier == 0)
                    {
                        upgrades.comboInterestRate = 0.5f;
                    }
                    else if (currentTier == 1)
                    {
                        upgrades.comboInterestRate = 0.75f;
                    }
                    else if (currentTier == 2)
                    {
                        upgrades.comboInterestRate = 1.0f;
                    }
                });
                break;
        }
    }
}

