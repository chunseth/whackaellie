# Extended Rounds System - 5 Rounds × 5 Waves

## Overview
Extended the roguelike mode from 8 waves (Round 1: 5 waves, Round 2: 3 waves) to **25 waves** (5 rounds × 5 waves each).

## Round Structure

### Round 1: Tutorial (Waves 1-5)
**Point Targets:** 40 → 90 → 180 → 310 → 480  
**Animals:** Rabbits only (Snakes appear in Wave 5)  
**Focus:** Learn basic mechanics, build spawn rate and combo upgrades  
**Total Points Needed:** 1,100

### Round 2: Conversion Unlocked (Waves 6-10)
**Point Targets:** 650 → 900 → 1,200 → 1,550 → 1,950  
**Animals:** Rabbits (Meerkats via upgrade)  
**Focus:** Unlock and upgrade Rabbit→Meerkat conversion  
**Total Points Needed:** 6,250

### Round 3: Scaling Difficulty (Waves 11-15)
**Point Targets:** 2,400 → 2,900 → 3,450 → 4,050 → 4,700  
**Animals:** Rabbits (Meerkats via upgrade)  
**Focus:** Maximize conversion percentage and spawn rate  
**Total Points Needed:** 17,500

### Round 4: Expert Challenge (Waves 16-20)
**Point Targets:** 5,400 → 6,150 → 6,950 → 7,800 → 8,700  
**Animals:** Rabbits (Meerkats via upgrade)  
**Focus:** Optimize build, max out key upgrades  
**Total Points Needed:** 35,000

### Round 5: Final Boss (Waves 21-25)
**Point Targets:** 9,650 → 10,650 → 11,700 → 12,800 → 14,000  
**Animals:** Rabbits (Meerkats via upgrade)  
**Focus:** Fully upgraded build required to succeed  
**Total Points Needed:** 58,800

## Total Game Length
- **Total Waves:** 25
- **Total Points Needed:** ~118,650 points
- **Estimated Time:** 3-4 minutes per wave = 75-100 minutes (1.5-2 hours)

## Point Progression Scaling

| Round | Starting Target | Ending Target | Growth Rate |
|-------|----------------|---------------|-------------|
| 1     | 40             | 480           | ~120%/wave  |
| 2     | 650            | 1,950         | ~35%/wave   |
| 3     | 2,400          | 4,700         | ~18%/wave   |
| 4     | 5,400          | 8,700         | ~13%/wave   |
| 5     | 9,650          | 14,000        | ~10%/wave   |

**Fallback Scaling** (for extra waves beyond configured):
- Round 1: +250 pts/wave
- Round 2: +500 pts/wave
- Round 3: +750 pts/wave
- Round 4: +1,000 pts/wave
- Round 5: +1,500 pts/wave

## Victory Condition
- Complete all 5 rounds (Wave 25)
- Victory message: "VICTORY! All 5 Rounds Complete!"

## Recommended Upgrade Path

### Round 1 (Waves 1-5)
- **Priority 1:** Spawn Rate (1-2 tiers) → More animals on screen
- **Priority 2:** Round Length (1-2 tiers) → More time to hit targets
- **Priority 3:** Combo system → Start earning gold efficiently

**Gold Target:** $20-30

### Round 2 (Waves 6-10)
- **Priority 1:** Rabbit→Meerkat Conversion (2-3 tiers) → 30-50% conversion
- **Priority 2:** Meerkat Points (+1 or +3) → Maximize value
- **Priority 3:** More Spawn Rate → Increase meerkat count

**Gold Target:** $40-60

### Round 3 (Waves 11-15)
- **Priority 1:** Max out Rabbit→Meerkat (100% conversion)
- **Priority 2:** Spawn Rate (total 13-15 animals)
- **Priority 3:** Combo Multiplier → Higher point bonuses

**Gold Target:** $60-80

### Round 4 (Waves 16-20)
- **Priority 1:** Maximize Spawn Rate (17-19+ animals)
- **Priority 2:** Max Meerkat Points
- **Priority 3:** Max Combo Multiplier

**Gold Target:** $80-100

### Round 5 (Waves 21-25)
- **Strategy:** Maintain optimal build
- **Goal:** Perfect combos with 100% meerkats
- **Expected:** 17-21 meerkats @ 13-15pts each with 3-4x combo multipliers

**Victory Requirements:**
- ~15-20 animals on screen
- 100% meerkat conversion
- High combo multiplier (3x+)
- Meerkat points maxed (13-15+ per hit)

## Balance Notes

### Difficulty Curve
The game is designed to require incremental upgrades:
- Round 1-2: Build foundation
- Round 3: Transition to meerkat-focused strategy
- Round 4-5: Requires near-perfect build optimization

### Gold Economy
With goldPerCombo = $2:
- Average 10-15 combos per wave
- $20-30 gold per wave
- Total gold across 25 waves: ~$500-750
- Enough for 125-188 upgrade purchases

### Rebalancing Levers
If too hard/easy, adjust:
1. **Point targets** in wave configuration arrays
2. **Gold per combo** ($1-3 recommended range)
3. **Upgrade costs** in UpgradeDatabase
4. **Wave duration** (default 8s, upgradable)

## Code Changes Summary

### Files Modified
1. **RoguelikeManager.cs**
   - Added round3WaveTargets, round4WaveTargets, round5WaveTargets arrays
   - Updated GenerateWaveData() to handle all 5 rounds
   - Updated ContinueToShop() for proper round advancement
   - Updated ShowVictory() message

### Key Changes
- Round advancement now checks `currentWave > 5` instead of different values per round
- Victory triggers when `currentRound > 5`
- All rounds use consistent 5-wave structure
- Point scaling increases with each round

## Testing Checklist

- [ ] Verify Round 1 Wave 1-5 point targets correct
- [ ] Verify Round 2 Wave 1-5 point targets correct
- [ ] Verify Round 3-5 wave targets correct
- [ ] Test round advancement at wave 5 → advances to next round
- [ ] Test victory condition after Round 5 Wave 5
- [ ] Verify shop shows correct wave number (currentWave - 1)
- [ ] Test fallback point scaling if waves exceed 5 per round
- [ ] Verify all animal spawning works across all rounds
- [ ] Check that upgrades persist across all 25 waves
- [ ] Test game over screen shows correct round/wave info

