# Whack-a-Mole Roguelike: Balancing Report

## Animal Statistics

| Animal  | Points | Visible Time | Spawn Weight | Description        |
|---------|--------|--------------|--------------|-------------------|
| Rabbit  | +5pt   | 1.0s         | 1.0          | Fast, low value   |
| Meerkat | +10pt  | 1.5s         | 0.8          | Medium            |
| Fox     | +15pt  | 1.2s         | 0.6          | Quick, valuable   |
| Badger  | +25pt  | 2.5s         | 0.4          | Slow, high value  |
| Snake   | -10pt  | 1.8s         | 0.3          | PENALTY - Avoid!  |

---

## Combo Tier Progression

| Unlocked After | Hits to Start | Multiplier | Active Hits | Description |
|----------------|---------------|------------|-------------|-------------|
| Start          | ∞             | 1.0x       | 0           | No combo    |
| Boss 1         | 3             | 2.0x       | 5           | Basic combo |
| Boss 2         | 2             | 2.5x       | 8           | Improved    |
| Boss 3         | 2             | 3.0x       | 12          | Advanced    |
| Boss 4         | 1             | 3.5x       | 15          | Master      |

---

## Round-by-Round Breakdown

### ROUND 1 (No Combo)
**Available Animals:** Rabbit, Meerkat  
**Combo Tier:** None (1.0x multiplier)

#### Wave 1 (Warm-up)
- **Target:** 50 points
- **Duration:** 8 seconds
- **Spawn Rate:** 1-3 animals/second
- **Composition:**
  - Rabbit: 10x (50 pts)
- **Total:** 10 animals, 50 raw points

#### Wave 2 (Standard)
- **Target:** 100 points
- **Duration:** 10 seconds
- **Spawn Rate:** 1-3 animals/second
- **Composition:**
  - Rabbit: 20x (100 pts)
- **Total:** 20 animals, 100 raw points

#### Wave 3 (BOSS)
- **Target:** 200 points
- **Duration:** 12 seconds
- **Spawn Rate:** 2-5 animals/second
- **Composition:**
  - Rabbit: 12x (60 pts)
  - Meerkat: 14x (140 pts)
- **Total:** 26 animals, 200 raw points

---

### ROUND 2 (After Boss 1: 2x Combo)
**Available Animals:** Rabbit, Meerkat, Fox, Snake  
**Combo Tier:** 3 hits → 2.0x (5 hits)  
**Average Multiplier:** ~1.6x (accounting for combo uptime)

#### Wave 1
- **Target:** 75 points
- **Duration:** 9 seconds
- **Spawn Rate:** 1-4 animals/second
- **Composition:**
  - Rabbit: 6x (30 pts)
  - Meerkat: 3x (30 pts)
  - Fox: 1x (15 pts)
  - Snake: 2x (-20 pts)
- **Raw Points:** 55 pts × 1.6 combo × 0.75 accuracy = ~66 expected
- **Total:** 12 animals

#### Wave 2
- **Target:** 150 points
- **Duration:** 11 seconds
- **Spawn Rate:** 1-4 animals/second
- **Composition:**
  - Rabbit: 12x (60 pts)
  - Meerkat: 7x (70 pts)
  - Fox: 2x (30 pts)
  - Snake: 4x (-40 pts)
- **Raw Points:** 120 pts × 1.6 × 0.75 = ~144 expected
- **Total:** 25 animals

#### Wave 3 (BOSS)
- **Target:** 300 points
- **Duration:** 13 seconds
- **Spawn Rate:** 2-6 animals/second
- **Composition:**
  - Rabbit: 16x (80 pts)
  - Meerkat: 14x (140 pts)
  - Fox: 4x (60 pts)
  - Snake: 6x (-60 pts)
- **Raw Points:** 220 pts × 1.6 × 0.75 = ~264 expected
- **Total:** 40 animals

---

### ROUND 3 (Still 2x Combo)
**Available Animals:** Rabbit, Meerkat, Fox, Snake  
**Combo Tier:** 3 hits → 2.0x (5 hits)  
**Scaling Factor:** 1.5x from Round 2

#### Wave 1
- **Target:** 113 points (75 × 1.5)
- **Duration:** 10 seconds
- **Spawn Rate:** 2-5 animals/second
- **Composition:**
  - Rabbit: 8x (40 pts)
  - Meerkat: 5x (50 pts)
  - Fox: 2x (30 pts)
  - Snake: 3x (-30 pts)
- **Total:** 18 animals, ~90 raw pts

#### Wave 2
- **Target:** 225 points (150 × 1.5)
- **Duration:** 12 seconds
- **Spawn Rate:** 2-5 animals/second
- **Composition:**
  - Rabbit: 15x (75 pts)
  - Meerkat: 10x (100 pts)
  - Fox: 4x (60 pts)
  - Snake: 5x (-50 pts)
- **Total:** 34 animals, ~185 raw pts

#### Wave 3 (BOSS)
- **Target:** 450 points (300 × 1.5)
- **Duration:** 14 seconds
- **Spawn Rate:** 3-7 animals/second
- **Composition:**
  - Rabbit: 22x (110 pts)
  - Meerkat: 18x (180 pts)
  - Fox: 7x (105 pts)
  - Snake: 9x (-90 pts)
- **Total:** 56 animals, ~305 raw pts

---

### ROUND 4 (After Boss 3: 2.5x Combo)
**Available Animals:** Rabbit, Meerkat, Fox, Badger, Snake  
**Combo Tier:** 2 hits → 2.5x (8 hits)  
**Average Multiplier:** ~2.0x

#### Wave 1
- **Target:** 169 points
- **Duration:** 11 seconds
- **Spawn Rate:** 2-5 animals/second
- **Composition:**
  - Meerkat: 6x (60 pts)
  - Fox: 4x (60 pts)
  - Badger: 2x (50 pts)
  - Snake: 2x (-20 pts)
- **Total:** 14 animals, ~150 raw pts

#### Wave 2
- **Target:** 338 points
- **Duration:** 13 seconds
- **Spawn Rate:** 2-5 animals/second
- **Composition:**
  - Meerkat: 10x (100 pts)
  - Fox: 7x (105 pts)
  - Badger: 3x (75 pts)
  - Snake: 4x (-40 pts)
- **Total:** 24 animals, ~240 raw pts

#### Wave 3 (BOSS)
- **Target:** 675 points
- **Duration:** 15 seconds
- **Spawn Rate:** 3-7 animals/second
- **Composition:**
  - Meerkat: 18x (180 pts)
  - Fox: 12x (180 pts)
  - Badger: 6x (150 pts)
  - Snake: 7x (-70 pts)
- **Total:** 43 animals, ~440 raw pts

---

### ROUND 5 (Still 2.5x Combo)
**Available Animals:** Rabbit, Meerkat, Fox, Badger, Snake  
**Scaling:** 1.5x from Round 4

#### Wave 1
- **Target:** 254 points
- **Duration:** 12 seconds
- **Spawn Rate:** 2-6 animals/second

#### Wave 2
- **Target:** 507 points
- **Duration:** 14 seconds
- **Spawn Rate:** 2-6 animals/second

#### Wave 3 (BOSS)
- **Target:** 1,013 points
- **Duration:** 16 seconds
- **Spawn Rate:** 3-8 animals/second

---

### ROUND 6 (After Boss 5: 3x Combo)
**Available Animals:** Meerkat, Fox, Badger, Snake (No more Rabbits!)  
**Combo Tier:** 2 hits → 3.0x (12 hits)  
**Average Multiplier:** ~2.4x

#### Wave 1
- **Target:** 381 points
- **Duration:** 13 seconds
- **Spawn Rate:** 3-6 animals/second

#### Wave 2
- **Target:** 761 points
- **Duration:** 15 seconds
- **Spawn Rate:** 3-6 animals/second

#### Wave 3 (BOSS)
- **Target:** 1,519 points
- **Duration:** 17 seconds
- **Spawn Rate:** 4-9 animals/second

---

### ROUND 7 (Still 3x Combo)
**Available Animals:** Meerkat, Fox, Badger, Snake  
**Scaling:** 1.5x from Round 6

#### Wave 1
- **Target:** 571 points
- **Duration:** 14 seconds
- **Spawn Rate:** 3-7 animals/second

#### Wave 2
- **Target:** 1,141 points
- **Duration:** 16 seconds
- **Spawn Rate:** 3-7 animals/second

#### Wave 3 (BOSS)
- **Target:** 2,279 points
- **Duration:** 18 seconds
- **Spawn Rate:** 4-10 animals/second

---

### ROUND 8 (After Boss 7: 3.5x Combo)
**Available Animals:** Meerkat, Fox, Badger, Snake  
**Combo Tier:** 1 hit → 3.5x (15 hits)  
**Average Multiplier:** ~3.0x (nearly always in combo)

#### Wave 1
- **Target:** 857 points
- **Duration:** 15 seconds
- **Spawn Rate:** 4-7 animals/second

#### Wave 2
- **Target:** 1,712 points
- **Duration:** 17 seconds
- **Spawn Rate:** 4-7 animals/second

#### Wave 3 (FINAL BOSS)
- **Target:** 3,418 points
- **Duration:** 19 seconds
- **Spawn Rate:** 4-10 animals/second

---

## Key Balancing Notes

### Assumptions
- **Player Accuracy:** 75% (miss 1 in 4 animals)
- **Combo Uptime:** 60% (player maintains combo most of the time)
- **Difficulty Scaling:** 1.5x per round (exponential)
- **Boss Multiplier:** 2x point threshold (double difficulty)

### Progression Philosophy
1. **Round 1:** Tutorial, learn mechanics, no combo
2. **Rounds 2-3:** Master basic combo, introduce variety
3. **Rounds 4-5:** Combo becomes essential, remove rabbits eventually
4. **Rounds 6-7:** High-value animals only, advanced combos required
5. **Round 8:** Final challenge, perfect combo execution needed

### Animal Introduction
- **Round 1:** Rabbit, Meerkat (basics)
- **Round 2-3:** Add Fox and Snake (variety)
- **Round 4-5:** Add Badger, all animals available
- **Round 6+:** Remove Rabbit (force high-value targeting)

### Why Exponential Scaling Works
Without combo upgrades, Round 8 would be **impossible**. The scaling forces players to:
- Master the combo system
- Choose upgrades strategically
- Improve accuracy and speed
- Plan animal priority (Badgers > Foxes > Meerkats)

---

## CSV Export Format

```csv
Round,Wave,Type,Target_Points,Duration_Sec,Min_Spawn_Rate,Max_Spawn_Rate,Rabbits,Meerkats,Foxes,Badgers,Snakes,Total_Animals,Raw_Points,Combo_Tier
1,1,W1,50,8,1,3,10,0,0,0,0,10,50,0
1,2,W2,100,10,1,3,20,0,0,0,0,20,100,0
1,3,BOSS,200,12,2,5,12,14,0,0,0,26,200,0
2,1,W1,75,9,1,4,6,3,1,0,2,12,55,1
2,2,W2,150,11,1,4,12,7,2,0,4,25,120,1
2,3,BOSS,300,13,2,6,16,14,4,0,6,40,220,1
...
```

---

## Usage Instructions

1. **In Unity Editor:**
   - Attach `RoguelikeBalancer.cs` to an empty GameObject
   - Attach `BalancingVisualizer.cs` to the same GameObject
   - Right-click the component → "Generate Formatted Report"
   - Check Console for detailed output
   - Find CSV at `Assets/balancing_export.csv`

2. **Adjusting Balance:**
   - Modify `ACCURACY_ASSUMPTION` (default 0.75)
   - Modify `SCALING_FACTOR` (default 1.5x per round)
   - Modify `BOSS_MULTIPLIER` (default 1.3x harder)
   - Adjust animal point values in `animalStats` array
   - Adjust combo tiers in `comboTiers` array

3. **Testing Difficulty:**
   - If rounds feel too easy: Increase `SCALING_FACTOR` to 1.6 or 1.7
   - If combo feels weak: Increase `comboMultiplier` values
   - If too many snakes: Reduce snake spawn percentage (currently 10-20%)
   - If not enough time: Increase duration scaling

---

## Recommended Power-Up Pricing

Based on these point thresholds, shop items should cost:

### Early Game (Rounds 1-3)
- Common upgrades: 20-30 points
- Rare upgrades: 50-75 points
- Epic upgrades: 100-150 points

### Mid Game (Rounds 4-6)
- Common: 50-100 points
- Rare: 150-250 points
- Epic: 300-500 points

### Late Game (Rounds 7-8)
- Common: 200-300 points
- Rare: 500-800 points
- Epic: 1000-1500 points

**Pricing Philosophy:** Shop upgrades should cost ~30-40% of the next wave's target, forcing strategic choices.

