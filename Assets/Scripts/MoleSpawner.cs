using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoleSpawner : MonoBehaviour
{
    [Header("Mole Grid References")]
    [SerializeField] private MoleController[] moleControllers;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 1.5f;
    [SerializeField] private int minActiveMoles = 1;
    [SerializeField] private int maxActiveMoles = 9;  // Total animals (rabbits + snakes)
    [SerializeField] private float holeCooldown = 1f;
    
    // Runtime spawn settings (configured by RoguelikeManager upgrades or GameManager survival mode)
    private float currentMinSpawnInterval = 0.5f;
    private float currentMaxSpawnInterval = 1.5f;
    private int configuredMaxActiveAnimals = -1;  // -1 means use default logic
    
    [Header("Animal Type Limits")]
    [SerializeField] private int maxActiveRabbits = 7;    // Max rabbits at once
    [SerializeField] private int maxActiveMeerkats = 3;   // Max meerkats at once
    [SerializeField] private int maxActiveSnakes = 2;     // Max snakes at once
    
    [Header("Snake Event Spawning")]
    [SerializeField] private float snakeEventMinInterval = 1f;   // Min seconds between snake events
    [SerializeField] private float snakeEventMaxInterval = 2.5f; // Max seconds between snake events
    [SerializeField] private int snakesPerEvent = 2;        // Snakes to spawn per event

    private bool isSpawning = false;
    private Dictionary<MoleController, float> lastSpawnTimes = new Dictionary<MoleController, float>();
    private Coroutine regularSpawnCoroutine;
    private Coroutine snakeSpawnCoroutine;
    private int currentActiveSnakes = 0;
    private int currentActiveRabbits = 0;
    private int currentActiveMeerkats = 0;
    private int currentActiveFoxes = 0;
    private int currentActiveBadgers = 0;

    private void Start()
    {
        // Initialize current spawn intervals from defaults
        currentMinSpawnInterval = minSpawnInterval;
        currentMaxSpawnInterval = maxSpawnInterval;
        
        // Initialize last spawn times
        foreach (MoleController mole in moleControllers)
        {
            if (mole != null)
            {
                lastSpawnTimes[mole] = -holeCooldown; // Allow immediate spawn
            }
        }
    }

    /// <summary>
    /// Starts spawning moles
    /// </summary>
    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            
            // Start regular animal spawning (rabbits/meerkats)
            if (regularSpawnCoroutine != null)
            {
                StopCoroutine(regularSpawnCoroutine);
            }
            regularSpawnCoroutine = StartCoroutine(SpawnRegularAnimalsCoroutine());
            
            // Start snake event spawning if in roguelike mode with snakes enabled
            if (RoguelikeManager.Instance != null)
            {
                var waveData = RoguelikeManager.Instance.GetCurrentWaveData();
                if (waveData != null && waveData.allowedAnimals.Contains(AnimalType.Snake))
                {
                    if (snakeSpawnCoroutine != null)
                    {
                        StopCoroutine(snakeSpawnCoroutine);
                    }
                    snakeSpawnCoroutine = StartCoroutine(SpawnSnakeEventsCoroutine());
                    Debug.Log("[Spawner] Snake event spawning enabled!");
                }
            }
        }
    }

    /// <summary>
    /// Stops spawning moles and hides all active moles
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
        
        if (regularSpawnCoroutine != null)
        {
            StopCoroutine(regularSpawnCoroutine);
            regularSpawnCoroutine = null;
        }
        
        if (snakeSpawnCoroutine != null)
        {
            StopCoroutine(snakeSpawnCoroutine);
            snakeSpawnCoroutine = null;
        }

        // Force hide all moles
        foreach (MoleController mole in moleControllers)
        {
            if (mole != null)
            {
                mole.ForceHide();
            }
        }
    }

    /// <summary>
    /// Coroutine that spawns regular animals (rabbits/meerkats) - NOT snakes
    /// </summary>
    private IEnumerator SpawnRegularAnimalsCoroutine()
    {
        while (isSpawning)
        {
            // Wait for random interval before next spawn attempt
            // Use current values which can be modified by upgrades
            float waitTime = Random.Range(currentMinSpawnInterval, currentMaxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // Count currently active regular animals (not snakes)
            int currentRegularAnimals = currentActiveRabbits + currentActiveMeerkats + currentActiveFoxes + currentActiveBadgers;
            
            // Get max active animals from configuration
            int maxRegularAnimals = 7;  // Default
            
            // Check if manually configured (survival mode or other modes)
            if (configuredMaxActiveAnimals > 0)
            {
                maxRegularAnimals = configuredMaxActiveAnimals;
            }
            // Otherwise check roguelike manager
            else if (RoguelikeManager.Instance != null)
            {
                var upgrades = RoguelikeManager.Instance.GetPlayerUpgrades();
                if (upgrades != null)
                {
                    maxRegularAnimals = upgrades.maxActiveAnimals;
                }
            }
            
            // Determine how many regular animals should be active (don't count snakes)
            int targetRegularAnimals = Random.Range(minActiveMoles, maxRegularAnimals + 1);
            
            // Spawn regular animals to reach target
            int animalsToSpawn = targetRegularAnimals - currentRegularAnimals;
            int activeBeforeBatch = currentRegularAnimals;

            if (animalsToSpawn > 0 && PlaytestTelemetry.Instance != null)
            {
                PlaytestTelemetry.Instance.SetSpawnBatchSize(animalsToSpawn);
            }

            if (animalsToSpawn > 0 && GetAvailableMoles().Count == 0)
            {
                PlaytestTelemetry.Instance?.LogSpawnBlocked(
                    PlaytestTelemetry.Instance.WaveElapsed,
                    activeBeforeBatch,
                    maxRegularAnimals,
                    animalsToSpawn);
            }

            for (int i = 0; i < animalsToSpawn; i++)
            {
                int activeBefore = currentActiveRabbits + currentActiveMeerkats + currentActiveFoxes + currentActiveBadgers;
                bool spawned = SpawnRegularAnimal(activeBefore, maxRegularAnimals, animalsToSpawn);
                if (!spawned && activeBefore >= maxRegularAnimals)
                {
                    PlaytestTelemetry.Instance?.LogSpawnBlocked(
                        PlaytestTelemetry.Instance.WaveElapsed,
                        activeBefore,
                        maxRegularAnimals,
                        animalsToSpawn - i);
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Coroutine that spawns snake events independently
    /// </summary>
    private IEnumerator SpawnSnakeEventsCoroutine()
    {
        while (isSpawning)
        {
            // Wait for snake event interval
            float waitTime = Random.Range(snakeEventMinInterval, snakeEventMaxInterval);
            yield return new WaitForSeconds(waitTime);
            
            // Spawn snakes for this event
            int snakesToSpawn = Mathf.Min(snakesPerEvent, maxActiveSnakes - currentActiveSnakes);
            
            if (snakesToSpawn > 0 && PlaytestTelemetry.Instance != null)
            {
                PlaytestTelemetry.Instance.SetSpawnBatchSize(snakesToSpawn);
            }

            for (int i = 0; i < snakesToSpawn; i++)
            {
                int activeBefore = currentActiveRabbits + currentActiveMeerkats + currentActiveFoxes + currentActiveBadgers;
                SpawnSnake(activeBefore, GetMaxActiveAllowed());
            }
            
            if (snakesToSpawn > 0)
            {
                Debug.Log($"[Spawner] Snake event! Spawned {snakesToSpawn} snake(s)");
            }
        }
    }

    /// <summary>
    /// Spawns a regular animal (rabbit/meerkat) in a random available hole
    /// </summary>
    private bool SpawnRegularAnimal(int activeBefore, int maxActiveAllowed, int batchSize)
    {
        List<MoleController> availableMoles = GetAvailableMoles();
        bool blockedByCap = activeBefore >= maxActiveAllowed;

        if (availableMoles.Count > 0)
        {
            int randomIndex = Random.Range(0, availableMoles.Count);
            MoleController selectedMole = availableMoles[randomIndex];

            lastSpawnTimes[selectedMole] = Time.time;
            selectedMole.PopUp();
            return true;
        }

        if (batchSize > 0)
        {
            PlaytestTelemetry.Instance?.LogSpawnBlocked(
                PlaytestTelemetry.Instance.WaveElapsed,
                activeBefore,
                maxActiveAllowed,
                batchSize);
        }

        return false;
    }

    /// <summary>
    /// Spawns a snake in a random available hole (snake event system)
    /// </summary>
    private void SpawnSnake(int activeBefore, int maxActiveAllowed)
    {
        List<MoleController> availableMoles = GetAvailableMoles();

        if (availableMoles.Count > 0)
        {
            int randomIndex = Random.Range(0, availableMoles.Count);
            MoleController selectedMole = availableMoles[randomIndex];

            lastSpawnTimes[selectedMole] = Time.time;
            selectedMole.PopUpSnake();
        }
        else
        {
            PlaytestTelemetry.Instance?.LogSpawnBlocked(
                PlaytestTelemetry.Instance.WaveElapsed,
                activeBefore,
                maxActiveAllowed,
                1);
        }
    }

    /// <summary>
    /// Spawns a mole in a random available hole (legacy method for other modes)
    /// </summary>
    private void SpawnRandomMole()
    {
        int activeBefore = currentActiveRabbits + currentActiveMeerkats + currentActiveFoxes + currentActiveBadgers;
        SpawnRegularAnimal(activeBefore, GetMaxActiveAllowed(), 1);
    }
    
    /// <summary>
    /// Called by MoleController to check if a snake can spawn
    /// </summary>
    public bool CanSpawnSnake()
    {
        int limit = maxActiveSnakes;
        
        // Check for wave-specific limits in roguelike mode
        if (RoguelikeManager.Instance != null)
        {
            var waveData = RoguelikeManager.Instance.GetCurrentWaveData();
            if (waveData != null)
            {
                limit = waveData.maxSnakes;
            }
        }
        
        return currentActiveSnakes < limit;
    }
    
    /// <summary>
    /// Called by MoleController to check if a rabbit can spawn
    /// Uses total maxActiveAnimals cap instead of per-type caps
    /// </summary>
    public bool CanSpawnRabbit()
    {
        // Check total regular animals against maxActiveAnimals
        int maxRegularAnimals = maxActiveRabbits + maxActiveMeerkats; // Default for non-roguelike
        
        // Check if manually configured (survival mode or other modes)
        if (configuredMaxActiveAnimals > 0)
        {
            maxRegularAnimals = configuredMaxActiveAnimals;
        }
        // Otherwise check roguelike manager
        else if (RoguelikeManager.Instance != null)
        {
            var upgrades = RoguelikeManager.Instance.GetPlayerUpgrades();
            if (upgrades != null)
            {
                maxRegularAnimals = upgrades.maxActiveAnimals;
            }
        }
        
        int currentRegularAnimals = currentActiveRabbits + currentActiveMeerkats + currentActiveFoxes + currentActiveBadgers;
        return currentRegularAnimals < maxRegularAnimals;
    }
    
    /// <summary>
    /// Called by MoleController to check if a meerkat can spawn
    /// Uses total maxActiveAnimals cap instead of per-type caps
    /// </summary>
    public bool CanSpawnMeerkat()
    {
        // Check total regular animals against maxActiveAnimals
        int maxRegularAnimals = maxActiveRabbits + maxActiveMeerkats; // Default for non-roguelike
        
        // Check if manually configured (survival mode or other modes)
        if (configuredMaxActiveAnimals > 0)
        {
            maxRegularAnimals = configuredMaxActiveAnimals;
        }
        // Otherwise check roguelike manager
        else if (RoguelikeManager.Instance != null)
        {
            var upgrades = RoguelikeManager.Instance.GetPlayerUpgrades();
            if (upgrades != null)
            {
                maxRegularAnimals = upgrades.maxActiveAnimals;
            }
        }
        
        int currentRegularAnimals = currentActiveRabbits + currentActiveMeerkats + currentActiveFoxes + currentActiveBadgers;
        return currentRegularAnimals < maxRegularAnimals;
    }
    
    /// <summary>
    /// Called by MoleController to check if a fox can spawn
    /// Uses total maxActiveAnimals cap instead of per-type caps
    /// </summary>
    public bool CanSpawnFox()
    {
        // Check total regular animals against maxActiveAnimals
        int maxRegularAnimals = maxActiveRabbits + maxActiveMeerkats; // Default for non-roguelike
        
        // Check if manually configured (survival mode or other modes)
        if (configuredMaxActiveAnimals > 0)
        {
            maxRegularAnimals = configuredMaxActiveAnimals;
        }
        // Otherwise check roguelike manager
        else if (RoguelikeManager.Instance != null)
        {
            var upgrades = RoguelikeManager.Instance.GetPlayerUpgrades();
            if (upgrades != null)
            {
                maxRegularAnimals = upgrades.maxActiveAnimals;
            }
        }
        
        int currentRegularAnimals = currentActiveRabbits + currentActiveMeerkats + currentActiveFoxes + currentActiveBadgers;
        return currentRegularAnimals < maxRegularAnimals;
    }
    
    /// <summary>
    /// Called by MoleController to check if a badger can spawn
    /// Uses total maxActiveAnimals cap instead of per-type caps
    /// </summary>
    public bool CanSpawnBadger()
    {
        // Check total regular animals against maxActiveAnimals
        int maxRegularAnimals = maxActiveRabbits + maxActiveMeerkats; // Default for non-roguelike
        
        // Check if manually configured (survival mode or other modes)
        if (configuredMaxActiveAnimals > 0)
        {
            maxRegularAnimals = configuredMaxActiveAnimals;
        }
        // Otherwise check roguelike manager
        else if (RoguelikeManager.Instance != null)
        {
            var upgrades = RoguelikeManager.Instance.GetPlayerUpgrades();
            if (upgrades != null)
            {
                maxRegularAnimals = upgrades.maxActiveAnimals;
            }
        }
        
        int currentRegularAnimals = currentActiveRabbits + currentActiveMeerkats + currentActiveFoxes + currentActiveBadgers;
        return currentRegularAnimals < maxRegularAnimals;
    }
    
    /// <summary>
    /// Called by MoleController when a snake pops up
    /// </summary>
    public void OnSnakeSpawned()
    {
        currentActiveSnakes++;
    }
    
    /// <summary>
    /// Called by MoleController when a snake goes down
    /// </summary>
    public void OnSnakeHidden()
    {
        currentActiveSnakes--;
        if (currentActiveSnakes < 0) currentActiveSnakes = 0;
    }
    
    /// <summary>
    /// Called by MoleController when a rabbit pops up
    /// </summary>
    public void OnRabbitSpawned()
    {
        currentActiveRabbits++;
    }
    
    /// <summary>
    /// Called by MoleController when a rabbit goes down
    /// </summary>
    public void OnRabbitHidden()
    {
        currentActiveRabbits--;
        if (currentActiveRabbits < 0) currentActiveRabbits = 0;
    }
    
    /// <summary>
    /// Called by MoleController when a meerkat pops up
    /// </summary>
    public void OnMeerkatSpawned()
    {
        currentActiveMeerkats++;
    }
    
    /// <summary>
    /// Called by MoleController when a meerkat goes down
    /// </summary>
    public void OnMeerkatHidden()
    {
        currentActiveMeerkats--;
        if (currentActiveMeerkats < 0) currentActiveMeerkats = 0;
    }
    
    /// <summary>
    /// Called by MoleController when a fox pops up
    /// </summary>
    public void OnFoxSpawned()
    {
        currentActiveFoxes++;
    }
    
    /// <summary>
    /// Called by MoleController when a fox goes down
    /// </summary>
    public void OnFoxHidden()
    {
        currentActiveFoxes--;
        if (currentActiveFoxes < 0) currentActiveFoxes = 0;
    }
    
    /// <summary>
    /// Called by MoleController when a badger pops up
    /// </summary>
    public void OnBadgerSpawned()
    {
        currentActiveBadgers++;
    }
    
    /// <summary>
    /// Called by MoleController when a badger goes down
    /// </summary>
    public void OnBadgerHidden()
    {
        currentActiveBadgers--;
        if (currentActiveBadgers < 0) currentActiveBadgers = 0;
    }
    
    /// <summary>
    /// Set spawn intervals (called by RoguelikeManager or GameManager when spawn rates change)
    /// </summary>
    public void SetSpawnIntervals(float minSpawnRate, float maxSpawnRate)
    {
        // Spawn rate = animals per second
        // Interval = seconds between spawns
        // So if spawn rate increases, interval decreases
        
        // Convert spawn rate (animals/sec) to interval (sec/spawn)
        // minSpawnRate = 1/sec → maxInterval = 1.0s
        // maxSpawnRate = 3/sec → minInterval = 0.33s
        
        if (maxSpawnRate > 0)
        {
            currentMinSpawnInterval = 1f / maxSpawnRate;  // High rate = short interval
        }
        
        if (minSpawnRate > 0)
        {
            currentMaxSpawnInterval = 1f / minSpawnRate;  // Low rate = long interval
        }
        
        Debug.Log($"[Spawner] Spawn rate: {minSpawnRate}-{maxSpawnRate}/sec → Intervals: {currentMinSpawnInterval:F2}s - {currentMaxSpawnInterval:F2}s");
    }
    
    /// <summary>
    /// Set max active animals cap (called by GameManager for survival mode)
    /// </summary>
    public void SetMaxActiveAnimals(int maxAnimals)
    {
        configuredMaxActiveAnimals = maxAnimals;
        Debug.Log($"[Spawner] Max active animals set to: {maxAnimals}");
    }

    /// <summary>
    /// Gets a list of moles that are available to spawn (not active and not on cooldown)
    /// </summary>
    private List<MoleController> GetAvailableMoles()
    {
        List<MoleController> available = new List<MoleController>();

        foreach (MoleController mole in moleControllers)
        {
            if (mole != null)
            {
                // Check if mole is not active and cooldown has passed
                bool isOnCooldown = (Time.time - lastSpawnTimes[mole]) < holeCooldown;
                
                if (!mole.IsActiveOrAnimating() && !isOnCooldown)
                {
                    available.Add(mole);
                }
            }
        }

        return available;
    }

    public int GetMaxActiveAllowed()
    {
        if (configuredMaxActiveAnimals > 0)
        {
            return configuredMaxActiveAnimals;
        }

        if (RoguelikeManager.Instance != null)
        {
            var upgrades = RoguelikeManager.Instance.GetPlayerUpgrades();
            if (upgrades != null)
            {
                return upgrades.maxActiveAnimals;
            }
        }

        return maxActiveMoles;
    }

    public int GetActiveRegularCount()
    {
        return currentActiveRabbits + currentActiveMeerkats + currentActiveFoxes + currentActiveBadgers;
    }

    public void GetBoardTelemetryCounts(out int activeUnhit, out int hitAnimating, out int totalVisible)
    {
        activeUnhit = 0;
        hitAnimating = 0;
        totalVisible = 0;

        foreach (MoleController mole in moleControllers)
        {
            if (mole == null) continue;

            mole.GetTelemetryState(out bool unhitVisible, out bool animatingHit, out bool anyVisible);
            if (unhitVisible) activeUnhit++;
            if (animatingHit) hitAnimating++;
            if (anyVisible) totalVisible++;
        }
    }

    public void NotifyAnimalSpawned(AnimalType type, int activeAfter, int maxActiveAllowed, int activeBefore)
    {
        bool blockedByCap = activeBefore >= maxActiveAllowed;
        PlaytestTelemetry.Instance?.LogSpawn(
            PlaytestTelemetry.Instance.WaveElapsed,
            activeBefore,
            activeAfter,
            maxActiveAllowed,
            blockedByCap,
            type.ToString());
    }

    public int CountVisibleUnhitAnimals()
    {
        int count = 0;
        foreach (MoleController mole in moleControllers)
        {
            if (mole != null)
            {
                mole.GetTelemetryState(out bool unhitVisible, out _, out _);
                if (unhitVisible) count++;
            }
        }

        return count;
    }
}

