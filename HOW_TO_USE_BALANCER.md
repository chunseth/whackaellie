# How to Use the Roguelike Balancer

## Quick Start (3 Steps)

### 1. Setup in Unity
```
1. Open Unity Editor
2. Create empty GameObject: Right-click Hierarchy → Create Empty
3. Name it "BalancingCalculator"
4. Add components:
   - Drag RoguelikeBalancer.cs onto GameObject
   - Drag BalancingVisualizer.cs onto GameObject
```

### 2. Run Calculations
```
1. Select "BalancingCalculator" GameObject in Hierarchy
2. In Inspector, find BalancingVisualizer component
3. Right-click component header → "Generate Formatted Report"
4. Check Console window for output
5. Find CSV file at: Assets/balancing_export.csv
```

### 3. Review & Adjust
```
1. Review point thresholds in Console
2. Check if difficulty curve feels right
3. Adjust parameters in RoguelikeBalancer Inspector:
   - Animal point values
   - Combo tier multipliers
   - Scaling factors
4. Re-run calculations
5. Repeat until satisfied
```

---

## What You Get

### Console Output
- Animal statistics table
- Combo tier progression
- Detailed round-by-round breakdown
- Wave composition (how many of each animal)
- Expected points with combo calculations
- Summary table of all 8 rounds

### CSV Export (balancing_export.csv)
- Importable into Excel/Google Sheets
- Every wave's data in one row
- Easy to create charts and graphs
- Can be loaded by game systems

### Markdown Documentation
- **ROGUELIKE_BALANCING.md**: Full breakdown with explanations
- **BALANCING_SUMMARY.md**: Quick reference tables
- **HOW_TO_USE_BALANCER.md**: This guide

---

## Understanding the Numbers

### Example Wave Output:
```
[R2W1] Target: 75pts | Duration: 9s | Spawn Rate: 1-4/s
  Rabbit: 6x (30 pts)
  Meerkat: 3x (30 pts)
  Fox: 1x (15 pts)
  Snake: 2x (-20 pts)
  Total: 12 animals | Raw: 55pts | Expected w/combo: 66pts
  Avg Combo Mult: 1.60x | Accuracy: 75%
```

**What this means:**
- **Target: 75pts** - Player needs to score 75 points to complete wave
- **Duration: 9s** - Wave lasts 9 seconds
- **Spawn Rate: 1-4/s** - Between 1 and 4 animals spawn per second
- **Rabbit: 6x** - 6 rabbits will spawn (worth 5pts each = 30pts)
- **Total: 12 animals** - 12 animals total will spawn
- **Raw: 55pts** - If player hits all animals = 55 points
- **Expected: 66pts** - With 75% accuracy + combo = 66 points
- **Combo Mult: 1.60x** - Average multiplier assuming combo maintenance

**The Challenge:** Player needs 75pts but expected score is only 66pts.  
**Solution:** Need better accuracy, maintain combo longer, or have power-ups!

---

## Adjustable Parameters

### In RoguelikeBalancer Component:

#### Animal Stats Array
```csharp
[0] Rabbit
  - Base Points: 5
  - Spawn Weight: 1.0  (more likely to spawn)
  - Visible Duration: 1.0s (how long it stays up)
  - Description: "Fast, low value"
```
**Adjust to:**
- Make animals worth more/less points
- Make them appear more/less frequently
- Make them easier/harder to hit (duration)

#### Combo Tiers Array
```csharp
[1] After Boss 1
  - Hits To Start Combo: 3
  - Combo Multiplier: 2.0x
  - Combo Active Hits: 5
  - Description: "Basic: 3 hits for 2x (5 hits)"
```
**Adjust to:**
- Make combo easier/harder to start
- Increase/decrease multiplier strength
- Make combo last longer/shorter

#### Constants (in code)
```csharp
ACCURACY_ASSUMPTION = 0.75f   // Assume 75% accuracy
SCALING_FACTOR = 1.5f         // 1.5x harder each round
BOSS_MULTIPLIER = 1.3f        // Boss waves 30% harder
```
**Edit these in RoguelikeBalancer.cs around line 65**

---

## Common Adjustments

### "Round 8 is impossible!"
```
Option 1: Reduce scaling
  SCALING_FACTOR = 1.5f → 1.4f

Option 2: Buff combos
  Tier 4: 3.5x → 4.0x multiplier

Option 3: Assume better accuracy
  ACCURACY_ASSUMPTION = 0.75f → 0.80f

Option 4: Remove some snakes
  In CalculateAnimalComposition(), change:
  snakeCount = totalAnimals * 0.15f (was 0.2f)
```

### "Early rounds too easy!"
```
Option 1: Increase Round 1 targets
  Change Wave 1: 50 → 75
  Change Wave 2: 100 → 150
  Change Boss: 200 → 300

Option 2: Reduce early combos
  Tier 1: 2.0x → 1.8x multiplier

Option 3: Spawn more snakes early
  In GetAllowedAnimals(), add snakes to Round 1
```

### "Combos feel too weak!"
```
Option 1: Increase multipliers
  Tier 1: 2.0x → 2.5x
  Tier 2: 2.5x → 3.0x
  Tier 3: 3.0x → 3.5x
  Tier 4: 3.5x → 4.0x

Option 2: Make combo last longer
  Tier 1: 5 hits → 8 hits
  Tier 2: 8 hits → 12 hits
  etc.

Option 3: Easier to start
  Tier 1: 3 hits → 2 hits
  Tier 2: 2 hits → 1 hit
```

### "Not enough variety in animals!"
```
Option 1: Adjust spawn weights
  Make rare animals more common:
  Fox: 0.6 → 0.8
  Badger: 0.4 → 0.6

Option 2: Change availability
  In GetAllowedAnimals(), add:
  Round 1: Add Fox
  Round 2: Add Badger earlier

Option 3: More snakes for challenge
  Change snake spawn: 10-20% → 20-30%
```

---

## Integration into Your Game

### Using the WaveData in Code

```csharp
// Get wave data from balancer
RoguelikeBalancer balancer = FindObjectOfType<RoguelikeBalancer>();
balancer.CalculateAllWaveBalancing();

// Get specific wave
var wave = balancer.GetWave(roundNumber: 2, waveNumber: 1);

// Access wave properties
int targetPoints = wave.pointThreshold;
float duration = wave.duration;
int minSpawnRate = wave.minSpawnRate;
int maxSpawnRate = wave.maxSpawnRate;

// Get animal counts
int rabbitCount = wave.animalCounts[AnimalType.Rabbit];
int meerkatCount = wave.animalCounts[AnimalType.Meerkat];

// Use in your spawner
foreach (var kvp in wave.animalCounts)
{
    AnimalType type = kvp.Key;
    int count = kvp.Value;
    
    // Spawn this many animals of this type
    SpawnAnimals(type, count);
}
```

### Creating a Wave Manager

```csharp
public class WaveManager : MonoBehaviour
{
    private RoguelikeBalancer balancer;
    private int currentRound = 1;
    private int currentWave = 1;
    
    void Start()
    {
        balancer = GetComponent<RoguelikeBalancer>();
        balancer.CalculateAllWaveBalancing();
        StartWave();
    }
    
    void StartWave()
    {
        var waveData = balancer.GetWave(currentRound, currentWave);
        
        // Set up UI
        GameManager.Instance.SetPointTarget(waveData.pointThreshold);
        
        // Configure spawner
        MoleSpawner spawner = FindObjectOfType<MoleSpawner>();
        spawner.SetSpawnRate(waveData.minSpawnRate, waveData.maxSpawnRate);
        spawner.SetWaveDuration(waveData.duration);
        spawner.SetAnimalComposition(waveData.animalCounts);
        
        // Start spawning
        spawner.StartSpawning();
    }
    
    public void OnWaveComplete()
    {
        currentWave++;
        
        if (currentWave > 3)
        {
            currentWave = 1;
            currentRound++;
            
            // Show combo upgrade screen after boss
            if (currentRound > 1)
            {
                ShowComboUpgradeScreen();
            }
        }
        
        StartWave();
    }
}
```

---

## Validation & Testing

### Manual Testing Checklist

**Round 1 (No Combo):**
- [ ] Can you complete with 70-80% accuracy?
- [ ] Does it feel like a tutorial?
- [ ] Boss introduces new animal (meerkat)?

**Round 2 (First Combo):**
- [ ] Does combo feel rewarding?
- [ ] Is 75pt target achievable with combo?
- [ ] Do you understand combo mechanics?

**Round 4 (Badger Unlock):**
- [ ] Is difficulty noticeably higher?
- [ ] Are badgers valuable targets?
- [ ] Does improved combo help significantly?

**Round 6 (No More Rabbits):**
- [ ] Does removal of rabbits change strategy?
- [ ] Are high-value targets prioritized?
- [ ] Is difficulty spike manageable?

**Round 8 (Final Challenge):**
- [ ] Is it nearly impossible without good build?
- [ ] Do power-ups feel essential?
- [ ] Is victory satisfying?

### Metrics to Track

Create a telemetry system to track:
```csharp
// Per wave
- Point target vs actual score
- Completion rate (% of players who pass)
- Average accuracy
- Average combo length
- Time to complete
- Animals missed vs hit

// Per round
- Combo tier usage
- Power-ups purchased
- Lives remaining
- Preferred animal targets

// Per run
- Furthest round reached
- Total playtime
- Build composition
- Quit rate per round
```

### Ideal Target Metrics

```
Round 1: 95% completion rate (tutorial)
Round 2: 85% completion rate
Round 3: 75% completion rate
Round 4: 60% completion rate (difficulty spike)
Round 5: 50% completion rate
Round 6: 35% completion rate
Round 7: 20% completion rate (elite)
Round 8: 10% completion rate (expert only)
```

If your actual metrics differ significantly, adjust balancing!

---

## Advanced: Custom Formulas

### Creating Your Own Scaling

Edit `CalculateBaseThreshold()` in RoguelikeBalancer.cs:

```csharp
// Current: Exponential (1.5x per round)
float multiplier = Mathf.Pow(SCALING_FACTOR, roundNumber - 1);

// Alternative 1: Linear (add 50 per round)
float multiplier = roundNumber * 0.5f;

// Alternative 2: Quadratic (round squared)
float multiplier = roundNumber * roundNumber * 0.1f;

// Alternative 3: Custom curve
float[] customScaling = {1f, 1.3f, 1.8f, 2.5f, 3.5f, 5f, 7f, 10f};
float multiplier = customScaling[roundNumber - 1];

// Alternative 4: Logarithmic (gentler late game)
float multiplier = Mathf.Log(roundNumber + 1, 1.5f);
```

### Custom Animal Distribution

Edit `CalculateAnimalComposition()` in RoguelikeBalancer.cs:

```csharp
// Current: Weight-based distribution

// Alternative: Specific ratios
composition[AnimalType.Rabbit] = totalAnimals * 0.4f;   // 40% rabbits
composition[AnimalType.Meerkat] = totalAnimals * 0.3f;  // 30% meerkats
composition[AnimalType.Fox] = totalAnimals * 0.2f;      // 20% foxes
composition[AnimalType.Badger] = totalAnimals * 0.1f;   // 10% badgers

// Alternative: Boss-specific compositions
if (isBoss)
{
    // Boss waves have more high-value targets
    composition[AnimalType.Badger] *= 2;
    composition[AnimalType.Fox] *= 1.5f;
}
```

---

## Troubleshooting

### "Calculator shows weird numbers"
- Check that AnimalType enum matches your game's enum
- Verify all animals have point values assigned
- Make sure combo tiers array has 5 entries (0-4)

### "CSV won't export"
- Check Console for errors
- Verify write permissions on Assets folder
- Try running as Administrator (Windows)

### "Numbers don't match gameplay"
- Calculator assumes 75% accuracy - test actual player accuracy
- Calculator assumes 60% combo uptime - check if realistic
- Power-ups in game may change balance significantly

### "Console output is empty"
- Make sure "Show Detailed Logs" is checked in Inspector
- Check Console filters (enable Info/Warning/Error)
- Try calling from code instead of context menu

---

## Summary

✅ **Created:**
- RoguelikeBalancer.cs - Calculation engine
- BalancingVisualizer.cs - Output formatter
- Complete documentation set

✅ **You can:**
- Generate balanced point thresholds for 8 rounds
- See detailed animal compositions per wave
- Export to CSV for analysis
- Adjust all parameters easily
- Iterate quickly on balance

✅ **Next steps:**
1. Run calculator in Unity
2. Review numbers in Console
3. Tweak parameters to taste
4. Integrate into game systems
5. Playtest and iterate!

**Good luck with your roguelike!** 🎮🐰

---

*Questions? Check the code comments or modify the scripts to fit your needs. The balancer is designed to be flexible and hackable!*

