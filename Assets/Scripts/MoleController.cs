using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class MoleController : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private RectTransform moleImage;
    [SerializeField] private Image moleImageComponent;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Animal Data")]
    [SerializeField] private AnimalDatabase animalDatabase;
    [SerializeField] private AnimalData[] animalDataArray;
    
    [Header("Settings")]
    [SerializeField] private float popUpDistance = 100f;
    [SerializeField] private float animationDuration = 0.4f;
    [SerializeField] private float visibleDuration = 2f;
    [SerializeField] private float hitSpriteDuration = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.5f;

    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private bool isVisible = false;
    private bool isAnimating = false;
    private bool wasHit = false;  // Track if animal was hit or escaped
    private Coroutine popUpCoroutine;
    private Coroutine whackCoroutine;
    private AnimalData currentAnimal;
    private bool forceSnakeSpawn = false;  // Set by spawner for snake events

    private void Start()
    {
        if (moleImage != null)
        {
            hiddenPosition = moleImage.anchoredPosition;
            visiblePosition = hiddenPosition + Vector2.up * popUpDistance;
            
            // Ensure mole starts hidden
            moleImage.anchoredPosition = hiddenPosition;
        }
        
        // Get the Image component if not assigned
        if (moleImageComponent == null && moleImage != null)
        {
            moleImageComponent = moleImage.GetComponent<Image>();
        }
        
        // Get or add AudioSource component if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            // Configure AudioSource for sound effects
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
        
        // Initialize animal data array with default values if empty
        if (animalDataArray == null || animalDataArray.Length == 0)
        {
            InitializeDefaultAnimalData();
        }
    }
    
    /// <summary>
    /// Initialize default animal data with placeholder point values
    /// Different animals have different visible durations for skill-based gameplay
    /// Faster = Harder to hit but worth fewer points
    /// Slower = Easier to hit but worth more points
    /// </summary>
    private void InitializeDefaultAnimalData()
    {
        animalDataArray = new AnimalData[5];
        animalDataArray[0] = new AnimalData(AnimalType.Rabbit, "Rabbit", 5, 1.0f);      // Fast & low value
        animalDataArray[1] = new AnimalData(AnimalType.Meerkat, "Meerkat", 10, 1.5f);   // Medium speed
        animalDataArray[2] = new AnimalData(AnimalType.Fox, "Fox", 15, 1.2f);           // Medium-fast
        animalDataArray[3] = new AnimalData(AnimalType.Badger, "Badger", 25, 2.5f);     // Slow & high value
        animalDataArray[4] = new AnimalData(AnimalType.Snake, "Snake", 20, 1.8f);       // Medium-slow
    }

    /// <summary>
    /// Makes the mole pop up from the hole with a random animal
    /// </summary>
    public void PopUp()
    {
        // Don't allow pop up if already visible, animating, OR whack sequence is running
        if (!isVisible && !isAnimating && whackCoroutine == null)
        {
            // Ensure we're starting from hidden position
            if (moleImage != null)
            {
                moleImage.anchoredPosition = hiddenPosition;
            }
            
            // Select a random animal
            SelectRandomAnimal();
            wasHit = false;  // Reset hit status
            forceSnakeSpawn = false;  // Reset flag
            
            if (popUpCoroutine != null)
            {
                StopCoroutine(popUpCoroutine);
                popUpCoroutine = null;
            }
            
            popUpCoroutine = StartCoroutine(PopUpSequence());
        }
    }
    
    /// <summary>
    /// Forces this hole to spawn a snake (called by snake event system)
    /// </summary>
    public void PopUpSnake()
    {
        forceSnakeSpawn = true;
        PopUp();
    }
    
    /// <summary>
    /// Quick preview animation for pre-wave sequence (pop up briefly then slide down)
    /// </summary>
    public void PopUpPreview()
    {
        if (!isVisible && !isAnimating && whackCoroutine == null)
        {
            if (moleImage != null)
            {
                moleImage.anchoredPosition = hiddenPosition;
            }
            
            SelectRandomAnimal();
            wasHit = false;
            forceSnakeSpawn = false;
            
            if (popUpCoroutine != null)
            {
                StopCoroutine(popUpCoroutine);
            }
            
            popUpCoroutine = StartCoroutine(PreviewPopUpSequence());
        }
    }
    
    /// <summary>
    /// Preview pop up sequence: quick up and down for pre-wave animation
    /// </summary>
    private IEnumerator PreviewPopUpSequence()
    {
        // Calculate halfway position
        Vector2 halfwayPosition = (hiddenPosition + visiblePosition) / 2f;
        
        // Slide up halfway
        yield return StartCoroutine(SlideMole(hiddenPosition, halfwayPosition));
        isVisible = true;
        
        // Hold at halfway point (0.7s)
        yield return new WaitForSeconds(0.7f);
        
        // Slide back down
        yield return StartCoroutine(SlideMole(halfwayPosition, hiddenPosition));
        isVisible = false;
        
        // Notify spawner
        NotifySnakeHidden();
        
        popUpCoroutine = null;
    }
    
    /// <summary>
    /// Selects a random animal and sets its sprite (respects current level restrictions and snake limits)
    /// </summary>
    private void SelectRandomAnimal()
    {
        // Check if we're in roguelike mode first
        if (RoguelikeManager.Instance != null)
        {
            SelectRandomAnimalRoguelike();
            return;
        }
        
        // Get current level data from GameManager
        LevelData currentLevelData = null;
        if (GameManager.Instance != null)
        {
            currentLevelData = GameManager.Instance.GetCurrentLevelData();
        }
        
        // Check if snakes are allowed (ask spawner)
        MoleSpawner spawner = FindObjectOfType<MoleSpawner>();
        bool canSpawnSnake = spawner != null && spawner.CanSpawnSnake();
        
        // Try to get from database first (with level filtering), then fall back to array
        if (animalDatabase != null)
        {
            if (currentLevelData != null)
            {
                currentAnimal = animalDatabase.GetRandomAnimalForLevel(currentLevelData);
            }
            else
            {
                currentAnimal = animalDatabase.GetRandomAnimal();
            }
        }
        else if (animalDataArray != null && animalDataArray.Length > 0)
        {
            // Fallback: filter array by level if level data exists
            if (currentLevelData != null && currentLevelData.allowedAnimals != null && currentLevelData.allowedAnimals.Length > 0)
            {
                System.Collections.Generic.List<AnimalData> allowedAnimals = new System.Collections.Generic.List<AnimalData>();
                foreach (AnimalData animal in animalDataArray)
                {
                    if (animal != null && currentLevelData.IsAnimalAllowed(animal.animalType))
                    {
                        allowedAnimals.Add(animal);
                    }
                }
                
                if (allowedAnimals.Count > 0)
                {
                    int randomIndex = Random.Range(0, allowedAnimals.Count);
                    currentAnimal = allowedAnimals[randomIndex];
                }
                else
                {
                    // No matches, use any animal
                    int randomIndex = Random.Range(0, animalDataArray.Length);
                    currentAnimal = animalDataArray[randomIndex];
                }
            }
            else
            {
                // No level restrictions, use any animal
                int randomIndex = Random.Range(0, animalDataArray.Length);
                currentAnimal = animalDataArray[randomIndex];
            }
        }
        
        // If we selected a snake but can't spawn one, reroll to a non-snake
        if (currentAnimal != null && currentAnimal.animalType == AnimalType.Snake && !canSpawnSnake)
        {
            currentAnimal = GetNonSnakeAnimal(currentLevelData);
        }
        
        // Notify spawner if we're spawning a snake
        if (currentAnimal != null && currentAnimal.animalType == AnimalType.Snake && spawner != null)
        {
            spawner.OnSnakeSpawned();
        }
        
        // Set the normal sprite
        if (moleImageComponent != null && currentAnimal != null && currentAnimal.normalSprite != null)
        {
            moleImageComponent.sprite = currentAnimal.normalSprite;
        }
    }
    
    /// <summary>
    /// Selects a random animal for roguelike mode
    /// </summary>
    private void SelectRandomAnimalRoguelike()
    {
        AnimalType selectedType = AnimalType.Rabbit; // Default to rabbit
        MoleSpawner spawner = FindObjectOfType<MoleSpawner>();
        
        // Check if this is a forced snake spawn from snake event system
        if (forceSnakeSpawn)
        {
            selectedType = AnimalType.Snake;
        }
        // Check which wave we're on from RoguelikeManager
        else if (RoguelikeManager.Instance != null)
        {
            var waveData = RoguelikeManager.Instance.GetCurrentWaveData();
            
            if (waveData != null && waveData.allowedAnimals != null && waveData.allowedAnimals.Count > 0)
            {
                bool canSpawnRabbit = spawner != null && spawner.CanSpawnRabbit();
                bool canSpawnMeerkat = spawner != null && spawner.CanSpawnMeerkat();
                
                // Check what animals are allowed
                bool hasMeerkats = waveData.allowedAnimals.Contains(AnimalType.Meerkat);
                
                if (hasMeerkats)
                {
                    // Round 2: Use wave-specific probability
                    float meerkatChance = waveData.meerkatSpawnChance;
                    float roll = Random.value;
                    
                    if (roll < meerkatChance && canSpawnMeerkat)
                    {
                        selectedType = AnimalType.Meerkat;
                    }
                    else if (canSpawnRabbit)
                    {
                        selectedType = AnimalType.Rabbit;
                    }
                    else if (canSpawnMeerkat)
                    {
                        // Rabbit limit hit, spawn meerkat
                        selectedType = AnimalType.Meerkat;
                    }
                }
                else
                {
                    // Just rabbits (snakes handled by parallel system)
                    selectedType = AnimalType.Rabbit;
                }
            }
        }
        
        // Find the animal data for selected type
        if (animalDatabase != null)
        {
            foreach (var animal in animalDatabase.animals)
            {
                if (animal != null && animal.animalType == selectedType)
                {
                    currentAnimal = animal;
                    break;
                }
            }
        }
        
        // Fallback to array
        if (currentAnimal == null && animalDataArray != null)
        {
            foreach (var animal in animalDataArray)
            {
                if (animal != null && animal.animalType == selectedType)
                {
                    currentAnimal = animal;
                    break;
                }
            }
        }
        
        // Notify spawner of what type we spawned
        if (spawner != null)
        {
            if (selectedType == AnimalType.Snake)
            {
                spawner.OnSnakeSpawned();
            }
            else if (selectedType == AnimalType.Rabbit)
            {
                spawner.OnRabbitSpawned();
            }
            else if (selectedType == AnimalType.Meerkat)
            {
                spawner.OnMeerkatSpawned();
            }
        }
        
        // Set the sprite
        if (moleImageComponent != null && currentAnimal != null && currentAnimal.normalSprite != null)
        {
            moleImageComponent.sprite = currentAnimal.normalSprite;
        }
        
        Debug.Log($"[Roguelike] Selected animal: {currentAnimal?.animalName ?? "None"} ({selectedType})");
    }
    
    /// <summary>
    /// Gets a non-snake animal when snake limit is reached
    /// </summary>
    private AnimalData GetNonSnakeAnimal(LevelData levelData)
    {
        System.Collections.Generic.List<AnimalData> nonSnakeAnimals = new System.Collections.Generic.List<AnimalData>();
        
        if (animalDatabase != null && animalDatabase.animals != null)
        {
            foreach (AnimalData animal in animalDatabase.animals)
            {
                if (animal != null && animal.animalType != AnimalType.Snake)
                {
                    if (levelData == null || levelData.IsAnimalAllowed(animal.animalType))
                    {
                        nonSnakeAnimals.Add(animal);
                    }
                }
            }
        }
        else if (animalDataArray != null)
        {
            foreach (AnimalData animal in animalDataArray)
            {
                if (animal != null && animal.animalType != AnimalType.Snake)
                {
                    if (levelData == null || levelData.IsAnimalAllowed(animal.animalType))
                    {
                        nonSnakeAnimals.Add(animal);
                    }
                }
            }
        }
        
        if (nonSnakeAnimals.Count > 0)
        {
            return nonSnakeAnimals[Random.Range(0, nonSnakeAnimals.Count)];
        }
        
        return currentAnimal; // Fallback (shouldn't happen)
    }

    /// <summary>
    /// Forces the mole to hide immediately (used when game ends)
    /// </summary>
    public void ForceHide()
    {
        // Notify spawner if this was a snake
        NotifySnakeHidden();
        
        if (popUpCoroutine != null)
        {
            StopCoroutine(popUpCoroutine);
            popUpCoroutine = null;
        }
        
        if (whackCoroutine != null)
        {
            StopCoroutine(whackCoroutine);
            whackCoroutine = null;
        }
        
        isVisible = false;
        isAnimating = false;
        wasHit = false;
        
        if (moleImage != null)
        {
            moleImage.anchoredPosition = hiddenPosition;
        }
    }

    /// <summary>
    /// Handles the pop up sequence: slide up, wait, slide down
    /// </summary>
    private IEnumerator PopUpSequence()
    {
        // Slide up
        yield return StartCoroutine(SlideMole(hiddenPosition, visiblePosition));
        isVisible = true;
        
        // Wait for animal-specific visible duration (or fallback to default)
        float waitTime = (currentAnimal != null) ? currentAnimal.visibleDuration : visibleDuration;
        yield return new WaitForSeconds(waitTime);
        
        // Slide down if still visible (not whacked)
        if (isVisible)
        {
            // Start sliding down - player can still hit during this animation!
            yield return StartCoroutine(SlideMole(visiblePosition, hiddenPosition));
            
            // Only NOW check if animal escaped (after slide down completes)
            // If wasHit is still false, the animal got away
            if (isVisible)
            {
                OnAnimalEscaped();
            }
            
            isVisible = false;
        }
        
        // Notify spawner when snake is hidden
        NotifySnakeHidden();
        
        popUpCoroutine = null;
    }

    /// <summary>
    /// Slides the mole between two positions with linear interpolation
    /// </summary>
    private IEnumerator SlideMole(Vector2 fromPosition, Vector2 toPosition)
    {
        isAnimating = true;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            
            if (moleImage != null)
            {
                moleImage.anchoredPosition = Vector2.Lerp(fromPosition, toPosition, t);
            }
            
            yield return null;
        }

        if (moleImage != null)
        {
            moleImage.anchoredPosition = toPosition;
        }
        
        isAnimating = false;
    }

    /// <summary>
    /// Handles pointer click events (tapping the mole)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Check roguelike mode first
        if (RoguelikeManager.Instance != null)
        {
            Debug.Log($"[Roguelike Click] {gameObject.name} - IsVisible: {isVisible}, Animal: {currentAnimal?.animalName}");
            
            // Only allow clicks if visible AND wave is active
            if (isVisible && RoguelikeManager.Instance.IsWaveActive())
            {
                Whack();
            }
            else if (isVisible)
            {
                Debug.Log("[Roguelike Click] Wave not active, ignoring click");
            }
            return;
        }
        
        // Standard game manager mode
        Debug.Log($"Click detected on {gameObject.name}! IsVisible: {isVisible}, GameActive: {(GameManager.Instance != null ? GameManager.Instance.IsGameActive() : false)}");
        
        // Only respond to clicks when visible and game is active
        if (isVisible && GameManager.Instance != null && GameManager.Instance.IsGameActive())
        {
            Whack();
        }
    }

    /// <summary>
    /// Called when the mole is successfully whacked
    /// </summary>
    private void Whack()
    {
        wasHit = true;  // Mark as hit
        
        // Check if we're in roguelike mode
        if (RoguelikeManager.Instance != null)
        {
            // Roguelike mode scoring - pass this GameObject's RectTransform (parent of mole image)
            if (currentAnimal != null)
            {
                RectTransform thisRect = GetComponent<RectTransform>();
                RoguelikeManager.Instance.OnAnimalHit(currentAnimal.animalType, thisRect);
            }
        }
        // Check if this is a snake in survival mode
        else if (GameManager.Instance != null)
        {
            bool isSnakeInSurvival = currentAnimal != null && 
                                     currentAnimal.animalType == AnimalType.Snake && 
                                     GameManager.Instance.GetGameMode() == GameMode.SurvivalMode;
            
            if (isSnakeInSurvival)
            {
                // Hitting snake in survival mode
                SurvivalModeSettings survivalSettings = GameManager.Instance.GetSurvivalSettings();
                
                if (survivalSettings != null && survivalSettings.loseLiveOnSnakeHit)
                {
                    // Lose a life for hitting snake
                    GameManager.Instance.LoseLife();
                    
                    // Optionally give penalty points
                    if (survivalSettings.snakeGivesPoints)
                    {
                        GameManager.Instance.AddScore(survivalSettings.snakePenaltyPoints);
                    }
                }
            }
            else
            {
                // Normal scoring for other animals (or snake in level mode)
                if (currentAnimal != null)
                {
                    GameManager.Instance.AddScore(currentAnimal.pointValue);
                }
            }
        }

        // Stop the pop up sequence
        if (popUpCoroutine != null)
        {
            StopCoroutine(popUpCoroutine);
            popUpCoroutine = null;
        }

        // Show hit sprite and slide down
        isVisible = false;
        
        if (whackCoroutine != null)
        {
            StopCoroutine(whackCoroutine);
        }
        whackCoroutine = StartCoroutine(WhackSequence());
    }
    
    /// <summary>
    /// Called when an animal escapes without being hit
    /// </summary>
    private void OnAnimalEscaped()
    {
        if (wasHit)
        {
            return;  // Animal was hit, don't penalize
        }
        
        // Check if we're in roguelike mode
        if (RoguelikeManager.Instance != null)
        {
            // Roguelike mode - notify manager of miss with position for miss text
            if (currentAnimal != null)
            {
                RectTransform thisRect = GetComponent<RectTransform>();
                RoguelikeManager.Instance.OnAnimalMissed(currentAnimal.animalType, thisRect);
            }
        }
        // In survival mode, lose a life when animal escapes
        else if (GameManager.Instance != null && 
            GameManager.Instance.GetGameMode() == GameMode.SurvivalMode)
        {
            // IMPORTANT: Snakes escaping is GOOD - don't penalize!
            // Only lose a life when good animals (rabbit, meerkat, fox, badger) escape
            bool isSnake = currentAnimal != null && currentAnimal.animalType == AnimalType.Snake;
            
            if (!isSnake)  // Only penalize if NOT a snake
            {
                SurvivalModeSettings survivalSettings = GameManager.Instance.GetSurvivalSettings();
                
                if (survivalSettings != null && survivalSettings.loseLiveOnMiss)
                {
                    GameManager.Instance.LoseLife();
                    Debug.Log($"{currentAnimal?.animalName ?? "Animal"} escaped! Lost a life.");
                }
            }
            else
            {
                Debug.Log("Snake escaped - no penalty (you dodged it!)");
            }
        }
    }
    
    /// <summary>
    /// Handles the whack animation: show hit sprite, play sound, then slide down
    /// </summary>
    private IEnumerator WhackSequence()
    {
        // Show hit sprite
        if (moleImageComponent != null && currentAnimal != null && currentAnimal.hitSprite != null)
        {
            moleImageComponent.sprite = currentAnimal.hitSprite;
        }
        
        // Play random hit sound effect for this animal
        PlayHitSound();
        
        // Brief pause to show hit sprite
        yield return new WaitForSeconds(hitSpriteDuration);
        
        // Slide down
        yield return StartCoroutine(SlideMole(moleImage.anchoredPosition, hiddenPosition));
        
        // Notify spawner when snake is hidden
        NotifySnakeHidden();
        
        // Clear the coroutine reference when done
        whackCoroutine = null;
    }
    
    /// <summary>
    /// Notifies the spawner when an animal is hidden (to decrement counters)
    /// </summary>
    private void NotifySnakeHidden()
    {
        if (currentAnimal != null)
        {
            MoleSpawner spawner = FindObjectOfType<MoleSpawner>();
            if (spawner != null)
            {
                if (currentAnimal.animalType == AnimalType.Snake)
                {
                    spawner.OnSnakeHidden();
                }
                else if (currentAnimal.animalType == AnimalType.Rabbit)
                {
                    spawner.OnRabbitHidden();
                }
                else if (currentAnimal.animalType == AnimalType.Meerkat)
                {
                    spawner.OnMeerkatHidden();
                }
            }
        }
    }
    
    /// <summary>
    /// Plays a random hit sound effect from the current animal's sound array
    /// </summary>
    private void PlayHitSound()
    {
        if (audioSource != null && currentAnimal != null)
        {
            AudioClip soundToPlay = currentAnimal.GetRandomHitSound();
            if (soundToPlay != null)
            {
                audioSource.PlayOneShot(soundToPlay, soundVolume);
            }
        }
    }

    /// <summary>
    /// Returns whether the mole is currently visible or animating
    /// </summary>
    public bool IsActiveOrAnimating()
    {
        return isVisible || isAnimating || whackCoroutine != null;
    }
}

