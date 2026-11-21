# Whack-a-Ellie — Playtesting & Balance Metrics

Guide for observing runs and collecting data to tune difficulty, economy, and feel. Use this alongside `RoguelikeBalancer` and `ROGUELIKE_BALANCING.md` — but **trust live playtest numbers** over spreadsheet assumptions.

---

## Game Summary

**Whack-a-Ellie** is a mobile whack-a-mole game with a **Balatro-style roguelike run** as the primary mode. Animals pop up from holes; tap good ones for points, avoid snakes, build combos, earn gold, and buy upgrades between waves.

### Board & Spawn (Live Config)

| Parameter | Value |
|-----------|-------|
| **Holes** | **25** (5×5 grid) |
| **Max on screen** | **7** concurrent animals |
| **Hole cooldown** | 0.8s before same hole respawns |
| **Pop animation** | 0.4s up, 0.4s down (hits allowed during slide-down) |
| **Spawn interval** | Random wait between spawns; rate configured as animals/sec via upgrades |

### Animals (`Assets/Resources/AnimalDatabase.asset`)

| Animal | Points | Visible Window | Role |
|--------|--------|----------------|------|
| Rabbit | +5 | 1.2s | Combo fuel, forgiving |
| Meerkat | +10 | 1.0s | Mid value |
| Fox | +15 | **0.7s** | Fast priority target |
| Badger | +25 | **0.4s** | High-risk skill shot |
| Snake | **−35** (database) / **−10** (`RoguelikeManager` code) | **2.5s** | Don't tap; escape = no penalty |

> **Known mismatch:** Snake penalty is −35 in `AnimalDatabase.asset` but hardcoded as −10 in `RoguelikeManager.GetAnimalPoints()`. Log actual score deltas during playtests until this is unified.

### Roguelike Loop (`BalatroScene`)

1. **5 rounds × 3 waves**
2. Each wave: reach a **point target** before the **timer** runs out (~10s base, extendable via upgrades)
3. **Combo:** N hits accumulate points → multiplier burst (default: 3 hits → 2×)
4. **Miss** (good animal escapes): breaks combo (or −1 count with Combo Miss Shield upgrade)
5. **Snake hit:** penalty + combo break | **Snake escape:** no penalty
6. **Between waves:** gold from combos → **shop upgrades**
7. **Fail:** score below target when timer hits 0

#### Wave Point Targets (Current)

| Round | Wave 1 | Wave 2 | Wave 3 (Boss) |
|-------|--------|--------|---------------|
| 1 | 40 | 100 | 200 |
| 2 | 350 | 550 | 750 |
| 3 | 1,000 | 1,300 | 1,650 |
| 4 | 2,050 | 2,500 | 3,000 |
| 5 | 3,600 | 4,300 | 5,100 |

### Other Modes

- **Level mode** — Fixed levels with score targets and animal pools per level
- **Survival mode** — Limited lives, count-up timer, escalating spawn rate; miss = lose life, snake hit = lose life

### Shop Upgrade Levers

Spawn rate, wave length, combo starter / multiplier / interest / miss shield, animal conversions (rabbit→meerkat→fox→badger), perfect hit bonus (≤0.4s after fully visible), crit chance, time per combo. Many upgrades have **tradeoffs** (e.g. faster combos = less gold per combo).

---

## Core Balance Question

Every wave asks:

> **Can the player earn `pointTarget` points in `waveDuration` seconds, given spawn supply, their build, and their skill?**

`RoguelikeBalancer` assumes **75% accuracy** and **~60% combo uptime**. Playtesting replaces those guesses with measured values.

### Key Derived Formulas

```
PPS_needed   = pointTarget / waveDuration
PPS_achieved = finalScore / waveDuration
Margin       = (finalScore - pointTarget) / pointTarget   // positive = passed with room
Spawn_rate   = totalSpawned / waveDuration
Hit_rate     = totalHits / waveDuration
Miss_rate    = totalMisses / waveDuration
Accuracy     = hits / (hits + misses)                     // exclude snakes from "misses"
```

**Pass margin heuristics (skilled player):**

| Margin | Interpretation |
|--------|----------------|
| 110–130% of target | Well tuned |
| &lt;80% of target | Too hard — tune targets, duration, or supply |
| &gt;150% of target | Too easy — raise targets or pressure |

---

## Metrics to Observe

### 1. Supply — What the Game Throws at the Player

| Metric | How to Measure | Why It Matters |
|--------|----------------|----------------|
| **Total spawned / duration** | Count every pop-up ÷ wave seconds | Actual spawn rate vs configured min/max |
| **Spawned by type** | Rabbit / Meerkat / Fox / Badger / Snake counts | Conversion mix and animal introduction |
| **Snake events** | Snake spawns ÷ duration | Hazard density |
| **Avg concurrent on screen** | Sample every 1s: how many visible? | Board utilization |
| **Time at max concurrent (7/7)** | Seconds at 7 visible ÷ duration | Primary saturation indicator |
| **Spawn gaps** | Longest stretch with 0 animals up | Dead air vs overwhelm |

**Questions to answer:**

- Is `total_spawned / duration` near `(minSpawnRate + maxSpawnRate) / 2`?
- If rarely at 7/7 → difficulty may be **target/duration**, not spawn rate
- If always at 7/7 → player is **supply-limited**; consider raising concurrent cap before spawn rate

---

### 2. Demand — What the Player Actually Does

| Metric | How to Measure | Why It Matters |
|--------|----------------|----------------|
| **Total hits / duration** | Good taps only ÷ seconds | Effective human tap rate |
| **Total missed / duration** | Escapes (non-snake) ÷ seconds | Miss pressure over time |
| **Miss rate (overall)** | Missed ÷ (hits + missed) | Replaces `ACCURACY_ASSUMPTION` |
| **Miss rate when N on screen** | Missed ÷ spawned, bucketed N=1–7 | Clutter vs accuracy |
| **Miss rate by animal type** | Per rabbit / meerkat / fox / badger | Validates visibility windows |
| **Hit rate by animal type** | Hits ÷ spawned per type | Are high-value targets hittable? |

**Accuracy by concurrent count (fill per wave):**

```
N=1 on screen:  __ hits, __ misses  →  __%
N=2 on screen:  __ hits, __ misses  →  __%
N=3 on screen:  __ hits, __ misses  →  __%
N=4 on screen:  __ hits, __ misses  →  __%
N=5 on screen:  __ hits, __ misses  →  __%
N=6 on screen:  __ hits, __ misses  →  __%
N=7 on screen:  __ hits, __ misses  →  __%
```

**Questions to answer:**

- Does accuracy drop sharply at 5–7 concurrent? → 25-hole search cost
- Is badger hit rate &lt;50%? → 0.4s window may be too tight on mobile
- Is rabbit miss rate high despite 1.2s? → grid search, not reaction time

---

### 3. Snake Interactions (Separate from Misses)

| Metric | How to Measure | Why It Matters |
|--------|----------------|----------------|
| **Snakes spawned** | Count | Exposure (2.5s visibility each) |
| **Snakes hit** | Count | False-positive / panic taps |
| **Snakes escaped** | Count | Correct avoidance (good outcome) |
| **Snake hit rate** | Hits ÷ spawned | Should stay low |
| **Points lost to snakes** | Sum of penalties | Real mistake cost |
| **Combos broken by snake** | Count | Impact beyond raw points |

> Snake escapes are **successes** — do not count them as misses.

---

### 4. Combo & Scoring Economy

| Metric | How to Measure | Why It Matters |
|--------|----------------|----------------|
| **Combos completed / wave** | Count | Burst DPS + gold income |
| **Combo breaks** | Misses + snake hits that reset combo | Frustration source |
| **Avg combo length** | Hits before each trigger | Combo pacing |
| **Points from base hits** | Sum | Score floor without multipliers |
| **Points from combo bonus** | Sum | How combo-dependent the wave is |
| **Perfect hits** | Hits within 0.4s of fully visible | Skill ceiling + upgrade value |
| **Crits** | Count | RNG contribution |
| **Points per second (PPS)** | Final score ÷ duration | Single compare-across-waves number |
| **Score vs target** | Actual ÷ required | Pass/fail margin |

**Combo dependency check:**

```
combo_bonus_pct = comboBonusPoints / finalScore
```

If combo bonus is **&gt;60%** of total score, waves are combo-gated more than tap-gated.

---

### 5. Timing & Reaction (Sample 20–30 Hits per Session)

| Metric | How to Measure | Why It Matters |
|--------|----------------|----------------|
| **Reaction time** | Tap timestamp − fully-visible timestamp | Human baseline on 25-hole grid |
| **Miss reason** | Too slow / didn't see / wrong hole | Actionable design fix |
| **Time left on pass** | Seconds remaining when target hit | Duration tuning |
| **Score at 0:00 on fail** | Final vs target | How far off the player was |

---

### 6. Shop & Build (Per Run)

| Metric | How to Measure | Why It Matters |
|--------|----------------|----------------|
| **Gold earned vs spent per wave** | Track $ | Economy pacing |
| **Upgrades purchased** | List per wave | Which builds succeed |
| **PPS before vs after upgrade** | Same player, adjacent waves | Upgrade ROI |
| **Win rate by build archetype** | Combo / spawn / conversion / crit | Strategic diversity |

---

### 7. Run-Level Outcomes

| Metric | Starting Target | Notes |
|--------|-----------------|-------|
| **Wave pass rate — Round 1** | ~95% | Tutorial territory |
| **Wave pass rate — Round 2–3** | ~75–85% | Combo + snakes online |
| **Wave pass rate — Round 4–5** | ~35–50% on boss waves | Build-dependent gate |
| **Furthest round reached** | Distribution across testers | Progression curve |
| **Fail reason** | Time / score / quit | What to tune |
| **Retry rate after fail** | % who start another run | Engagement |

Adjust targets after collecting real data from ~20+ runs across skill bands.

---

## Per-Wave Playtest Sheet

Copy one block per wave observed.

```
Run ID: ___          Date: ___
Player: ___          Skill: casual / mid / expert
Device: ___          Scene/mode: roguelike / survival / level

Round __  Wave __  |  Target: ___  Duration: ___s
Upgrades entering wave: ___
Spawn config: min ___/s  max ___/s  max concurrent ___

── SUPPLY ──────────────────────────────────────
Spawned total: ___  (___ / sec)
By type:  R ___  M ___  F ___  B ___  S ___
Avg concurrent: ___     Sec at 7/7: ___ / ___s  (___%)

── PERFORMANCE ─────────────────────────────────
Hits:     ___  (___ / sec)
Misses:   ___  (___ / sec)     Accuracy: ___%
Misses by concurrent count (hits/misses):
  N=1: ___/___   N=2: ___/___   N=3: ___/___   N=4: ___/___
  N=5: ___/___   N=6: ___/___   N=7: ___/___
Hits by type:  R ___  M ___  F ___  B ___
Misses by type: R ___  M ___  F ___  B ___

── SNAKES ───────────────────────────────────────
Spawned: ___   Hit: ___   Escaped: ___
Penalty points: ___   Combos broken: ___

── COMBO & SCORE ────────────────────────────────
Combos completed: ___   Combo breaks: ___
Base points: ___   Combo bonus: ___   (bonus = ___% of total)
Perfect hits: ___   Crits: ___
Final score: ___ / ___   PPS: ___   Margin: ___%
Time remaining: ___s   Result: PASS / FAIL

── NOTES ────────────────────────────────────────
Felt fair?  Y / N
Main struggle: ___
Suggested tuning: ___
```

---

## Priority Observations (Do These First)

1. **PPS achieved vs PPS needed** — every wave, every skill band
2. **Miss rate when 7/7 on screen** — 25-hole grid + 7 concurrent cap
3. **Badger & fox hit rates** — validate 0.4s and 0.7s windows on real devices
4. **Snake hit rate + combo breaks from snakes** — 2.5s visibility is long exposure
5. **Time at max concurrent** — tune spawn rate vs concurrent cap vs point targets
6. **Combo bonus as % of total score** — combo-gated vs tap-gated waves
7. **Round 2→3 and Round 4→5 target jumps** — largest spikes in current table

---

## Skill Band Expectations (After ~20 Playtests)

| Skill | Expected Progress |
|-------|-------------------|
| **Casual** | Clears Round 1–2; struggles Round 3+ |
| **Mid** | Clears Round 3–4; Round 5 boss is a real gate |
| **Expert** | Beats Round 5 with a strong build; wants Round 6+ content |

Failures should feel **attributable**: "I broke combo," "I tapped snakes," "Badgers were too fast" — not "the game starved me of spawns."

Upgrades should move PPS by roughly **10–25%** per meaningful purchase; shop choices should feel strategic, not obvious.

---

## Config Gaps to Validate During Playtests

| Issue | What to Watch |
|-------|---------------|
| Snake −10 (code) vs −35 (database) | Floating text and actual score change |
| `RoguelikeBalancer` / docs: 8 rounds, old animal durations | Live game: 5 rounds, inverted durations (badger fast, snake slow) |
| Fox / Badger in roguelike | Only appear via shop conversions — track when they enter the pool |
| `ACCURACY_ASSUMPTION = 0.75` in balancer | Replace with measured accuracy from this sheet |

---

## Telemetry (Implemented)

Roguelike mode auto-creates `PlaytestTelemetry` and writes CSVs to:

`Application.persistentDataPath/playtest_logs/{sessionId}/`

| File | Contents |
|------|----------|
| `spawn_events.csv` | Each spawn + blocked attempts (waveSize, active before/after, cap) |
| `animal_outcomes.csv` | Hit / Expired / RoundEnded per animal |
| `board_snapshots.csv` | Board state every 0.5s (unhit, hit-animating, total visible) |
| `upgrade_snapshots.csv` | Build state at each round start |
| `round_summary.csv` | One row per completed or failed round |

**Accuracy** in `round_summary.csv` = `totalHit / (totalHit + totalExpired)` — `totalTimerEnded` is excluded.

Toggle logging via `PlaytestTelemetry.loggingEnabled` on the component (or disable the GameObject).

---

## Ideal Telemetry Schema (Future)

When instrumenting the game, log this struct at end of each wave:

```csharp
WaveStats {
    // Context
    int round, wave;
    int pointTarget;
    float waveDuration;
    string upgradeSnapshot;   // serialized build

    // Supply
    int totalSpawned;
    int spawnedRabbit, spawnedMeerkat, spawnedFox, spawnedBadger, spawnedSnake;
    float avgConcurrent;
    float secondsAtMaxConcurrent;

    // Performance
    int totalHits, totalMisses;
    int hitsAtConcurrent1..7, missesAtConcurrent1..7;
    int hitsByType[5], missesByType[5];

    // Snakes
    int snakeHits, snakeEscapes, snakePenaltyPoints, snakeComboBreaks;

    // Scoring
    int combosCompleted, comboBreaks;
    int basePoints, comboBonusPoints;
    int perfectHits, crits;
    int finalScore;
    float timeRemaining;
    bool passed;
}
```

Export to CSV under `Assets/playtest_logs/` for aggregation across sessions.

---

## Related Docs

- `ROGUELIKE_BALANCING.md` — Theoretical wave breakdown (may be stale vs live config)
- `HOW_TO_USE_BALANCER.md` — Unity editor balancer tool
- `BALANCING_SUMMARY.md` — Quick reference for point thresholds and combo tiers
- `ROGUELIKE_SETUP.md` — Mode setup and mechanics overview
