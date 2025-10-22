using UnityEngine;

/// <summary>
/// Defines which animals appear in each level
/// </summary>
[System.Serializable]
public class LevelData
{
    [Header("Level Info")]
    public int levelNumber;
    public string levelName;
    
    [Header("Animals in This Level")]
    public AnimalType[] allowedAnimals;
    
    [Header("Level Settings")]
    public int targetScore = 100;  // Score needed to complete level
    public float gameDuration = 60f;  // Time limit for this level
    
    public LevelData(int number, string name, AnimalType[] animals, int target = 100, float duration = 60f)
    {
        levelNumber = number;
        levelName = name;
        allowedAnimals = animals;
        targetScore = target;
        gameDuration = duration;
    }
    
    /// <summary>
    /// Checks if an animal type is allowed in this level
    /// Removed System.Linq dependency for iOS/IL2CPP compatibility
    /// </summary>
    public bool IsAnimalAllowed(AnimalType type)
    {
        if (allowedAnimals == null || allowedAnimals.Length == 0)
            return false;
            
        for (int i = 0; i < allowedAnimals.Length; i++)
        {
            if (allowedAnimals[i] == type)
                return true;
        }
        
        return false;
    }
}

/// <summary>
/// ScriptableObject to manage all level configurations
/// Create via: Assets > Create > WhackAEllie > Level Configuration
/// </summary>
[CreateAssetMenu(fileName = "LevelConfiguration", menuName = "WhackAEllie/Level Configuration", order = 2)]
public class LevelConfiguration : ScriptableObject
{
    [Header("Level Progression")]
    public LevelData[] levels;
    
    /// <summary>
    /// Gets level data by level number (1-based)
    /// </summary>
    public LevelData GetLevel(int levelNumber)
    {
        if (levels != null && levelNumber > 0 && levelNumber <= levels.Length)
        {
            return levels[levelNumber - 1];
        }
        return null;
    }
    
    /// <summary>
    /// Gets the total number of levels
    /// </summary>
    public int GetTotalLevels()
    {
        return levels != null ? levels.Length : 0;
    }
    
    /// <summary>
    /// Initializes default 4-level progression
    /// </summary>
    public void InitializeDefaultLevels()
    {
        levels = new LevelData[4];
        
        // Level 1: Rabbits only
        levels[0] = new LevelData(
            1, 
            "Bunny Basics",
            new AnimalType[] { AnimalType.Rabbit },
            50,   // Lower target since only bunnies (5 pts each)
            10f
        );
        
        // Level 2: Rabbits + Meerkats
        levels[1] = new LevelData(
            2,
            "Meerkat Mayhem",
            new AnimalType[] { AnimalType.Rabbit, AnimalType.Meerkat, AnimalType.Snake },
            120,
            15f
        );
        
        // Level 3: Rabbits + Meerkats + Foxes
        levels[2] = new LevelData(
            3,
            "Foxy Challenge",
            new AnimalType[] { AnimalType.Rabbit, AnimalType.Meerkat, AnimalType.Fox, AnimalType.Snake },
            200,
            20f
        );
        
        // Level 4: Meerkats + Foxes + Badgers
        levels[3] = new LevelData(
            4,
            "Badger Bash",
            new AnimalType[] { AnimalType.Meerkat, AnimalType.Fox, AnimalType.Badger, AnimalType.Snake },
            300,
            20f
        );
    }
}

