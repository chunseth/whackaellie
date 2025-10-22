using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual display of lives using sprites that change when lives are lost
/// </summary>
public class LivesDisplay : MonoBehaviour
{
    [Header("Life Sprites")]
    [SerializeField] private Sprite lifeSprite;      // Sprite for active life
    [SerializeField] private Sprite lostLifeSprite;  // Sprite for lost life
    
    [Header("Life Image References")]
    [SerializeField] private Image life1Image;
    [SerializeField] private Image life2Image;
    [SerializeField] private Image life3Image;
    
    private Image[] lifeImages;
    
    private void Awake()
    {
        // Store references in array for easy access
        lifeImages = new Image[] { life1Image, life2Image, life3Image };
    }
    
    private void Start()
    {
        // Initialize all lives to full
        UpdateLivesDisplay(3);
    }
    
    /// <summary>
    /// Updates the visual display based on current lives
    /// </summary>
    /// <param name="currentLives">Number of lives remaining (0-3)</param>
    public void UpdateLivesDisplay(int currentLives)
    {
        // Clamp to valid range
        currentLives = Mathf.Clamp(currentLives, 0, 3);
        
        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null)
            {
                // If this life index is less than current lives, show full life
                // Otherwise show lost life
                if (i < currentLives)
                {
                    lifeImages[i].sprite = lifeSprite;
                }
                else
                {
                    lifeImages[i].sprite = lostLifeSprite;
                }
            }
        }
    }
    
    /// <summary>
    /// Sets the sprites to use for lives display
    /// </summary>
    public void SetSprites(Sprite life, Sprite lostLife)
    {
        lifeSprite = life;
        lostLifeSprite = lostLife;
    }
}

