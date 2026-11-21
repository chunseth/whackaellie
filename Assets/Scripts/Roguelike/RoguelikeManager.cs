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
    
    [Header("Audio")]
    [SerializeField] private AudioClip gameStartSound;
    [SerializeField] private AudioClip buttonPressSound;
    [SerializeField] private AudioClip coinGlimmerSound;
    private AudioSource audioSource;
    
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
    
    // Game feel / presentation state
    private const int DefaultSnakePenalty = -35;
    private int cachedSnakePenalty = 0;
    private RectTransform holeGrid;
    private RectTransform scoreBarFill;
    private Image scoreBarFillImage;
    private bool targetReachedThisWave = false;
    private Color timerBaseColor = Color.white;
    private Color scoreBaseColor = Color.white;
    private bool timerColorCached = false;
    private Coroutine coinSoundsCoroutine;
    private bool skipCoinAnimation = false;
    private Coroutine scorePulseCoroutine;
    
    private static readonly Color ScoreBarColor = new Color(0.35f, 0.85f, 0.4f, 1f);
    private static readonly Color ScoreBarDoneColor = new Color(1f, 0.82f, 0.2f, 1f);
    private const string TutorialDonePrefKey = "WAE_RoguelikeTutorialDone";
    private const string ShopTipSeenPrefKey = "WAE_ShopTipSeen";
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

        EnsurePlaytestTelemetry();
        
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
        
        // Get or add AudioSource component for sound effects
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
        
        // Presentation setup (all built at runtime, no scene changes needed)
        MusicPlayer.Ensure().SetQuiet();
        CreateScoreBar();
        if (rewardPanel != null)
        {
            PointerDownRelay relay = rewardPanel.GetComponent<PointerDownRelay>();
            if (relay == null) relay = rewardPanel.AddComponent<PointerDownRelay>();
            relay.OnDown += () => skipCoinAnimation = true;
        }
        
        // Show start panel
        ShowStartPanel();
    }
    
    /// <summary>
    /// The hole grid (shaken on big hits). Found by walking up from a mole to the canvas.
    /// </summary>
    private RectTransform HoleGrid
    {
        get
        {
            if (holeGrid == null)
            {
                MoleController mole = FindObjectOfType<MoleController>();
                if (mole != null)
                {
                    Transform t = mole.transform;
                    while (t.parent != null && t.parent.GetComponent<Canvas>() == null)
                    {
                        t = t.parent;
                    }
                    holeGrid = t as RectTransform;
                }
            }
            return holeGrid;
        }
    }
    
    private TMP_FontAsset UIFont
    {
        get { return scoreText != null ? scoreText.font : null; }
    }
    
    /// <summary>
    /// Thin progress bar under the score text that fills toward the wave target and turns gold when reached.
    /// </summary>
    private void CreateScoreBar()
    {
        if (scoreText == null || scoreBarFill != null) return;
        scoreBaseColor = scoreText.color;
        
        GameObject background = new GameObject("ScoreBar", typeof(RectTransform), typeof(Image));
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.SetParent(scoreText.rectTransform, false);
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 0f);
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.anchoredPosition = new Vector2(0f, -4f);
        bgRect.sizeDelta = new Vector2(0f, 14f);
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.35f);
        bgImage.raycastTarget = false;
        
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        scoreBarFill = fill.GetComponent<RectTransform>();
        scoreBarFill.SetParent(bgRect, false);
        scoreBarFill.anchorMin = new Vector2(0f, 0f);
        scoreBarFill.anchorMax = new Vector2(0f, 1f);
        scoreBarFill.offsetMin = Vector2.zero;
        scoreBarFill.offsetMax = Vector2.zero;
        scoreBarFillImage = fill.GetComponent<Image>();
        scoreBarFillImage.color = ScoreBarColor;
        scoreBarFillImage.raycastTarget = false;
    }
    
    private void UpdateScoreBar()
    {
        if (scoreBarFill == null) return;
        int target = currentWaveData?.pointTarget ?? 0;
        float progress = target > 0 ? Mathf.Clamp01(currentScore / (float)target) : 0f;
        scoreBarFill.anchorMax = new Vector2(progress, 1f);
        scoreBarFillImage.color = progress >= 1f ? ScoreBarDoneColor : ScoreBarColor;
    }
    
    private IEnumerator PulseScoreText()
    {
        if (scoreText == null) yield break;
        RectTransform rt = scoreText.rectTransform;
        float elapsed = 0f;
        const float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rt.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        rt.localScale = Vector3.one;
        scorePulseCoroutine = null;
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
        // Play button press sound
        PlayButtonPressSound();
        
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
        
        // Play game start sound effect
        if (audioSource != null && gameStartSound != null)
        {
            audioSource.PlayOneShot(gameStartSound);
        }
        
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

        EnsurePlaytestTelemetry();
        if (currentWave == 1)
        {
            PlaytestTelemetry.Instance?.BeginRound(currentRound, playerUpgrades);
        }
        
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
        
        targetReachedThisWave = false;
        if (scoreText != null) scoreText.color = scoreBaseColor;
        
        Debug.Log($"[Roguelike] Starting Round {currentRound} Wave {currentWave} - Target: {currentWaveData.pointTarget}pts in {currentWaveData.duration}s");
        
        ShowWaveIntroBanner();
        
        // Show pre-wave animation, then start actual wave
        StartCoroutine(PreWaveAnimation());
    }
    
    /// <summary>
    /// Shown during the pre-wave preview: the wave's target, or a one-line tip during the first round
    /// until the player has cleared round 1 once.
    /// </summary>
    private void ShowWaveIntroBanner()
    {
        string tip = null;
        bool tutorialDone = PlayerPrefs.GetInt(TutorialDonePrefKey, 0) == 1;
        if (currentRound == 1 && !tutorialDone)
        {
            if (currentWave == 1)
            {
                tip = "Tap the animals!";
            }
            else if (currentWave == 2)
            {
                tip = $"{playerUpgrades.comboHitsToStart} hits in a row = COMBO\n<size=60%>Combos multiply points and earn gold</size>";
            }
            else if (currentWave == 3)
            {
                tip = $"Don't tap snakes!\n<size=60%>{SnakePenalty} points and your combo breaks</size>";
            }
        }
        
        string header = $"<size=55%>Round {currentRound} - Wave {currentWave}</size>";
        string body = tip ?? $"Target: {currentWaveData.pointTarget}";
        GameFeel.Banner(canvasTransform, UIFont, $"{header}\n{body}", Color.white, 80f, 0.9f);
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

        GameFeel.Banner(canvasTransform, UIFont, "GO!", new Color(0.45f, 1f, 0.5f), 140f, 0.15f);
        if (MusicPlayer.Instance != null) MusicPlayer.Instance.SetGameplay();
        
        int maxActive = playerUpgrades != null ? playerUpgrades.maxActiveAnimals : 7;
        PlaytestTelemetry.Instance?.BeginWave(currentRound, currentWave, maxActive);
        
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
    /// Called by MoleController when an animal is hit
    /// </summary>
    public void OnAnimalHit(AnimalType animalType, RectTransform animalRectTransform, bool isPerfectHit = false)
    {
        if (!isWaveActive)
        {
            return;
        }
        
        // Get base points for this animal
        int basePoints = GetAnimalPoints(animalType);
        
        // Check for critical hit
        bool isCriticalHit = false;
        if (playerUpgrades.critChance > 0f && basePoints > 0)
        {
            if (Random.value < playerUpgrades.critChance)
            {
                isCriticalHit = true;
                basePoints = basePoints * 2; // Critical hit: 2x points
            }
        }
        
        // Apply perfect hit bonus if applicable (stacks multiplicatively with crit)
        bool perfectBonusApplied = false;
        if (isPerfectHit && playerUpgrades.perfectHitMultiplier > 1.0f && basePoints > 0)
        {
            basePoints = Mathf.RoundToInt(basePoints * playerUpgrades.perfectHitMultiplier);
            perfectBonusApplied = true;
        }
        
        // Check if this is a snake (penalty)
        if (basePoints < 0)
        {
            // Snake hit - apply penalty and break combo
            currentScore += basePoints;
            SpawnFloatingText(basePoints, 0, animalRectTransform, false, false);
            
            if (comboSystem.GetComboInfo().currentCount > 0)
            {
                comboSystem.OnAnimalMissed(); // Reset combo
            }
            
            // Snake hits should sting
            Haptics.Play(Haptics.Style.Error);
            GameFeel.Shake(HoleGrid, 18f, 0.25f);
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.Duck(0.6f, 0.3f);
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
                playerUpgrades.AwardGold(playerUpgrades.goldPerCombo);
                
                // Add time to wave if upgrade purchased
                if (playerUpgrades.timeAddedPerCombo > 0f)
                {
                    timeRemaining += playerUpgrades.timeAddedPerCombo;
                }
                
                // Spawn the combo text now, so it shows the multiplier this combo actually used
                SpawnFloatingText(points, bonus, animalRectTransform, isCriticalHit, perfectBonusApplied);
                
                // Increase combo multiplier for future combos if interest upgrade purchased
                if (playerUpgrades.comboInterestRate > 0f)
                {
                    playerUpgrades.comboInterestAccumulated += playerUpgrades.comboInterestRate;
                    comboSystem.Initialize(playerUpgrades); // Refresh combo system with new multiplier
                }
                
                // Make combos feel big
                Haptics.Play(Haptics.Style.Heavy);
                GameFeel.HitStop(0.06f);
                GameFeel.Shake(HoleGrid, 10f, 0.2f);
                if (animalRectTransform != null)
                {
                    GameFeel.Burst(canvasTransform, animalRectTransform.position, new Color(1f, 0.8f, 0.2f, 1f), 18, 650f, 26f, 0.6f);
                }
            }
            else
            {
                SpawnFloatingText(points, bonus, animalRectTransform, isCriticalHit, perfectBonusApplied);
                
                bool bigAnimal = animalType == AnimalType.Badger || animalType == AnimalType.Fox;
                Haptics.Play(bigAnimal || isCriticalHit ? Haptics.Style.Medium : Haptics.Style.Light);
                if (animalType == AnimalType.Badger)
                {
                    GameFeel.HitStop(0.04f);
                }
            }
        }
        
        // Don't end wave when target reached - let them keep earning points!
        // Wave ends when timer runs out (see Update() method)
        
        UpdateUI();
    }
    
    /// <summary>
    /// Spawn floating text above the hit animal:
    /// building hits show combo progress ("2/3"), a combo shows the multiplier and bonus, snakes show the penalty.
    /// Crit and Perfect get their own label.
    /// </summary>
    private void SpawnFloatingText(int basePoints, int bonusPoints, RectTransform animalRectTransform, bool isCrit, bool isPerfect)
    {
        if (comboTextPrefab == null || canvasTransform == null || animalRectTransform == null)
        {
            return;
        }
        
        string displayText;
        Color color;
        float size;
        float duration;
        float rise;
        
        string label = "";
        if (isCrit && isPerfect) label = "<color=#FF5A5A>CRIT</color> <color=#5AE0FF>PERFECT</color>";
        else if (isCrit) label = "<color=#FF5A5A>CRIT!</color>";
        else if (isPerfect) label = "<color=#5AE0FF>PERFECT!</color>";
        
        if (bonusPoints > 0)
        {
            // Combo triggered - show the multiplier and the bonus it earned
            float multiplier = comboSystem.GetCurrentMultiplier();
            displayText = $"<size=55%>COMBO</size>\n{multiplier:0.#}x\n<size=50%>+{basePoints + bonusPoints}</size>";
            color = new Color(1f, 0.82f, 0.2f);
            size = 1.6f;
            duration = 0.9f;
            rise = 70f;
        }
        else if (basePoints < 0)
        {
            // Snake hit - show penalty
            displayText = $"{basePoints}";
            color = new Color(1f, 0.3f, 0.3f);
            size = 1.3f;
            duration = 0.7f;
            rise = 30f;
        }
        else
        {
            // Building toward a combo - show progress and warm the color as it gets closer
            var comboInfo = comboSystem.GetComboInfo();
            int required = Mathf.Max(1, comboInfo.requiredCount);
            float progress = Mathf.Clamp01(comboInfo.currentCount / (float)required);
            displayText = $"{comboInfo.currentCount}<size=55%>/{required}</size>";
            color = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.15f), progress);
            size = Mathf.Lerp(0.9f, 1.25f, progress);
            duration = 0.55f;
            rise = 45f;
        }
        
        if (label.Length > 0)
        {
            displayText = $"<size=45%>{label}</size>\n{displayText}";
        }
        
        // Instantiate and initialize
        GameObject floatingTextObj = Instantiate(comboTextPrefab, canvasTransform);
        FloatingComboText floatingText = floatingTextObj.GetComponent<FloatingComboText>();
        
        if (floatingText != null)
        {
            floatingText.SetStyle(color, size, duration, rise);
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
            return;
        }
        
        // Show "Miss!" when a combo breaks; otherwise a gentle grey puff so players still notice the escape
        var comboInfo = comboSystem.GetComboInfo();
        if (comboInfo.currentCount > 0 && !hasShownMissThisFrame)
        {
            SpawnMissText(animalRectTransform);
            hasShownMissThisFrame = true;
            Haptics.Play(Haptics.Style.Medium);
        }
        else if (animalRectTransform != null)
        {
            GameFeel.Burst(canvasTransform, animalRectTransform.position, new Color(0.7f, 0.7f, 0.7f, 0.6f), 6, 160f, 16f, 0.35f);
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
        
        GameObject floatingTextObj = Instantiate(comboTextPrefab, canvasTransform);
        FloatingComboText floatingText = floatingTextObj.GetComponent<FloatingComboText>();
        
        if (floatingText != null)
        {
            floatingText.SetStyle(new Color(1f, 0.55f, 0.5f), 0.9f, 0.6f, 20f);
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
                return SnakePenalty;
            default:
                return 5;
        }
    }
    
    /// <summary>
    /// Snake penalty comes from AnimalDatabase (currently -35) so code and data can't drift apart again.
    /// </summary>
    private int SnakePenalty
    {
        get
        {
            if (cachedSnakePenalty == 0)
            {
                cachedSnakePenalty = DefaultSnakePenalty;
                AnimalDatabase database = Resources.Load<AnimalDatabase>("AnimalDatabase");
                AnimalData snake = database != null ? database.GetAnimalByType(AnimalType.Snake) : null;
                if (snake != null && snake.pointValue < 0)
                {
                    cachedSnakePenalty = snake.pointValue;
                }
            }
            return cachedSnakePenalty;
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
        
        PlaytestTelemetry.Instance?.PrepareWaveEnd();

        // Stop spawning and force hide all active animals
        if (moleSpawner != null)
        {
            moleSpawner.StopSpawning();
        }

        PlaytestTelemetry.Instance?.EndWave(success, currentScore);

        if (!success)
        {
            PlaytestTelemetry.Instance?.EndRound(false);
        }
        else if (currentWave == 3)
        {
            PlaytestTelemetry.Instance?.EndRound(true);
        }
        
        if (MusicPlayer.Instance != null) MusicPlayer.Instance.SetQuiet();
        UpdateTimerUI(); // clears the red "urgent" timer
        
        if (success)
        {
            if (currentRound == 1 && currentWave == 3)
            {
                PlayerPrefs.SetInt(TutorialDonePrefKey, 1);
            }
            
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
            
            // Show combo count immediately, but delay gold earned display
            if (rewardGoldText != null)
            {
                rewardGoldText.text = $"Combos: {combosCompleted}\nGold Earned: $0";
            }
            
            // Start with empty dollar symbols - will be filled by coroutine
            if (rewardGoldSymbolsText != null)
            {
                rewardGoldSymbolsText.text = "";
            }
        }
        
        // Play coin glimmer sound for each gold earned and update display progressively
        if (goldEarned > 0)
        {
            skipCoinAnimation = false;
            if (coinSoundsCoroutine != null) StopCoroutine(coinSoundsCoroutine);
            coinSoundsCoroutine = StartCoroutine(PlayCoinGlimmerSounds(goldEarned, combosCompleted));
        }
        else
        {
            // No gold earned, show immediately
            if (rewardGoldText != null)
            {
                rewardGoldText.text = $"Combos: {combosCompleted}\nGold Earned: $0";
            }
        }
    }
    
    /// <summary>
    /// Continue from reward panel to shop
    /// </summary>
    public void ContinueToShop()
    {
        // Don't let the coin count-up keep playing over the shop
        if (coinSoundsCoroutine != null)
        {
            StopCoroutine(coinSoundsCoroutine);
            coinSoundsCoroutine = null;
        }
        
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
        
        // Play button press sound
        PlayButtonPressSound();
        
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
        
        if (PlayerPrefs.GetInt(ShopTipSeenPrefKey, 0) == 0)
        {
            PlayerPrefs.SetInt(ShopTipSeenPrefKey, 1);
            GameFeel.Banner(canvasTransform, UIFont, "Spend combo gold on upgrades\n<size=60%>Tap one to see what it does</size>", ScoreBarDoneColor, 70f, 2.2f, 300f);
        }
        
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
                int target = currentWaveData?.pointTarget ?? 0;
                int shortBy = Mathf.Max(0, target - currentScore);
                gameOverMessageText.text = $"Game Over\n\n{currentScore} / {target}\n<size=70%>{shortBy} points short</size>\n\nReached: Round {currentRound} Wave {currentWave}";
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
            bool reached = currentScore >= target && target > 0;
            
            scoreText.text = $"Score: {currentScore} / {target}";
            scoreText.color = reached ? ScoreBarDoneColor : scoreBaseColor;
            
            // Celebrate the moment the target is reached (once per wave)
            if (reached && !targetReachedThisWave && isWaveActive)
            {
                targetReachedThisWave = true;
                if (scorePulseCoroutine != null) StopCoroutine(scorePulseCoroutine);
                scorePulseCoroutine = StartCoroutine(PulseScoreText());
                GameFeel.Banner(canvasTransform, UIFont, "TARGET HIT!\n<size=45%>Combos still earn gold</size>", ScoreBarDoneColor, 90f, 0.7f, 250f);
                Haptics.Play(Haptics.Style.Medium);
            }
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
            if (!timerColorCached)
            {
                timerBaseColor = timerText.color;
                timerColorCached = true;
            }
            
            int seconds = Mathf.CeilToInt(Mathf.Max(0, timeRemaining));
            timerText.text = $"Time: {seconds}s";
            
            // Last 5 seconds: timer turns red and the music speeds up a little
            bool urgent = isWaveActive && timeRemaining <= 5f && timeRemaining > 0f;
            timerText.color = urgent ? new Color(1f, 0.3f, 0.25f) : timerBaseColor;
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.SetUrgent(urgent);
        }
        
        UpdateScoreBar();
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
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.SetQuiet();
            
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
            // Play button press sound
            PlayButtonPressSound();
            
            isPaused = false;
            Time.timeScale = 1f;
            if (MusicPlayer.Instance != null && isWaveActive) MusicPlayer.Instance.SetGameplay();
            
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
        // Play button press sound
        PlayButtonPressSound();
        
        // Make sure time scale is restored
        Time.timeScale = 1f;
        isPaused = false;
        
        Debug.Log("[Roguelike] Returning to Main Menu");
        SceneManager.LoadScene("MainMenuScene");
    }
    
    /// <summary>
    /// Play button press sound effect
    /// </summary>
    private void PlayButtonPressSound()
    {
        if (audioSource != null && buttonPressSound != null)
        {
            audioSource.PlayOneShot(buttonPressSound);
        }
    }
    
    /// <summary>
    /// Play coin glimmer sound multiple times (once per gold earned) and update display progressively
    /// </summary>
    private IEnumerator PlayCoinGlimmerSounds(int goldEarned, int combosCompleted)
    {
        if (audioSource == null || coinGlimmerSound == null) yield break;
        
        // Play the sound multiple times with a small delay between each
        // Display one $ symbol per sound and increment the gold count
        for (int i = 0; i < goldEarned; i++)
        {
            // Tapping the reward panel jumps straight to the total
            if (skipCoinAnimation)
            {
                if (rewardGoldText != null)
                {
                    rewardGoldText.text = $"Combos: {combosCompleted}\nGold Earned: ${goldEarned}";
                }
                if (rewardGoldSymbolsText != null)
                {
                    rewardGoldSymbolsText.text = new string('$', goldEarned);
                }
                audioSource.PlayOneShot(coinGlimmerSound);
                break;
            }
            
            // Play the glimmer sound
            audioSource.PlayOneShot(coinGlimmerSound);
            
            // Current gold count (i + 1 because we start at 0)
            int currentGold = i + 1;
            
            // Update gold earned text to show current count
            if (rewardGoldText != null)
            {
                rewardGoldText.text = $"Combos: {combosCompleted}\nGold Earned: ${currentGold}";
            }
            
            // Add one $ symbol to the display
            if (rewardGoldSymbolsText != null)
            {
                rewardGoldSymbolsText.text += "$";
            }
            
            // Wait a bit between each sound (adjust delay as needed)
            // Using 0.1 seconds between sounds - can be adjusted for better feel
            yield return new WaitForSeconds(0.1f);
        }
        coinSoundsCoroutine = null;
    }

    private void EnsurePlaytestTelemetry()
    {
        if (PlaytestTelemetry.Instance == null)
        {
            GameObject telemetryObj = new GameObject("PlaytestTelemetry");
            telemetryObj.AddComponent<PlaytestTelemetry>();
        }
    }
}

