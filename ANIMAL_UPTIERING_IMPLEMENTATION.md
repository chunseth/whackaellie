# Animal Up-Tiering Implementation

## Overview
Implemented a player-driven progression system where players can purchase upgrades to convert rabbit spawns to meerkats, rather than having meerkats spawn automatically in later waves.

## Changes Made

### 1. New Upgrade Type: `RabbitToMeerkat`
- **Location**: `UpgradeDatabase.cs`
- **Effect**: +15% conversion chance per tier (15% → 30% → 50% → 75% → 100%)
- **Tiers**: 5 tiers (can be configured in Unity)
- **How it works**: Each time the game tries to spawn an animal, it rolls against the conversion percentage. If successful AND meerkat cap hasn't been reached, it spawns a meerkat instead of a rabbit.

### 2. PlayerUpgrades - New Field
- **Location**: `RoguelikeWaveData.cs`
- **Field**: `rabbitToMeerkatConversion` (float, 0.0 - 1.0)
- **Default**: 0f (no conversion)
- **Updated in**: Clone() method to ensure proper state copying

### 3. Removed Hardcoded Meerkat Spawning
- **Location**: `RoguelikeManager.cs` - `GenerateWaveData()` method
- **Changed**: Round 2 (waves 6-8) no longer automatically adds meerkats to `allowedAnimals`
- **Removed**: Hardcoded `meerkatSpawnChance` values (was 0.3f, 0.45f, 0.65f)
- **Removed**: Individual per-wave spawn caps (maxRabbits, maxMeerkats)
- **Now Using**: Only total `maxActiveAnimals` cap - conversion percentage determines natural ratio

### 4. Updated Spawn Logic
- **Location**: `MoleController.cs` - `SelectRandomAnimalRoguelike()` method
- **New Logic**:
  1. Default to rabbit spawn
  2. Roll against `playerUpgrades.rabbitToMeerkatConversion`
  3. If roll succeeds AND `CanSpawnMeerkat()` returns true, spawn meerkat
  4. Falls back to rabbit/meerkat based on total cap availability

### 5. Simplified Spawn Cap System
- **Location**: `MoleSpawner.cs` - `CanSpawnRabbit()` and `CanSpawnMeerkat()` methods
- **Changed**: Both methods now check total regular animals (rabbits + meerkats) against `maxActiveAnimals`
- **Removed**: Individual per-type cap enforcement
- **Benefit**: 
  - Spawn rate upgrades naturally increase meerkat capacity
  - 100% conversion can fill screen with meerkats
  - No artificial limits - ratio is determined purely by conversion %

## Configuration in Unity Editor

### Creating the Upgrade in UpgradeDatabase Asset

1. Open the `UpgradeDatabase` asset (`Assets/Resources/UpgradeDatabase.asset`)
2. Add a new upgrade to the `Upgrades` array:

```
Display Name: "Rabbit to Meerkat"
Description: "15% of rabbits become meerkats"
Upgrade Type: RabbitToMeerkat
Cost: 3-4$ (recommended)
Max Tiers: 5
Is Points Upgrade: false

Spawn Weight Per Wave:
[0] Wave 1: 0     (not available yet)
[1] Wave 2: 0     (not available yet)
[2] Wave 3: 0     (not available yet)
[3] Wave 4: 0     (not available yet)
[4] Wave 5: 50    (becomes available)
[5] Wave 6: 100   (high priority in Round 2)
[6] Wave 7: 100   (high priority in Round 2)
[7] Wave 8: 100   (high priority in Round 2)
```

### Recommended Balancing

**Tier Progression:**
- Tier 1: 15% conversion ($3-4)
- Tier 2: 30% conversion ($3-4)
- Tier 3: 50% conversion ($3-4)
- Tier 4: 75% conversion ($3-4)
- Tier 5: 100% conversion ($3-4)

**When to Offer:**
- Start offering in Wave 5 (before Round 2 begins)
- High priority in Round 2 (waves 6-8)
- Consider making it cheaper early to encourage adoption

**Synergies:**
- Works well with "Meerkat Points" upgrades
- Complements "Spawn Rate" upgrades (more spawns = more meerkats)
- Combo multiplier upgrades benefit from higher-value targets

## Gameplay Impact

### Before (Hardcoded System)
- Wave 1-5: Only rabbits (5 pts each)
- Wave 6: 30% meerkats, 70% rabbits (automatic)
- Wave 7: 45% meerkats, 55% rabbits (automatic)
- Wave 8: 65% meerkats, 35% rabbits (automatic)
- **Problem**: No player agency, forced progression

### After (Upgrade-Driven System)
- All waves: Start with 100% rabbits
- Player choice: Buy upgrade to convert rabbits → meerkats
- Progressive tiers: 15% → 30% → 50% → 75% → 100%
- Strategic depth: Players decide when/how much to invest
- **Benefit**: Player-driven progression, multiple viable strategies

## How Spawn Rate + Conversion Synergize

**Example Progression:**

**Base Stats (No Upgrades):**
- Total animals: 7
- Conversion: 0%
- Result: ~7 rabbits (5pts each) = ~35pts max

**With Spawn Rate Upgrade (+2 animals):**
- Total animals: 9
- Conversion: 0%
- Result: ~9 rabbits = ~45pts max

**With 50% Conversion:**
- Total animals: 7
- Conversion: 50%
- Result: ~3-4 meerkats (10pts) + ~3-4 rabbits (5pts) = ~50-60pts max

**With Spawn Rate + 100% Conversion:**
- Total animals: 9
- Conversion: 100%
- Result: ~9 meerkats (10pts each) = ~90pts max
- **This is the optimal strategy for maximizing points!**

## Testing Checklist

- [ ] Verify upgrade appears in shop starting at Wave 5
- [ ] Confirm 15% conversion at Tier 1 (roughly 1-2 meerkats per wave with 7 total animals)
- [ ] Test that total cap scales with spawn rate upgrades
- [ ] Ensure 100% conversion at Tier 5 spawns only meerkats (up to maxActiveAnimals)
- [ ] Verify with maxActiveAnimals=11 and 100% conversion, all 11 can be meerkats
- [ ] Check that points are awarded correctly for meerkats
- [ ] Test that combos work with mixed rabbit/meerkat spawns
- [ ] Verify spawn rate upgrade increases meerkat capacity naturally

## Future Enhancements

### Possible Additional Upgrades:
1. **"Premium Targets"**: Meerkats worth more points (synergy with conversion)
2. **"Mixed Combo"**: Bonus points for hitting both rabbit and meerkat in same combo
3. **"Tier Skip"**: Small chance to convert meerkats to badgers/foxes (if they exist)
4. **"Conversion Efficiency"**: Lower cost per tier or higher % gains (20% per tier instead of 15%)

### Alternative Balancing:
- Make conversion cheaper (2$) but cap at 75% max
- Add diminishing returns (20% → 35% → 45% → 50%)
- Make it wave-specific (only works in Round 2)

