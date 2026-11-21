using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class MoleController : MonoBehaviour, IPointerDownHandler
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
    private float timeWhenFullyVisible = 0f;  // Timestamp when animal finished popping up
    private float telemetrySpawnTime = 0f;
    private bool telemetrySpawnLogged = false;

    // Hits are allowed once the animal is partway out of the hole, not only after it has fully risen
    [SerializeField] [Range(0f, 1f)] private float hittableFromRiseFraction = 0.4f;
    private bool isRising = false;
    private float riseStartTime = 0f;
    private MoleSpawner cachedSpawner;
    private Transform cachedCanvasRoot;

    private MoleSpawner Spawner
    {
        get
        {
            if (cachedSpawner == null)
            {
                cachedSpawner = FindObjectOfType<MoleSpawner>();
            }
            return cachedSpawner;
        }
    }

    private Transform CanvasRoot
    {
        get
        {
            if (cachedCanvasRoot == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null) cachedCanvasRoot = canvas.rootCanvas.transform;
            }
            return cachedCanvasRoot;
        }
    }

    /// <summary>
    /// True while the animal can be whacked: fully up, sliding down, or far enough into its rise.
    /// </summary>
    private bool CanBeHit
    {
        get
        {
            if (isVisible) return true;
            return isRising && Time.time >= riseStartTime + animationDuration * hittableFromRiseFraction;
        }
    }

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

        // Decorative images next to this hole (like the front lip drawn over the animal) were catching
        // taps before they reached this controller. Let taps pass through them.
        if (transform.parent != null)
        {
            if (transform.parent.GetComponent<Canvas>() == null)
            {
                foreach (Graphic g in transform.parent.GetComponentsInChildren<Graphic>(true))
                {
                    bool belongsToAMole = g.GetComponentInParent<MoleController>(true) != null;
                    bool isControl = g.GetComponentInParent<Selectable>(true) != null;
                    if (!belongsToAMole && !isControl)
                    {
                        g.raycastTarget = false;
                    }
                }
            }
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
            telemetrySpawnLogged = false;
            
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

    private void LogSpawnIfNeeded()
    {
        if (telemetrySpawnLogged || currentAnimal == null) return;

        MoleSpawner spawner = Spawner;
        if (spawner == null) return;

        int activeAfter = spawner.GetActiveRegularCount();
        int maxAllowed = spawner.GetMaxActiveAllowed();
        int activeBefore = Mathf.Max(0, activeAfter - 1);

        spawner.NotifyAnimalSpawned(currentAnimal.animalType, activeAfter, maxAllowed, activeBefore);
        telemetrySpawnLogged = true;
    }

    private void LogAnimalOutcome(string outcome, bool countsTowardAccuracy)
    {
        if (currentAnimal == null || PlaytestTelemetry.Instance == null) return;
        if (telemetrySpawnTime <= 0f && outcome != "RoundEnded") return;

        float outcomeTime = PlaytestTelemetry.Instance.WaveElapsed;
        float lifetime = telemetrySpawnTime > 0f ? outcomeTime - telemetrySpawnTime : 0f;

        MoleSpawner spawner = Spawner;
        int activeAtOutcome = spawner != null ? spawner.CountVisibleUnhitAnimals() : 0;

        PlaytestTelemetry.Instance.LogAnimalOutcome(
            telemetrySpawnTime,
            outcomeTime,
            currentAnimal.animalType.ToString(),
            outcome,
            activeAtOutcome,
            lifetime,
            countsTowardAccuracy);
    }

    private bool CountsTowardAccuracy()
    {
        return currentAnimal != null && currentAnimal.animalType != AnimalType.Snake;
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
        MoleSpawner spawner = Spawner;
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
        MoleSpawner spawner = Spawner;
        
        // Check if this is a forced snake spawn from snake event system
        if (forceSnakeSpawn)
        {
            selectedType = AnimalType.Snake;
        }
        // Check which wave we're on from RoguelikeManager
        else if (RoguelikeManager.Instance != null)
        {
            var waveData = RoguelikeManager.Instance.GetCurrentWaveData();
            var playerUpgrades = RoguelikeManager.Instance.GetPlayerUpgrades();
            
            if (waveData != null && waveData.allowedAnimals != null && waveData.allowedAnimals.Count > 0)
            {
                bool canSpawnRabbit = spawner != null && spawner.CanSpawnRabbit();
                bool canSpawnMeerkat = spawner != null && spawner.CanSpawnMeerkat();
                bool canSpawnFox = spawner != null && spawner.CanSpawnFox();
                bool canSpawnBadger = spawner != null && spawner.CanSpawnBadger();
                
                // Optimization: Start at the highest guaranteed conversion tier
                // If all conversions are at 100%, spawn badgers directly
                if (playerUpgrades != null && 
                    playerUpgrades.rabbitToMeerkatConversion >= 1.0f &&
                    playerUpgrades.meerkatToFoxConversion >= 1.0f &&
                    playerUpgrades.foxToBadgerConversion >= 1.0f)
                {
                    selectedType = AnimalType.Badger;
                }
                // If rabbit→meerkat and meerkat→fox are 100%, spawn foxes directly
                else if (playerUpgrades != null &&
                         playerUpgrades.rabbitToMeerkatConversion >= 1.0f &&
                         playerUpgrades.meerkatToFoxConversion >= 1.0f)
                {
                    selectedType = AnimalType.Fox;
                }
                // If rabbit→meerkat is 100%, spawn meerkats directly
                else if (playerUpgrades != null && playerUpgrades.rabbitToMeerkatConversion >= 1.0f)
                {
                    selectedType = AnimalType.Meerkat;
                }
                else
                {
                    // Default to rabbit spawn
                    selectedType = AnimalType.Rabbit;
                }
                
                // Apply partial conversions if not at 100%
                // Step 1: Check if we should convert rabbit to meerkat (if not already starting as meerkat)
                if (selectedType == AnimalType.Rabbit && playerUpgrades != null && playerUpgrades.rabbitToMeerkatConversion > 0f)
                {
                    float roll = Random.value;
                    
                    // If roll succeeds and we can spawn meerkat, convert to meerkat
                    if (roll < playerUpgrades.rabbitToMeerkatConversion && canSpawnMeerkat)
                    {
                        selectedType = AnimalType.Meerkat;
                    }
                    else if (!canSpawnRabbit && canSpawnMeerkat)
                    {
                        // Rabbit limit hit but meerkats available, spawn meerkat
                        selectedType = AnimalType.Meerkat;
                    }
                }
                
                // Step 2: Check if we should convert meerkat to fox (if not already starting as fox)
                if (selectedType == AnimalType.Meerkat && playerUpgrades != null && 
                    playerUpgrades.meerkatToFoxConversion > 0f && playerUpgrades.meerkatToFoxConversion < 1.0f)
                {
                    float roll = Random.value;
                    
                    // If roll succeeds and we can spawn fox, convert to fox
                    if (roll < playerUpgrades.meerkatToFoxConversion && canSpawnFox)
                    {
                        selectedType = AnimalType.Fox;
                    }
                }
                
                // Step 3: Check if we should convert fox to badger (if not already starting as badger)
                if (selectedType == AnimalType.Fox && playerUpgrades != null && 
                    playerUpgrades.foxToBadgerConversion > 0f && playerUpgrades.foxToBadgerConversion < 1.0f)
                {
                    float roll = Random.value;
                    
                    // If roll succeeds and we can spawn badger, convert to badger
                    if (roll < playerUpgrades.foxToBadgerConversion && canSpawnBadger)
                    {
                        selectedType = AnimalType.Badger;
                    }
                }
                
                // Fallback: If we can't spawn the selected type, try others (in order of value)
                if (selectedType == AnimalType.Badger && !canSpawnBadger)
                {
                    if (canSpawnFox) selectedType = AnimalType.Fox;
                    else if (canSpawnMeerkat) selectedType = AnimalType.Meerkat;
                    else selectedType = AnimalType.Rabbit;
                }
                else if (selectedType == AnimalType.Fox && !canSpawnFox)
                {
                    if (canSpawnMeerkat) selectedType = AnimalType.Meerkat;
                    else selectedType = AnimalType.Rabbit;
                }
                else if (selectedType == AnimalType.Meerkat && !canSpawnMeerkat)
                {
                    selectedType = canSpawnRabbit ? AnimalType.Rabbit : AnimalType.Fox;
                }
                else if (selectedType == AnimalType.Rabbit && !canSpawnRabbit)
                {
                    if (canSpawnMeerkat) selectedType = AnimalType.Meerkat;
                    else if (canSpawnFox) selectedType = AnimalType.Fox;
                    else if (canSpawnBadger) selectedType = AnimalType.Badger;
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
            else if (selectedType == AnimalType.Fox)
            {
                spawner.OnFoxSpawned();
            }
            else if (selectedType == AnimalType.Badger)
            {
                spawner.OnBadgerSpawned();
            }
        }
        
        // Set the sprite
        if (moleImageComponent != null && currentAnimal != null && currentAnimal.normalSprite != null)
        {
            moleImageComponent.sprite = currentAnimal.normalSprite;
        }
        
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
        bool wasVisibleUnhit = isVisible && !wasHit && currentAnimal != null;
        bool waveEnding = PlaytestTelemetry.Instance != null && PlaytestTelemetry.Instance.IsWaveEnding;

        if (wasVisibleUnhit && waveEnding)
        {
            LogAnimalOutcome("RoundEnded", CountsTowardAccuracy());
        }

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
        isRising = false;
        wasHit = false;
        
        if (moleImage != null)
        {
            moleImage.anchoredPosition = hiddenPosition;
            moleImage.localScale = Vector3.one;
            moleImage.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Handles the pop up sequence: slide up, wait, slide down.
    /// The slides run inline (not as nested coroutines) so a hit can stop them instantly.
    /// </summary>
    private IEnumerator PopUpSequence()
    {
        bool isSnake = currentAnimal != null && currentAnimal.animalType == AnimalType.Snake;

        // Rise with a quick pop and slight overshoot (snakes slither up smoothly instead, so they read differently)
        isRising = true;
        isAnimating = true;
        riseStartTime = Time.time;
        timeWhenFullyVisible = riseStartTime + animationDuration;
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float eased = isSnake ? GameFeel.EaseOutQuad(t) : GameFeel.EaseOutBack(t);
            if (moleImage != null)
            {
                moleImage.anchoredPosition = Vector2.LerpUnclamped(hiddenPosition, visiblePosition, eased);
            }
            yield return null;
        }
        if (moleImage != null)
        {
            moleImage.anchoredPosition = visiblePosition;
        }
        isRising = false;
        isAnimating = false;
        isVisible = true;

        if (PlaytestTelemetry.Instance != null && PlaytestTelemetry.Instance.IsWaveActive)
        {
            telemetrySpawnTime = PlaytestTelemetry.Instance.WaveElapsed;
            LogSpawnIfNeeded();
        }

        // Wait for animal-specific visible duration (or fallback to default). Snakes sway so they're easy to spot.
        float waitTime = (currentAnimal != null) ? currentAnimal.visibleDuration : visibleDuration;
        elapsed = 0f;
        while (elapsed < waitTime)
        {
            elapsed += Time.deltaTime;
            if (isSnake && moleImage != null)
            {
                moleImage.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 9f) * 7f);
            }
            yield return null;
        }
        if (moleImage != null)
        {
            moleImage.localRotation = Quaternion.identity;
        }

        // Slide down if still visible (not whacked) - player can still hit during this animation!
        if (isVisible)
        {
            isAnimating = true;
            elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                if (moleImage != null)
                {
                    moleImage.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, GameFeel.EaseInQuad(t));
                }
                yield return null;
            }
            if (moleImage != null)
            {
                moleImage.anchoredPosition = hiddenPosition;
            }
            isAnimating = false;

            // Only NOW check if animal escaped (after slide down completes)
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
    /// Slides the mole between two positions (ease-in, used for going back into the hole)
    /// </summary>
    private IEnumerator SlideMole(Vector2 fromPosition, Vector2 toPosition)
    {
        isAnimating = true;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);

            if (moleImage != null)
            {
                moleImage.anchoredPosition = Vector2.Lerp(fromPosition, toPosition, GameFeel.EaseInQuad(t));
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
    /// Handles taps. Uses pointer DOWN (not click) so hits register the instant a finger lands,
    /// and a finger that slides slightly still counts.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        bool gameActive;
        if (RoguelikeManager.Instance != null)
        {
            gameActive = RoguelikeManager.Instance.IsWaveActive();
        }
        else
        {
            gameActive = GameManager.Instance != null && GameManager.Instance.IsGameActive();
        }

        if (!gameActive) return;

        if (CanBeHit && !wasHit)
        {
            Whack();
        }
        else if (!isVisible && !isRising && whackCoroutine == null)
        {
            // Tapped an empty hole - a little dust puff confirms the tap registered
            GameFeel.Burst(CanvasRoot, transform.position, new Color(0.85f, 0.75f, 0.6f, 0.8f), 6, 220f, 18f, 0.3f);
        }
    }

    /// <summary>
    /// Called when the mole is successfully whacked
    /// </summary>
    private void Whack()
    {
        wasHit = true;  // Mark as hit

        // If the animal was hit while still rising, log its spawn first so telemetry stays consistent
        if (isRising && !telemetrySpawnLogged && PlaytestTelemetry.Instance != null && PlaytestTelemetry.Instance.IsWaveActive)
        {
            telemetrySpawnTime = PlaytestTelemetry.Instance.WaveElapsed;
            LogSpawnIfNeeded();
        }
        isRising = false;

        LogAnimalOutcome("Hit", CountsTowardAccuracy());
        
        // Check if we're in roguelike mode
        if (RoguelikeManager.Instance != null)
        {
            // Roguelike mode scoring - pass this GameObject's RectTransform (parent of mole image)
            if (currentAnimal != null)
            {
                RectTransform thisRect = GetComponent<RectTransform>();
                
                // Perfect hit = within 0.4s of becoming fully visible (hits during the rise count as perfect)
                float timeSinceVisible = Time.time - timeWhenFullyVisible;
                bool isPerfectHit = timeSinceVisible <= 0.4f;
                
                RoguelikeManager.Instance.OnAnimalHit(currentAnimal.animalType, thisRect, isPerfectHit);
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
                    // Lose a life for hitting snake with floating text
                    RectTransform thisRect = GetComponent<RectTransform>();
                    GameManager.Instance.OnSnakeHit(thisRect);
                    
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

        // Stop the pop up sequence (this also stops any rise/fall in progress)
        if (popUpCoroutine != null)
        {
            StopCoroutine(popUpCoroutine);
            popUpCoroutine = null;
        }
        if (moleImage != null)
        {
            moleImage.localRotation = Quaternion.identity;
        }

        // Hit particles, drawn above the hole
        if (moleImage != null && currentAnimal != null)
        {
            bool isSnake = currentAnimal.animalType == AnimalType.Snake;
            Color burstColor = isSnake ? new Color(0.55f, 0.9f, 0.35f, 0.9f) : new Color(1f, 0.92f, 0.45f, 0.95f);
            int burstCount = isSnake ? 8 : (currentAnimal.animalType == AnimalType.Badger ? 14 : 10);
            GameFeel.Burst(CanvasRoot, moleImage.position, burstColor, burstCount, 420f, 22f, 0.4f);
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

        if (currentAnimal != null && currentAnimal.animalType != AnimalType.Snake)
        {
            LogAnimalOutcome("Expired", true);
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
                    // Lose a life with floating text feedback
                    RectTransform thisRect = GetComponent<RectTransform>();
                    GameManager.Instance.OnAnimalMissed(thisRect);
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
        
        // Squash on impact, then spring back while the hit sprite shows
        float elapsed = 0f;
        while (elapsed < hitSpriteDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hitSpriteDuration);
            if (moleImage != null)
            {
                // 0 -> 0.35: squash in, 0.35 -> 1: spring back
                float squash = t < 0.35f ? t / 0.35f : 1f - GameFeel.EaseOutQuad((t - 0.35f) / 0.65f);
                moleImage.localScale = new Vector3(1f + 0.25f * squash, 1f - 0.25f * squash, 1f);
            }
            yield return null;
        }
        if (moleImage != null)
        {
            moleImage.localScale = Vector3.one;
        }
        
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
            MoleSpawner spawner = Spawner;
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
                else if (currentAnimal.animalType == AnimalType.Fox)
                {
                    spawner.OnFoxHidden();
                }
                else if (currentAnimal.animalType == AnimalType.Badger)
                {
                    spawner.OnBadgerHidden();
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

    public void GetTelemetryState(out bool unhitVisible, out bool animatingHit, out bool anyVisible)
    {
        unhitVisible = isVisible && !wasHit && whackCoroutine == null;
        animatingHit = whackCoroutine != null;
        anyVisible = isVisible || isAnimating || whackCoroutine != null;
    }
}

