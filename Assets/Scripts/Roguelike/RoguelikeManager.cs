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
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    
    [Header("Pause Panel Upgrade Display")]
    [SerializeField] private Transform[] pauseUpgradeSlots = new Transform[10]; // UpgradeImage1-10
    [SerializeField] private UpgradeDatabase upgradeDatabase;
    
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
    [SerializeField] private bool isPaused = false;
    
    [Header("Player Upgrades")]
    [SerializeField] private PlayerUpgrades playerUpgrades;
    private PlayerUpgrades initialUpgrades;  // Store initial Inspector values
    
    [Header("Wave Configuration - Round 1")]
    [SerializeField] private int[] round1WaveTargets = new int[] { 40, 100, 200 };  // Wave 1-3: Rabbits only
    
    [Header("Wave Configuration - Round 2")]
    [SerializeField] private int[] round2WaveTargets = new int[] { 350, 550, 750 };  // Wave 1-3: Rabbit→Meerkat conversion
    
    [Header("Wave Configuration - Round 3")]
    [SerializeField] private int[] round3WaveTargets = new int[] { 1000, 1300, 1650 };  // Wave 1-3
    
    [Header("Wave Configuration - Round 4")]
    [SerializeField] private int[] round4WaveTargets = new int[] { 2050, 2500, 3000 };  // Wave 1-3
    
    [Header("Wave Configuration - Round 5")]
    [SerializeField] private int[] round5WaveTargets = new int[] { 3600, 4300, 5100 };  // Wave 1-3: Final round!
    
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
            // Set gold per combo from Inspector value
            playerUpgrades.goldPerCombo = goldPerCombo;
            initialUpgrades = playerUpgrades.Clone();
        }
        else
        {
            playerUpgrades = new PlayerUpgrades();
            playerUpgrades.goldPerCombo = goldPerCombo;
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
        // Don't update game logic if paused
        if (isWaveActive && !isPaused)
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
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        // Show pause button if assigned
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
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
        isPaused = false;
        Time.timeScale = 1f;
        
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
        
        // Show pause button when game starts
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
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
        
        // Reset combo interest accumulation for new wave
        if (playerUpgrades != null)
        {
            playerUpgrades.comboInterestAccumulated = 0f;
        }
        
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
            // Round 1: 3 waves total - Rabbits only (tutorial round)
            if (wave > 0 && wave <= round1WaveTargets.Length)
            {
                data.pointTarget = round1WaveTargets[wave - 1];
            }
            else
            {
                data.pointTarget = round1WaveTargets[round1WaveTargets.Length - 1] + ((wave - round1WaveTargets.Length) * 100);
            }
            
            // Round 1 animals: Rabbits only, wave 3 adds snakes
            data.allowedAnimals.Add(AnimalType.Rabbit);
            if (wave == 3)
            {
                data.allowedAnimals.Add(AnimalType.Snake);
            }
        }
        else if (round == 2)
        {
            // Round 2: 3 waves total - Rabbit→Meerkat conversion available
            if (wave > 0 && wave <= round2WaveTargets.Length)
            {
                data.pointTarget = round2WaveTargets[wave - 1];
            }
            else
            {
                data.pointTarget = round2WaveTargets[round2WaveTargets.Length - 1] + ((wave - round2WaveTargets.Length) * 250);
            }
            
            // Round 2 animals: Just rabbits by default (meerkats spawn via upgrade)
            data.allowedAnimals.Add(AnimalType.Rabbit);
            
            // Waves 2 and 3 add snakes
            if (wave == 2 || wave == 3)
            {
                data.allowedAnimals.Add(AnimalType.Snake);
            }
            
            // Note: Individual animal caps removed - using total maxActiveAnimals instead
            // The rabbitToMeerkatConversion upgrade determines the ratio naturally
        }
        else if (round == 3)
        {
            // Round 3: 3 waves total - Higher difficulty
            if (wave > 0 && wave <= round3WaveTargets.Length)
            {
                data.pointTarget = round3WaveTargets[wave - 1];
            }
            else
            {
                data.pointTarget = round3WaveTargets[round3WaveTargets.Length - 1] + ((wave - round3WaveTargets.Length) * 400);
            }
            
            // Round 3 animals: Rabbits + snakes + optional meerkats via upgrade
            data.allowedAnimals.Add(AnimalType.Rabbit);
            data.allowedAnimals.Add(AnimalType.Snake);
        }
        else if (round == 4)
        {
            // Round 4: 3 waves total - Expert difficulty
            if (wave > 0 && wave <= round4WaveTargets.Length)
            {
                data.pointTarget = round4WaveTargets[wave - 1];
            }
            else
            {
                data.pointTarget = round4WaveTargets[round4WaveTargets.Length - 1] + ((wave - round4WaveTargets.Length) * 550);
            }
            
            // Round 4 animals: Rabbits + snakes + optional meerkats via upgrade
            data.allowedAnimals.Add(AnimalType.Rabbit);
            data.allowedAnimals.Add(AnimalType.Snake);
        }
        else if (round == 5)
        {
            // Round 5: 3 waves total - Final challenge!
            if (wave > 0 && wave <= round5WaveTargets.Length)
            {
                data.pointTarget = round5WaveTargets[wave - 1];
            }
            else
            {
                data.pointTarget = round5WaveTargets[round5WaveTargets.Length - 1] + ((wave - round5WaveTargets.Length) * 750);
            }
            
            // Round 5 animals: Rabbits + snakes + optional meerkats via upgrade
            data.allowedAnimals.Add(AnimalType.Rabbit);
            data.allowedAnimals.Add(AnimalType.Snake);
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
    public void OnAnimalHit(AnimalType animalType, RectTransform animalRectTransform, bool isPerfectHit = false)
    {
        Debug.Log($"[Roguelike] OnAnimalHit called! Animal: {animalType}, WaveActive: {isWaveActive}");
        
        if (!isWaveActive)
        {
            Debug.LogWarning("[Roguelike] Wave not active, ignoring hit!");
            return;
        }
        
        // Get base points for this animal
        int basePoints = GetAnimalPoints(animalType);
        
        // Check for critical hit
        bool isCriticalHit = false;
        if (playerUpgrades.critChance > 0f && basePoints > 0)
        {
            float critRoll = Random.value;
            if (critRoll < playerUpgrades.critChance)
            {
                isCriticalHit = true;
                basePoints = basePoints * 2; // Critical hit: 2x points
                Debug.Log($"[Roguelike] CRITICAL HIT! {animalType}: {basePoints}pts (2x)");
            }
        }
        
        // Apply perfect hit bonus if applicable (stacks multiplicatively with crit)
        if (isPerfectHit && playerUpgrades.perfectHitMultiplier > 1.0f && basePoints > 0)
        {
            int pointsBeforePerfect = basePoints;
            basePoints = Mathf.RoundToInt(basePoints * playerUpgrades.perfectHitMultiplier);
            
            if (isCriticalHit)
            {
                float totalMultiplier = 2.0f * playerUpgrades.perfectHitMultiplier;
                Debug.Log($"[Roguelike] CRITICAL + PERFECT HIT! {animalType}: {basePoints}pts ({totalMultiplier:F1}x total!)");
            }
            else
            {
                Debug.Log($"[Roguelike] PERFECT HIT! {animalType}: {basePoints}pts ({playerUpgrades.perfectHitMultiplier:F1}x bonus)");
            }
        }
        else if (!isCriticalHit)
        {
            Debug.Log($"[Roguelike] Base points for {animalType}: {basePoints}");
        }
        
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
                // Combo triggered! Award gold with fractional accumulation
                combosThisWave++;
                int goldAwarded = playerUpgrades.AwardGold(playerUpgrades.goldPerCombo);
                
                // Add time to wave if upgrade purchased
                if (playerUpgrades.timeAddedPerCombo > 0f)
                {
                    timeRemaining += playerUpgrades.timeAddedPerCombo;
                }
                
                // Increase combo multiplier for future combos if interest upgrade purchased
                if (playerUpgrades.comboInterestRate > 0f)
                {
                    playerUpgrades.comboInterestAccumulated += playerUpgrades.comboInterestRate;
                    comboSystem.Initialize(playerUpgrades); // Refresh combo system with new multiplier
                    
                    float currentMultiplier = playerUpgrades.comboMultiplierIncrement + playerUpgrades.comboInterestAccumulated;
                    Debug.Log($"[Roguelike] +{points}pts + {bonus} COMBO BONUS = {totalPoints}pts | Combo #{combosThisWave} → +${goldAwarded} gold | Multiplier grew to {currentMultiplier:F2}x!");
                }
                else if (playerUpgrades.timeAddedPerCombo > 0f)
                {
                    Debug.Log($"[Roguelike] +{points}pts + {bonus} COMBO BONUS = {totalPoints}pts | Combo #{combosThisWave} → +${goldAwarded} gold, +{playerUpgrades.timeAddedPerCombo:F1}s time | Total: {currentScore} / {currentWaveData.pointTarget}");
                }
                else
                {
                    Debug.Log($"[Roguelike] +{points}pts + {bonus} COMBO BONUS = {totalPoints}pts | Combo #{combosThisWave} → +${goldAwarded} gold (${playerUpgrades.goldPerCombo:F1}/combo) | Total: {currentScore} / {currentWaveData.pointTarget}");
                }
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
            case AnimalType.Fox:
                return playerUpgrades.basePointsPerFox;
            case AnimalType.Badger:
                return playerUpgrades.basePointsPerBadger;
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
            
            // Calculate total gold earned this wave (gold was awarded per combo during wave)
            int goldEarned = Mathf.FloorToInt(combosThisWave * playerUpgrades.goldPerCombo);
            
            Debug.Log($"[Roguelike] Completed {combosThisWave} combos @ ${playerUpgrades.goldPerCombo:F1}/combo = ${goldEarned} gold! Total: ${playerUpgrades.currentGold} (fractional: ${playerUpgrades.fractionalGold:F2})");
            
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
        
        // Check if we've completed all waves in this round (3 waves per round)
        if (currentWave > 3)
        {
            // Advance to next round
            currentRound++;
            currentWave = 1;
            
            // Check if we've completed all 5 rounds
            if (currentRound > 5)
            {
                // All rounds complete - victory!
                ShowVictory();
                return;
            }
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
        
        // Hide score and timer text while shop is open
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
        
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
        
        // Hide pause button while in shop
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
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
        
        // Show score and timer text again
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
        }
        
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }
        
        // Show pause button again when returning to gameplay
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
        }
        
        StartWave();
    }
    
    /// <summary>
    /// Show victory screen
    /// </summary>
    private void ShowVictory()
    {
        // Hide pause button on victory
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
        
        // Make sure time scale is normal
        Time.timeScale = 1f;
        isPaused = false;
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        if (gameOverMessageText != null)
        {
            gameOverMessageText.text = $"VICTORY!\n\nAll 5 Rounds Complete!\n\nTotal Gold: ${playerUpgrades.currentGold}";
        }
    }
    
    /// <summary>
    /// Show game over screen
    /// </summary>
    private void ShowGameOver(bool victory)
    {
        // Hide pause button on game over
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
        
        // Make sure time scale is normal
        Time.timeScale = 1f;
        isPaused = false;
        
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
    /// Get current wave number (1-based) within the round
    /// </summary>
    public int GetCurrentWaveNumber()
    {
        return currentWave;
    }
    
    /// <summary>
    /// Get current round number (1-based)
    /// </summary>
    public int GetCurrentRound()
    {
        return currentRound;
    }
    
    /// <summary>
    /// Get absolute wave number across all rounds (1-based)
    /// Round 1 Wave 1 = 1, Round 1 Wave 3 = 3, Round 2 Wave 1 = 4, etc.
    /// </summary>
    public int GetAbsoluteWaveNumber()
    {
        return ((currentRound - 1) * 3) + currentWave;
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
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        if (!isPaused && isWaveActive)
        {
            isPaused = true;
            Time.timeScale = 0f;
            
            // Show pause panel
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
            
            // Update upgrade display in pause panel
            UpdatePauseUpgradeDisplay();
            
            Debug.Log("[Roguelike] Game paused");
        }
    }
    
    /// <summary>
    /// Update the upgrade display in the pause panel
    /// </summary>
    private void UpdatePauseUpgradeDisplay()
    {
        if (pauseUpgradeSlots == null || pauseUpgradeSlots.Length == 0 || upgradeDatabase == null || playerUpgrades == null)
        {
            return;
        }
        
        // Get list of purchased upgrades (those with tier > 0), excluding points upgrades
        var purchasedUpgrades = new System.Collections.Generic.List<UpgradeType>();
        foreach (var kvp in playerUpgrades.upgradeTiers)
        {
            if (kvp.Value > 0)
            {
                // Skip points upgrades (RabbitPoints, MeerkatPoints, etc.)
                UpgradeDefinition upgradeDef = upgradeDatabase.GetUpgrade(kvp.Key);
                if (upgradeDef != null && upgradeDef.isPointsUpgrade)
                {
                    continue; // Don't display points upgrades in pause menu
                }
                
                purchasedUpgrades.Add(kvp.Key);
            }
        }
        
        Debug.Log($"[Roguelike] Displaying {purchasedUpgrades.Count} purchased upgrades in pause panel (excluding points upgrades)");
        
        // Display each purchased upgrade
        for (int i = 0; i < pauseUpgradeSlots.Length; i++)
        {
            Transform slot = pauseUpgradeSlots[i];
            
            if (slot == null)
            {
                continue;
            }
            
            // If we have an upgrade to display in this slot
            if (i < purchasedUpgrades.Count)
            {
                UpgradeType upgradeType = purchasedUpgrades[i];
                int tierLevel = playerUpgrades.GetTierLevel(upgradeType);
                UpgradeDefinition upgradeDef = upgradeDatabase.GetUpgrade(upgradeType);
                
                // Show the slot
                slot.gameObject.SetActive(true);
                
                // Set the main upgrade icon
                UnityEngine.UI.Image mainImage = slot.GetComponent<UnityEngine.UI.Image>();
                if (mainImage != null && upgradeDef != null && upgradeDef.icon != null)
                {
                    mainImage.sprite = upgradeDef.icon;
                    mainImage.enabled = true;
                }
                
                // Update tier indicators (child images)
                int maxTiers = upgradeDef != null ? upgradeDef.maxTiers : 5;
                for (int tierIndex = 0; tierIndex < slot.childCount && tierIndex < 5; tierIndex++)
                {
                    Transform tierChild = slot.GetChild(tierIndex);
                    UnityEngine.UI.Image tierImage = tierChild.GetComponent<UnityEngine.UI.Image>();
                    
                    if (tierImage != null)
                    {
                        // Show tier indicator if within max tiers and purchased
                        bool shouldShow = tierIndex < tierLevel && tierIndex < maxTiers;
                        tierChild.gameObject.SetActive(shouldShow);
                    }
                }
                
                Debug.Log($"[Roguelike] Slot {i}: {upgradeType} (Tier {tierLevel}/{maxTiers})");
            }
            else
            {
                // No upgrade for this slot - hide it
                slot.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Resume the game
    /// </summary>
    public void ResumeGame()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = 1f;
            
            // Hide pause panel
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
            
            Debug.Log("[Roguelike] Game resumed");
        }
    }
    
    /// <summary>
    /// Return to main menu (from pause or game over)
    /// </summary>
    public void ReturnToMainMenu()
    {
        // Make sure time scale is restored
        Time.timeScale = 1f;
        isPaused = false;
        
        Debug.Log("[Roguelike] Returning to Main Menu");
        SceneManager.LoadScene("MainMenuScene");
    }
}

