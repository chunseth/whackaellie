using UnityEngine;

/// <summary>
/// ScriptableObject to hold all animal data in one centralized place
/// Create via: Assets > Create > WhackAEllie > Animal Database
/// </summary>
[CreateAssetMenu(fileName = "AnimalDatabase", menuName = "WhackAEllie/Animal Database", order = 1)]
public class AnimalDatabase : ScriptableObject
{
    [Header("Animal Configurations")]
    public AnimalData[] animals;

    /// <summary>
    /// Gets a random animal from the database
    /// </summary>
    public AnimalData GetRandomAnimal()
    {
        if (animals != null && animals.Length > 0)
        {
            int randomIndex = Random.Range(0, animals.Length);
            return animals[randomIndex];
        }
        return null;
    }
    
    /// <summary>
    /// Gets a random animal that's allowed in the specified level
    /// </summary>
    public AnimalData GetRandomAnimalForLevel(LevelData levelData)
    {
        if (levelData == null || levelData.allowedAnimals == null || levelData.allowedAnimals.Length == 0)
        {
            return GetRandomAnimal();
        }
        
        // Filter animals by level's allowed types
        System.Collections.Generic.List<AnimalData> allowedAnimals = new System.Collections.Generic.List<AnimalData>();
        
        foreach (AnimalData animal in animals)
        {
            if (animal != null && levelData.IsAnimalAllowed(animal.animalType))
            {
                allowedAnimals.Add(animal);
            }
        }
        
        // Return random from filtered list
        if (allowedAnimals.Count > 0)
        {
            int randomIndex = Random.Range(0, allowedAnimals.Count);
            return allowedAnimals[randomIndex];
        }
        
        // Fallback to any animal if filtering resulted in no matches
        return GetRandomAnimal();
    }

    /// <summary>
    /// Gets an animal by type
    /// </summary>
    public AnimalData GetAnimalByType(AnimalType type)
    {
        if (animals != null)
        {
            foreach (AnimalData animal in animals)
            {
                if (animal.animalType == type)
                {
                    return animal;
                }
            }
        }
        return null;
    }
}

