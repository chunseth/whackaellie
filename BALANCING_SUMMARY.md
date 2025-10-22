# Roguelike Balancing: Quick Reference

## 📊 Point Threshold Progression

```
Round | Wave 1  | Wave 2  | Boss    | Total   | Combo Tier
------|---------|---------|---------|---------|------------
  1   |   50    |  100    |  200    |   350   | None (1.0x)
  2   |   75    |  150    |  300    |   525   | T1 (2.0x)
  3   |  113    |  225    |  450    |   788   | T1 (2.0x)
  4   |  169    |  338    |  675    | 1,182   | T2 (2.5x)
  5   |  254    |  507    | 1,013   | 1,774   | T2 (2.5x)
  6   |  381    |  761    | 1,519   | 2,661   | T3 (3.0x)
  7   |  571    | 1,141   | 2,279   | 3,991   | T3 (3.0x)
  8   |  857    | 1,712   | 3,418   | 5,987   | T4 (3.5x)
```

## 🎯 Animal Values & Strategy

```
┌─────────┬────────┬──────────┬─────────────────────────┐
│ Animal  │ Points │ Duration │ Strategy                │
├─────────┼────────┼──────────┼─────────────────────────┤
│ Rabbit  │  +5pt  │  1.0s    │ Fast clicks, combo fuel │
│ Meerkat │ +10pt  │  1.5s    │ Reliable value          │
│ Fox     │ +15pt  │  1.2s    │ Quick, high priority    │
│ Badger  │ +25pt  │  2.5s    │ Easy target, big points │
│ Snake   │ -10pt  │  1.8s    │ AVOID! Combo breaker    │
└─────────┴────────┴──────────┴─────────────────────────┘
```

## 🔥 Combo System Progression

```
Tier 0 (Start):      No combo system
                     1.0x multiplier always
                     ↓
                  [BOSS 1]
                     ↓
Tier 1 (R2-R3):     ███ hits → 2.0x (█████ hits)
                     Hit 3 times to start, lasts 5 hits
                     Average multiplier: ~1.6x
                     ↓
                  [BOSS 3]
                     ↓
Tier 2 (R4-R5):     ██ hits → 2.5x (████████ hits)
                     Hit 2 times to start, lasts 8 hits
                     Average multiplier: ~2.0x
                     ↓
                  [BOSS 5]
                     ↓
Tier 3 (R6-R7):     ██ hits → 3.0x (████████████ hits)
                     Hit 2 times to start, lasts 12 hits
                     Average multiplier: ~2.4x
                     ↓
                  [BOSS 7]
                     ↓
Tier 4 (R8):        █ hit → 3.5x (███████████████ hits)
                     Hit 1 time to start, lasts 15 hits
                     Average multiplier: ~3.0x (nearly constant)
```

## 📈 Difficulty Scaling Formula

```
Base Threshold = 100 points (Round 1, Wave 2)

Round Threshold = Base × (1.5 ^ (round - 1))

Wave Thresholds:
  - Wave 1 = Round Threshold × 0.5  (warm-up)
  - Wave 2 = Round Threshold × 1.0  (standard)
  - Wave 3 = Round Threshold × 2.0  (boss)
```

### Example Calculations:
```
Round 4:
  Base = 100 × (1.5 ^ 3) = 100 × 3.375 = 337.5
  Wave 1 = 337.5 × 0.5 = 169 pts
  Wave 2 = 337.5 × 1.0 = 338 pts
  Wave 3 = 337.5 × 2.0 = 675 pts
```

## 🌊 Spawn Rate Progression

```
Round | Wave 1    | Wave 2    | Boss      | Notes
------|-----------|-----------|-----------|------------------
  1   | 1-3/sec   | 1-3/sec   | 2-5/sec   | Tutorial pace
  2   | 1-4/sec   | 1-4/sec   | 2-6/sec   | Slight increase
  3   | 2-5/sec   | 2-5/sec   | 3-7/sec   | Getting faster
  4   | 2-5/sec   | 2-5/sec   | 3-7/sec   | Sustained pace
  5   | 2-6/sec   | 2-6/sec   | 3-8/sec   | High intensity
  6   | 3-6/sec   | 3-6/sec   | 4-9/sec   | Expert level
  7   | 3-7/sec   | 3-7/sec   | 4-10/sec  | Near maximum
  8   | 4-7/sec   | 4-7/sec   | 4-10/sec  | Peak difficulty
```

## 🎲 Animal Availability by Round

```
Round 1:  🐰 Rabbit, 🦦 Meerkat
Round 2:  🐰 Rabbit, 🦦 Meerkat, 🦊 Fox, 🐍 Snake
Round 3:  🐰 Rabbit, 🦦 Meerkat, 🦊 Fox, 🐍 Snake
Round 4:  🐰 Rabbit, 🦦 Meerkat, 🦊 Fox, 🦡 Badger, 🐍 Snake
Round 5:  🐰 Rabbit, 🦦 Meerkat, 🦊 Fox, 🦡 Badger, 🐍 Snake
Round 6:  🦦 Meerkat, 🦊 Fox, 🦡 Badger, 🐍 Snake (no more rabbits!)
Round 7:  🦦 Meerkat, 🦊 Fox, 🦡 Badger, 🐍 Snake
Round 8:  🦦 Meerkat, 🦊 Fox, 🦡 Badger, 🐍 Snake
```

## 💰 Example Point Calculations

### Round 1, Wave 3 (Boss) - NO COMBO
```
Animals: 12 Rabbits, 14 Meerkats
Raw Points: (12 × 5) + (14 × 10) = 60 + 140 = 200 pts
Combo Multiplier: 1.0x (no combo yet)
Accuracy: 75% (miss 6-7 animals)
Expected Score: 200 × 1.0 × 0.75 = 150 pts
Target: 200 pts (need 100% accuracy!)
```

### Round 2, Wave 2 - BASIC COMBO
```
Animals: 12 Rabbits, 7 Meerkats, 2 Foxes, 4 Snakes
Raw Points: (12×5) + (7×10) + (2×15) + (4×-10) = 60+70+30-40 = 120 pts
Combo Multiplier: 1.6x (average with combo uptime)
Accuracy: 75%
Expected Score: 120 × 1.6 × 0.75 = 144 pts
Target: 150 pts (achievable with good combo!)
```

### Round 8, Wave 3 (Final Boss) - MASTER COMBO
```
Animals: ~30 Meerkats, ~20 Foxes, ~10 Badgers, ~8 Snakes
Raw Points: (30×10) + (20×15) + (10×25) + (8×-10) = 300+300+250-80 = 770 pts
Combo Multiplier: 3.0x (nearly constant combo)
Accuracy: 75%
Expected Score: 770 × 3.0 × 0.75 = 1,733 pts
Target: 3,418 pts

⚠️ WARNING: Impossible without power-ups!
Need: Time Slow, Hit Area, or Perfect Accuracy to succeed
```

## 🛠️ Balancing Assumptions

```
┌──────────────────────┬─────────┬──────────────────────────┐
│ Parameter            │ Value   │ Reasoning                │
├──────────────────────┼─────────┼──────────────────────────┤
│ Player Accuracy      │ 75%     │ Miss 1 in 4 animals      │
│ Combo Uptime         │ 60%     │ Maintain most of time    │
│ Difficulty Scaling   │ 1.5x    │ Exponential per round    │
│ Boss Multiplier      │ 2.0x    │ Double point threshold   │
│ Snake Frequency      │ 10-20%  │ Manageable penalty       │
│ Spawn Rate Cap       │ 10/sec  │ Technical/visual limit   │
└──────────────────────┴─────────┴──────────────────────────┘
```

## 🎮 Strategic Insights

### Why Combos Are Essential
```
Without Combo (1.0x):      Round 8 needs ~8,000 animals hit
With T2 Combo (2.5x):      Round 8 needs ~3,200 animals hit
With T4 Combo (3.5x):      Round 8 needs ~2,300 animals hit

Round 8 Boss duration: 19 seconds
Max spawn rate: 10/second
Max possible spawns: 190 animals

Conclusion: IMPOSSIBLE without combo mastery + power-ups!
```

### Power-Up Value Calculations

**Slow Time (-25% speed):**
```
Effective visible duration: 2.5s → 3.1s (+24%)
Accuracy improvement: 75% → 85% (+10%)
Point value: ~13% more points earned
```

**Hit Area (3x3):**
```
Hit radius: 1 hole → 9 holes
Miss reduction: 25% misses → 10% misses
Accuracy improvement: 75% → 90% (+15%)
Point value: ~20% more points earned
```

**Lucky Animal (+100% golden spawn):**
```
Golden value: 5x normal points
Spawn rate: 5% → 10%
Expected value: +25% more points
```

## 🔧 Tuning Guidelines

### If Game is Too Hard:
- Reduce `SCALING_FACTOR` from 1.5 to 1.4
- Increase `ACCURACY_ASSUMPTION` from 0.75 to 0.80
- Buff combo multipliers (2.0x → 2.2x, etc.)
- Increase animal visible duration (+0.2s each)
- Reduce snake spawn rate (20% → 15%)

### If Game is Too Easy:
- Increase `SCALING_FACTOR` from 1.5 to 1.6
- Decrease `ACCURACY_ASSUMPTION` from 0.75 to 0.70
- Nerf combo multipliers (2.0x → 1.8x, etc.)
- Decrease animal visible duration (-0.2s each)
- Increase snake spawn rate (20% → 25%)

### If Combos Feel Weak:
- Reduce hits to start combo (3 → 2, 2 → 1)
- Increase combo active hits (5 → 7, 8 → 10)
- Add "combo never expires" upgrade
- Increase multiplier values (+0.5x each tier)

### If Economy Feels Off:
- Adjust shop prices to 30-40% of next wave target
- Common upgrade = 0.3x wave target
- Rare upgrade = 0.6x wave target
- Epic upgrade = 1.0x wave target
- This forces meaningful choices between upgrades

## 📁 Files Created

1. **RoguelikeBalancer.cs** - Core balancing calculation script
2. **BalancingVisualizer.cs** - Visualization and export helper
3. **ROGUELIKE_BALANCING.md** - Detailed round-by-round breakdown
4. **BALANCING_SUMMARY.md** - This quick reference guide

## 🚀 Next Steps

1. Attach `RoguelikeBalancer.cs` to GameObject in Unity
2. Run "Calculate Wave Balancing" from context menu
3. Review console output and CSV export
4. Adjust balancing parameters in Inspector
5. Implement `WaveData` into your game's wave system
6. Playtest and iterate based on feel
7. Track metrics: completion rates, average combos, common fail points

---

**Pro Tip:** The balancing script is designed to be tweaked! All constants are exposed in the Inspector. Run calculations, test in-game, adjust values, and repeat until it feels right. The goal is for Round 8 to be **barely possible** with optimal play + upgrades, creating a real sense of achievement when conquered.

