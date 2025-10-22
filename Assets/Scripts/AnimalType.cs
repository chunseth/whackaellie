using UnityEngine;

/// <summary>
/// Enum for different animal types in the game
/// </summary>
public enum AnimalType
{
    Rabbit,
    Meerkat,
    Fox,
    Badger,
    Snake
}

/// <summary>
/// Data for each animal type including sprites and point values
/// </summary>
[System.Serializable]
public class AnimalData
{
    [Header("Animal Info")]
    public AnimalType animalType;
    public string animalName;
    
    [Header("Sprites")]
    public Sprite normalSprite;  // Sprite when popping out
    public Sprite hitSprite;     // Sprite when getting hit
    
    [Header("Audio")]
    public AudioClip[] hitSoundEffects;  // Array of sound effects to choose from when hit
    
    [Header("Game Settings")]
    public int pointValue;
    public float visibleDuration = 2f;  // How long this animal stays visible
    
    public AnimalData(AnimalType type, string name, int points, float duration = 2f)
    {
        animalType = type;
        animalName = name;
        pointValue = points;
        visibleDuration = duration;
    }
    
    /// <summary>
    /// Gets a random hit sound effect from this animal's sound array
    /// </summary>
    public AudioClip GetRandomHitSound()
    {
        if (hitSoundEffects != null && hitSoundEffects.Length > 0)
        {
            int randomIndex = Random.Range(0, hitSoundEffects.Length);
            return hitSoundEffects[randomIndex];
        }
        return null;
    }
}

