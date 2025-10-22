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
    
    // Runtime spawn settings (configured by RoguelikeManager upgrades)
    private float currentMinSpawnInterval = 0.5f;
    private float currentMaxSpawnInterval = 1.5f;
    
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
            int currentRegularAnimals = currentActiveRabbits + currentActiveMeerkats;
            
            // Get max active animals from upgrades (if in roguelike mode)
            int maxRegularAnimals = 7;  // Default
            if (RoguelikeManager.Instance != null)
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
            for (int i = 0; i < animalsToSpawn; i++)
            {
                SpawnRegularAnimal();
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
            
            for (int i = 0; i < snakesToSpawn; i++)
            {
                SpawnSnake();
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
    private void SpawnRegularAnimal()
    {
        List<MoleController> availableMoles = GetAvailableMoles();

        if (availableMoles.Count > 0)
        {
            // Pick a random available mole
            int randomIndex = Random.Range(0, availableMoles.Count);
            MoleController selectedMole = availableMoles[randomIndex];
            
            // Update last spawn time and spawn the mole
            lastSpawnTimes[selectedMole] = Time.time;
            selectedMole.PopUp();
        }
    }
    
    /// <summary>
    /// Spawns a snake in a random available hole (snake event system)
    /// </summary>
    private void SpawnSnake()
    {
        List<MoleController> availableMoles = GetAvailableMoles();

        if (availableMoles.Count > 0)
        {
            // Pick a random available mole
            int randomIndex = Random.Range(0, availableMoles.Count);
            MoleController selectedMole = availableMoles[randomIndex];
            
            // Update last spawn time and force spawn as snake
            lastSpawnTimes[selectedMole] = Time.time;
            selectedMole.PopUpSnake();
        }
    }
    
    /// <summary>
    /// Spawns a mole in a random available hole (legacy method for other modes)
    /// </summary>
    private void SpawnRandomMole()
    {
        SpawnRegularAnimal();
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
    /// </summary>
    public bool CanSpawnRabbit()
    {
        int limit = maxActiveRabbits;
        
        // Check for wave-specific limits in roguelike mode
        if (RoguelikeManager.Instance != null)
        {
            var waveData = RoguelikeManager.Instance.GetCurrentWaveData();
            if (waveData != null)
            {
                limit = waveData.maxRabbits;
            }
        }
        
        return currentActiveRabbits < limit;
    }
    
    /// <summary>
    /// Called by MoleController to check if a meerkat can spawn
    /// </summary>
    public bool CanSpawnMeerkat()
    {
        int limit = maxActiveMeerkats;
        
        // Check for wave-specific limits in roguelike mode
        if (RoguelikeManager.Instance != null)
        {
            var waveData = RoguelikeManager.Instance.GetCurrentWaveData();
            if (waveData != null)
            {
                limit = waveData.maxMeerkats;
            }
        }
        
        return currentActiveMeerkats < limit;
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
    /// Set spawn intervals (called by RoguelikeManager when upgrades change)
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

    /// <summary>
    /// Counts the number of currently active or animating moles
    /// </summary>
    private int CountActiveMoles()
    {
        int count = 0;
        
        foreach (MoleController mole in moleControllers)
        {
            if (mole != null && mole.IsActiveOrAnimating())
            {
                count++;
            }
        }

        return count;
    }
}

