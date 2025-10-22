using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Main manager for Roguelike mode - handles game loop, waves, and progression
/// </summary>
public class RoguelikeManager : MonoBehaviour
{
    public static RoguelikeManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private MoleSpawner moleSpawner;
    [SerializeField] private ComboSystem comboSystem;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI roundWaveText;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private TextMeshProUGUI rewardGoldText;
    [SerializeField] private TextMeshProUGUI rewardGoldSymbolsText;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverMessageText;
    
    [Header("Floating Text")]
    [SerializeField] private GameObject comboTextPrefab;
    [SerializeField] private Transform canvasTransform;
    
    [Header("Game State")]
    [SerializeField] private int currentRound = 1;
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int combosThisWave = 0;  // Track combos completed this wave
    [SerializeField] private float timeRemaining = 0f;
    [SerializeField] private bool isWaveActive = false;
    
    [Header("Player Upgrades")]
    [SerializeField] private PlayerUpgrades playerUpgrades;
    private PlayerUpgrades initialUpgrades;  // Store initial Inspector values
    
    [Header("Wave Configuration - Round 1")]
    [SerializeField] private int[] round1WaveTargets = new int[] { 40, 100, 200, 350, 550 };  // Wave 1-5
    
    [Header("Wave Configuration - Round 2")]
    [SerializeField] private int[] round2WaveTargets = new int[] { 650, 900, 1250 };  // Wave 1-3
    
    [Header("Economy")]
    [SerializeField] private int goldPerCombo = 2;  // Gold earned per combo completed
    
    [Header("Current Wave")]
    private RoguelikeWaveData currentWaveData;
    
    private bool hasShownMissThisFrame = false;  // Prevent multiple miss texts per frame
    private int lastCountdownNumber = -1;  // Track which number is currently showing
    private Coroutine countdownAnimationCoroutine;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize upgrades
        if (playerUpgrades == null)
        {
            playerUpgrades = new PlayerUpgrades();
        }
    }
    
    private void Start()
    {
        // Store initial Inspector values
        if (playerUpgrades != null)
        {
            initialUpgrades = playerUpgrades.Clone();
        }
        else
        {
            playerUpgrades = new PlayerUpgrades();
            initialUpgrades = playerUpgrades.Clone();
        }
        
        // Find references if not assigned
        if (moleSpawner == null)
        {
            moleSpawner = FindObjectOfType<MoleSpawner>();
        }
        
        if (comboSystem == null)
        {
            comboSystem = FindObjectOfType<ComboSystem>();
            if (comboSystem == null)
            {
                GameObject comboObj = new GameObject("ComboSystem");
                comboSystem = comboObj.AddComponent<ComboSystem>();
            }
        }
        
        // Initialize combo system
        comboSystem.Initialize(playerUpgrades);
        
        // Find canvas if not assigned
        if (canvasTransform == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                canvasTransform = canvas.transform;
            }
        }
        
        // Show start panel
        ShowStartPanel();
    }
    
    private void Update()
    {
        if (isWaveActive)
        {
            // Update timer
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
            UpdateCountdownUI();
            
            if (timeRemaining <= 0)
            {
                // Check if player reached the target
                bool success = currentScore >= currentWaveData.pointTarget;
                EndWave(success);
            }
        }
        
        // Reset miss flag each frame
        hasShownMissThisFrame = false;
    }
    
    /// <summary>
    /// Show start panel
    /// </summary>
    private void ShowStartPanel()
    {
        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }
        
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Start the game - called from UI button
    /// </summary>
    public void StartGame()
    {
        currentRound = 1;
        currentWave = 1;
        currentScore = 0;
        
        // Reset upgrades to initial Inspector values
        if (initialUpgrades != null)
        {
            playerUpgrades = initialUpgrades.Clone();
        }
        else
        {
            playerUpgrades = new PlayerUpgrades();
        }
        
        comboSystem.Initialize(playerUpgrades);
        
        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }
        
        StartWave();
    }
    
    /// <summary>
    /// Start a new wave
    /// </summary>
    public void StartWave()
    {
        // Generate wave data
        currentWaveData = GenerateWaveData(currentRound, currentWave);
        
        // Force hide any lingering animals from previous wave
        if (moleSpawner != null)
        {
            moleSpawner.StopSpawning(); // This force hides all animals
        }
        
        // Reset wave state
        currentScore = 0;
        combosThisWave = 0;  // Reset combo counter
        timeRemaining = currentWaveData.duration;
        
        UpdateUI();
        
        Debug.Log($"[Roguelike] Starting Round {currentRound} Wave {currentWave} - Target: {currentWaveData.pointTarget}pts in {currentWaveData.duration}s");
        
        // Show pre-wave animation, then start actual wave
        StartCoroutine(PreWaveAnimation());
    }
    
    /// <summary>
    /// Pre-wave animation: all animals pop up then slide down
    /// </summary>
    private IEnumerator PreWaveAnimation()
    {
        // Get all mole controllers
        MoleController[] allMoles = FindObjectsOfType<MoleController>();
        
        // Make all moles do quick preview pop up simultaneously
        foreach (MoleController mole in allMoles)
        {
            if (mole != null)
            {
                mole.PopUpPreview();
            }
        }
        
        // Wait for preview animation to complete:
        // Pop up (0.4s) + pause (0.7s) + slide down (0.4s) = 1.5s total
        yield return new WaitForSeconds(1.6f);
        
        // Now start the actual wave
        StartWaveActual();
    }
    
    /// <summary>
    /// Actually start the wave gameplay (called after pre-wave animation)
    /// </summary>
    private void StartWaveActual()
    {
        isWaveActive = true;
        
        // Reset combo
        comboSystem.ResetCombo();
        
        // Configure and start spawner
        if (moleSpawner != null)
        {
            ConfigureSpawner();
            moleSpawner.StartSpawning();
        }
        
        Debug.Log($"[Roguelike] Wave gameplay started!");
    }
    
    /// <summary>
    /// Generate wave data based on round and wave number
    /// </summary>
    private RoguelikeWaveData GenerateWaveData(int round, int wave)
    {
        RoguelikeWaveData data = new RoguelikeWaveData(round, wave, 0, 0, 0, 0);
        
        // Duration and spawn rates (can be upgraded)
        data.duration = playerUpgrades.waveDuration;
        data.minSpawnRate = playerUpgrades.minSpawnRate;
        data.maxSpawnRate = playerUpgrades.maxSpawnRate;
        
        // Configure based on round
        if (round == 1)
        {
            // Round 1: 5 waves total
            if (wave > 0 && wave <= round1WaveTargets.Length)
            {
                data.pointTarget = round1WaveTargets[wave - 1];
            }
            else
            {
                data.pointTarget = round1WaveTargets[round1WaveTargets.Length - 1] + ((wave - round1WaveTargets.Length) * 250);
            }
            
            // Round 1 animals: Rabbits only, wave 5 adds snakes
            data.allowedAnimals.Add(AnimalType.Rabbit);
            if (wave == 5)
            {
                data.allowedAnimals.Add(AnimalType.Snake);
            }
        }
        else if (round == 2)
        {
            // Round 2: 3 waves total
            if (wave > 0 && wave <= round2WaveTargets.Length)
            {
                data.pointTarget = round2WaveTargets[wave - 1];
            }
            else
            {
                data.pointTarget = round2WaveTargets[round2WaveTargets.Length - 1] + ((wave - round2WaveTargets.Length) * 250);
            }
            
            // Round 2 animals: Rabbits + Meerkats, no snakes
            data.allowedAnimals.Add(AnimalType.Rabbit);
            data.allowedAnimals.Add(AnimalType.Meerkat);
            
            // Wave-specific caps and spawn probabilities
            if (wave == 1)
            {
                data.maxRabbits = 7;
                data.maxMeerkats = 3;
                data.meerkatSpawnChance = 0.3f;  // 30% meerkats, 70% rabbits
            }
            else if (wave == 2)
            {
                data.maxRabbits = 5;
                data.maxMeerkats = 4;
                data.meerkatSpawnChance = 0.45f; // 45% meerkats, 55% rabbits
            }
            else if (wave == 3)
            {
                data.maxRabbits = 4;
                data.maxMeerkats = 5;
                data.meerkatSpawnChance = 0.65f; // 65% meerkats, 35% rabbits
            }
        }
        
        return data;
    }
    
    /// <summary>
    /// Configure spawner based on current wave and upgrades
    /// </summary>
    private void ConfigureSpawner()
    {
        if (moleSpawner != null && currentWaveData != null)
        {
            // Apply upgraded spawn rates to spawner
            moleSpawner.SetSpawnIntervals(currentWaveData.minSpawnRate, currentWaveData.maxSpawnRate);
            
            Debug.Log($"[Roguelike] Spawner configured - Rate: {currentWaveData.minSpawnRate}-{currentWaveData.maxSpawnRate}/sec");
        }
    }
    
    /// <summary>
    /// Called when an animal is hit (called by MoleController)
    /// </summary>
    public void OnAnimalHit(AnimalType animalType, RectTransform animalRectTransform)
    {
        Debug.Log($"[Roguelike] OnAnimalHit called! Animal: {animalType}, WaveActive: {isWaveActive}");
        
        if (!isWaveActive)
        {
            Debug.LogWarning("[Roguelike] Wave not active, ignoring hit!");
            return;
        }
        
        // Get base points for this animal
        int basePoints = GetAnimalPoints(animalType);
        Debug.Log($"[Roguelike] Base points for {animalType}: {basePoints}");
        
        // Check if this is a snake (penalty)
        if (basePoints < 0)
        {
            // Snake hit - apply penalty and break combo
            currentScore += basePoints;
            SpawnFloatingText(basePoints, 0, animalRectTransform);
            
            // Break combo if we had one
            var comboInfo = comboSystem.GetComboInfo();
            if (comboInfo.currentCount > 0)
            {
                Debug.Log($"[Roguelike] Snake hit! {basePoints}pts penalty & combo broken!");
                comboSystem.OnAnimalMissed(); // Reset combo
            }
            else
            {
                Debug.Log($"[Roguelike] Snake hit! {basePoints}pts penalty");
            }
        }
        else
        {
            // Regular animal - apply combo system
            var (points, bonus) = comboSystem.OnAnimalHit(basePoints);
            
            // Award points (base + bonus)
            int totalPoints = points + bonus;
            currentScore += totalPoints;
            
            // Check if combo was triggered
            if (bonus > 0)
            {
                // Combo triggered! Award gold
                combosThisWave++;
                playerUpgrades.currentGold += goldPerCombo;
                
                Debug.Log($"[Roguelike] +{points}pts + {bonus} COMBO BONUS = {totalPoints}pts | Combo #{combosThisWave} → +${goldPerCombo} gold | Total: {currentScore} / {currentWaveData.pointTarget}");
            }
            else
            {
                Debug.Log($"[Roguelike] +{points}pts | Total: {currentScore} / {currentWaveData.pointTarget}");
            }
            
            // Spawn floating text
            SpawnFloatingText(points, bonus, animalRectTransform);
        }
        
        // Don't end wave when target reached - let them keep earning points!
        // Wave ends when timer runs out (see Update() method)
        
        UpdateUI();
    }
    
    /// <summary>
    /// Spawn floating combo text above the hit animal
    /// </summary>
    private void SpawnFloatingText(int basePoints, int bonusPoints, RectTransform animalRectTransform)
    {
        if (comboTextPrefab == null || canvasTransform == null || animalRectTransform == null)
        {
            return;
        }
        
        // Determine text to display
        string displayText;
        if (bonusPoints > 0)
        {
            // Combo triggered - show the multiplier
            float multiplier = comboSystem.GetCurrentMultiplier();
            displayText = $"{multiplier:F1}x";
        }
        else if (basePoints < 0)
        {
            // Negative points (snake hit) - show penalty
            displayText = $"{basePoints}";
        }
        else
        {
            // Regular hit - show combo count
            var comboInfo = comboSystem.GetComboInfo();
            displayText = $"{comboInfo.currentCount}";
        }
        
        // Instantiate and initialize
        GameObject floatingTextObj = Instantiate(comboTextPrefab, canvasTransform);
        FloatingComboText floatingText = floatingTextObj.GetComponent<FloatingComboText>();
        
        if (floatingText != null)
        {
            floatingText.InitializeFromUI(displayText, animalRectTransform, canvasTransform);
        }
        else
        {
            Destroy(floatingTextObj);
        }
    }
    
    /// <summary>
    /// Called when an animal escapes
    /// </summary>
    public void OnAnimalMissed(AnimalType animalType, RectTransform animalRectTransform)
    {
        if (!isWaveActive) return;
        
        // If it's a snake escaping, that's GOOD - don't penalize
        if (animalType == AnimalType.Snake)
        {
            Debug.Log("[Roguelike] Snake escaped - no penalty!");
            return;
        }
        
        // Check if we should show miss text (only if combo > 0 and not already shown this frame)
        var comboInfo = comboSystem.GetComboInfo();
        bool shouldShowMiss = comboInfo.currentCount > 0 && !hasShownMissThisFrame;
        
        if (shouldShowMiss)
        {
            SpawnMissText(animalRectTransform);
            hasShownMissThisFrame = true;
        }
        
        // Break combo (lose accumulated points)
        comboSystem.OnAnimalMissed();
        
        UpdateUI();
    }
    
    /// <summary>
    /// Spawn "Miss!" text when combo is broken
    /// </summary>
    private void SpawnMissText(RectTransform animalRectTransform)
    {
        if (comboTextPrefab == null || canvasTransform == null || animalRectTransform == null)
        {
            return;
        }
        
        Debug.Log($"[Roguelike] Spawning MISS text above {animalRectTransform.name}");
        
        // Instantiate and initialize with "Miss!" text
        GameObject floatingTextObj = Instantiate(comboTextPrefab, canvasTransform);
        FloatingComboText floatingText = floatingTextObj.GetComponent<FloatingComboText>();
        
        if (floatingText != null)
        {
            floatingText.InitializeFromUI("Miss!", animalRectTransform, canvasTransform);
        }
        else
        {
            Destroy(floatingTextObj);
        }
    }
    
    /// <summary>
    /// Get point value for an animal type
    /// </summary>
    private int GetAnimalPoints(AnimalType type)
    {
        switch (type)
        {
            case AnimalType.Rabbit:
                return playerUpgrades.basePointsPerRabbit;
            case AnimalType.Meerkat:
                return playerUpgrades.basePointsPerMeerkat;
            case AnimalType.Snake:
                return -10; // Penalty for hitting snake
            default:
                return 5;
        }
    }
    
    /// <summary>
    /// End the current wave
    /// </summary>
    private void EndWave(bool success)
    {
        isWaveActive = false;
        
        // Hide countdown panel
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
        
        // Stop spawning and force hide all active animals
        if (moleSpawner != null)
        {
            moleSpawner.StopSpawning();
        }
        
        if (success)
        {
            Debug.Log($"[Roguelike] Wave {currentWave} complete! Score: {currentScore}/{currentWaveData.pointTarget}");
            
            // Gold was already awarded per combo during wave
            int goldEarned = combosThisWave * goldPerCombo;
            
            Debug.Log($"[Roguelike] Completed {combosThisWave} combos = ${goldEarned} gold! Total: ${playerUpgrades.currentGold}");
            
            // Show reward panel before shop
            ShowRewardPanel(goldEarned, combosThisWave);
        }
        else
        {
            Debug.Log($"[Roguelike] Wave {currentWave} failed! Score: {currentScore}/{currentWaveData.pointTarget}");
            ShowGameOver(false);
        }
    }
    
    /// <summary>
    /// Show reward panel with gold earned
    /// </summary>
    private void ShowRewardPanel(int goldEarned, int combosCompleted)
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            
            // Show combo count and gold amount
            if (rewardGoldText != null)
            {
                rewardGoldText.text = $"Combos: {combosCompleted}\nGold Earned: ${goldEarned}";
            }
            
            // Show repeated $ symbols for visual appeal
            if (rewardGoldSymbolsText != null)
            {
                string dollars = new string('$', goldEarned);
                rewardGoldSymbolsText.text = dollars;
            }
        }
    }
    
    /// <summary>
    /// Continue from reward panel to shop
    /// </summary>
    public void ContinueToShop()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
        
        // Advance wave
        currentWave++;
        
        // Check if we've completed all waves in this round
        if (currentRound == 1 && currentWave > 5)
        {
            // Advance to Round 2
            currentRound = 2;
            currentWave = 1;
        }
        else if (currentRound == 2 && currentWave > 3)
        {
            // Round 2 complete - victory!
            ShowVictory();
            return;
        }
        
        ShowShop();
    }
    
    /// <summary>
    /// Show shop UI
    /// </summary>
    private void ShowShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }
        
        Debug.Log($"[Roguelike] Shop opened! Gold: ${playerUpgrades.currentGold}");
        
        // Manually refresh shop to ensure UI is updated
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.RefreshShop();
        }
        else
        {
            Debug.LogWarning("[Roguelike] ShopManager not found! Make sure it's attached to the scene.");
        }
    }
    
    /// <summary>
    /// Close shop and continue to next wave
    /// </summary>
    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
        
        StartWave();
    }
    
    /// <summary>
    /// Show victory screen
    /// </summary>
    private void ShowVictory()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        if (gameOverMessageText != null)
        {
            gameOverMessageText.text = $"Round {currentRound} Complete!\n\nTotal Gold: ${playerUpgrades.currentGold}";
        }
    }
    
    /// <summary>
    /// Show game over screen
    /// </summary>
    private void ShowGameOver(bool victory)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        if (gameOverMessageText != null)
        {
            if (victory)
            {
                gameOverMessageText.text = "Victory!";
            }
            else
            {
                gameOverMessageText.text = $"Game Over\n\nReached: Round {currentRound} Wave {currentWave}";
            }
        }
    }
    
    /// <summary>
    /// Update all UI elements
    /// </summary>
    private void UpdateUI()
    {
        if (scoreText != null)
        {
            int target = currentWaveData?.pointTarget ?? 0;
            
            // Show if target reached
            if (currentScore >= target && target > 0)
            {
                scoreText.text = $"Score: {currentScore} / {target}";
            }
            else
            {
                scoreText.text = $"Score: {currentScore} / {target}";
            }
            
            Debug.Log($"[Roguelike UI] Updated score text: {scoreText.text}");
        }
        else
        {
            Debug.LogWarning("[Roguelike UI] scoreText is null!");
        }
        
        if (targetText != null)
        {
            targetText.text = $"Target: {currentWaveData?.pointTarget ?? 0}pts";
        }
        
        if (roundWaveText != null)
        {
            roundWaveText.text = $"Round {currentRound} - Wave {currentWave}";
        }
        
        UpdateTimerUI();
    }
    
    /// <summary>
    /// Update timer display
    /// </summary>
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0, timeRemaining));
            timerText.text = $"Time: {seconds}s";
        }
    }
    
    /// <summary>
    /// Update countdown panel (shows last 5 seconds)
    /// </summary>
    private void UpdateCountdownUI()
    {
        if (countdownPanel != null)
        {
            // Show countdown panel in last 5 seconds
            if (timeRemaining <= 5f && timeRemaining > 0f)
            {
                if (!countdownPanel.activeSelf)
                {
                    countdownPanel.SetActive(true);
                }
                
                // Check if we need to show a new number
                int seconds = Mathf.CeilToInt(timeRemaining);
                if (seconds != lastCountdownNumber && countdownText != null)
                {
                    lastCountdownNumber = seconds;
                    
                    // Start animation for this number
                    if (countdownAnimationCoroutine != null)
                    {
                        StopCoroutine(countdownAnimationCoroutine);
                    }
                    countdownAnimationCoroutine = StartCoroutine(AnimateCountdownNumber(seconds));
                }
            }
            else
            {
                // Hide countdown panel
                if (countdownPanel.activeSelf)
                {
                    countdownPanel.SetActive(false);
                    lastCountdownNumber = -1;
                }
            }
        }
    }
    
    /// <summary>
    /// Animate countdown number: scale up 20% and fade out
    /// </summary>
    private IEnumerator AnimateCountdownNumber(int number)
    {
        if (countdownText == null) yield break;
        
        // Set the number
        countdownText.text = number.ToString();
        
        // Get or add CanvasGroup for fading
        CanvasGroup canvasGroup = countdownText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = countdownText.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Initial state
        float startScale = 1.0f;
        float endScale = 1.2f;  // 20% larger
        float startAlpha = 0.4f;  // 40% opacity
        float duration = 1.0f;
        float elapsedTime = 0f;
        
        RectTransform rectTransform = countdownText.GetComponent<RectTransform>();
        
        // Set initial alpha
        canvasGroup.alpha = startAlpha;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Scale up
            if (rectTransform != null)
            {
                float scale = Mathf.Lerp(startScale, endScale, t);
                rectTransform.localScale = Vector3.one * scale;
            }
            
            // Fade out from 40% to 0%
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            
            yield return null;
        }
        
        // Reset for next number
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
        canvasGroup.alpha = startAlpha;
    }
    
    /// <summary>
    /// Get current player upgrades (for shop)
    /// </summary>
    public PlayerUpgrades GetPlayerUpgrades()
    {
        return playerUpgrades;
    }
    
    /// <summary>
    /// Get current wave data (for MoleController to check allowed animals)
    /// </summary>
    public RoguelikeWaveData GetCurrentWaveData()
    {
        return currentWaveData;
    }
    
    /// <summary>
    /// Check if wave is currently active (for MoleController to check if clicks are allowed)
    /// </summary>
    public bool IsWaveActive()
    {
        return isWaveActive;
    }
    
    /// <summary>
    /// Apply an upgrade (called by shop)
    /// </summary>
    public void ApplyUpgrade(System.Action<PlayerUpgrades> upgradeAction)
    {
        upgradeAction(playerUpgrades);
        comboSystem.Initialize(playerUpgrades); // Refresh combo system
        Debug.Log("[Roguelike] Upgrade applied!");
    }
    
    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[Roguelike] Returning to Main Menu");
        SceneManager.LoadScene("MainMenuScene");
    }
}

