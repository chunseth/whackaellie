# Roguelike Mode Setup Guide

## 📁 Files Created

### Core Scripts (Assets/Scripts/Roguelike/)
- **RoguelikeManager.cs** - Main game loop manager
- **RoguelikeWaveData.cs** - Data structures for waves and upgrades
- **ComboSystem.cs** - Combo tracking and multiplier system
- **ShopManager.cs** - Shop UI and upgrade purchases

### Modified Files
- **MoleController.cs** - Now supports roguelike mode notifications

---

## 🎮 How It Works

### Game Flow
```
Start Game
    ↓
Round 1, Wave 1 (50pts in 8s)
    ↓
Shop (buy upgrades with earned points)
    ↓
Round 1, Wave 2 (100pts in 8s)
    ↓
Victory / Game Over
```

### Combo System
```
Hit 1: No combo (×1.0)
Hit 2: No combo (×1.0)
Hit 3: COMBO STARTS! (×1.0)  ← Default: 3 hits to start
Hit 4: Combo active (×1.0)   ← Can upgrade multiplier
Hit 5: Combo active (×1.0)
Hit 6: Combo active (×1.0)
Hit 7: Combo active (×1.0)
Hit 8: Combo ends             ← Can upgrade duration
Hit 9: No combo but streak continues
Hit 10: COMBO STARTS again!
```

**Miss = Reset combo AND hit streak**

---

## 🔧 Unity Setup (Step-by-Step)

### 1. Create Roguelike Scene

#### Option A: Duplicate Existing Scene
```
1. In Project window, find GameScene
2. Right-click → Duplicate
3. Rename to "RoguelikeScene"
```

#### Option B: Create New Scene
```
1. File → New Scene
2. Save as "RoguelikeScene"
3. Copy HoleGrid GameObject from GameScene
4. Copy MoleSpawner from GameScene
```

---

### 2. Setup RoguelikeManager

```
1. In RoguelikeScene, create empty GameObject
2. Name it "RoguelikeManager"
3. Add component: RoguelikeManager
4. Add component: ComboSystem
```

#### Configure RoguelikeManager Inspector:
```
References:
  - Mole Spawner: [Drag MoleSpawner from scene]
  - Combo System: [Drag ComboSystem from same GameObject]

UI References:
  - Score Text: [Create UI Text and drag]
  - Target Text: [Create UI Text and drag]
  - Timer Text: [Create UI Text and drag]
  - Round Wave Text: [Create UI Text and drag]
  - Combo Text: [Create UI Text and drag]
  - Start Panel: [Create Panel and drag]
  - Shop Panel: [Create Panel and drag]
  - Game Over Panel: [Create Panel and drag]
  - Game Over Message Text: [Create Text and drag]
```

---

### 3. Create UI Panels

#### A. Start Panel
```
Hierarchy:
Canvas
  └─ StartPanel
      ├─ Title (TextMeshPro)
      └─ StartButton (Button)
          └─ Text (TextMeshPro) "Start Game"

Setup:
1. Create UI → Panel, name it "StartPanel"
2. Add TextMeshPro - Text: "Roguelike Mode"
3. Add Button: "Start Game"
4. Button OnClick() → RoguelikeManager.StartGame()
```

#### B. Game UI (HUD)
```
Canvas
  └─ GameHUD
      ├─ ScoreText (TMP) "Score: 0 / 50"
      ├─ TargetText (TMP) "Target: 50pts"
      ├─ TimerText (TMP) "Time: 8s"
      ├─ RoundWaveText (TMP) "Round 1 - Wave 1"
      └─ ComboText (TMP) "No Combo"

Setup:
1. Create empty GameObject "GameHUD"
2. Add 5 TextMeshPro objects as children
3. Position them in corners/top of screen
4. Drag each to RoguelikeManager's UI References
```

#### C. Shop Panel
```
Canvas
  └─ ShopPanel
      ├─ Header (TMP) "Shop"
      ├─ CurrencyText (TMP) "Points: 0"
      ├─ UpgradeButtons
      │   ├─ RabbitPointsButton
      │   ├─ RoundLengthButton
      │   ├─ SpawnRateButton
      │   ├─ ComboStarterButton
      │   ├─ ComboMultiplierButton
      │   └─ ComboActiveButton
      └─ ContinueButton (Button) "Continue"

Setup:
1. Create Panel, name it "ShopPanel"
2. Add header text
3. Add currency display
4. Create 6 upgrade buttons (see button setup below)
5. Add continue button
6. Set active = false by default
```

##### Upgrade Button Setup (each button):
```
UpgradeButton (Button)
  ├─ Background (Image)
  ├─ NameText (TMP) "Rabbit Points +1"
  └─ CostText (TMP) "20pts"

For each button, create a ShopUpgradeButton entry in ShopManager:
  - Button: [Drag Button component]
  - Name Text: [Drag NameText]
  - Cost Text: [Drag CostText]
  - Background Image: [Drag Background]
  - Affordable Color: Green
  - Unaffordable Color: Gray
```

#### D. Game Over Panel
```
Canvas
  └─ GameOverPanel
      ├─ MessageText (TMP) "Game Over"
      └─ RestartButton (Button) "Restart"

Setup:
1. Create Panel, name it "GameOverPanel"
2. Add message text
3. Add restart button → RoguelikeManager.StartGame()
4. Set active = false by default
```

---

### 4. Setup ShopManager

```
1. In RoguelikeManager GameObject, add component: ShopManager
2. Configure Inspector:

References:
  - Roguelike Manager: [Auto-finds or drag]

UI Elements:
  - Currency Text: [Drag from Shop Panel]
  - Continue Button: [Drag continue button]

Upgrade Buttons:
  - Rabbit Points Button: [Configure ShopUpgradeButton]
  - Round Length Button: [Configure ShopUpgradeButton]
  - Spawn Rate Button: [Configure ShopUpgradeButton]
  - Combo Starter Button: [Configure ShopUpgradeButton]
  - Combo Multiplier Button: [Configure ShopUpgradeButton]
  - Combo Active Button: [Configure ShopUpgradeButton]

Upgrade Costs:
  - Rabbit Points Cost: 20
  - Round Length Cost: 30
  - Spawn Rate Cost: 25
  - Combo Starter Cost: 40
  - Combo Multiplier Cost: 50
  - Combo Active Cost: 45
```

---

### 5. Connect MoleSpawner

The existing `MoleSpawner` and `MoleController` will work automatically!

**Important:** Make sure:
1. MoleSpawner is in the scene
2. MoleControllers are set up on each hole
3. AnimalDatabase has Rabbit data

The scripts will automatically detect RoguelikeManager and route clicks appropriately.

---

## 🎮 How to Play

### Wave Start
1. Click "Start Game" button
2. Round 1 Wave 1 begins
3. Target: Hit rabbits to reach 50pts in 8 seconds

### During Wave
- Click rabbits as they pop up
- Each rabbit worth 5pts (can be upgraded)
- Build combos for multipliers
- Watch the timer!

### Combo Building
- Hit 3 rabbits in a row → Combo starts
- Next 5 hits get multiplier (default 1.0x)
- Miss a rabbit → Combo resets!
- Combo UI shows progress

### Shop Phase
1. Wave ends (success or failure)
2. If successful, shop opens
3. Spend earned points on upgrades:
   - **Rabbit Points +1** (20pts) - Rabbits worth more
   - **Round Length +2s** (30pts) - More time per wave
   - **Spawn Rate +0.5/s** (25pts) - More rabbits spawn
   - **Combo Starter -1** (40pts) - Combo starts sooner
   - **Combo Mult +0.5x** (50pts) - Bigger combo bonus
   - **Combo Length +2** (45pts) - Combo lasts longer
4. Click "Continue" to start next wave

### Wave 2
- Same mechanics
- But with your upgrades!
- Target: 100pts

---

## 🎯 Upgrade Strategies

### Early Game (Wave 1 → Wave 2)
**Budget: ~50-70pts**

**Strategy A: Combo Focus**
- Buy Combo Starter -1 (40pts)
- Save rest for next shop
- Faster combos = more consistent points

**Strategy B: Raw Power**
- Buy Rabbit Points +1 (20pts)
- Buy Round Length +2s (30pts)
- More points per rabbit + more time

**Strategy C: Volume**
- Buy Spawn Rate +0.5/s (25pts)
- Buy Rabbit Points +1 (20pts)
- More rabbits worth more points

### Optimal Path (Theory)
```
Wave 1 (earn ~60pts):
  - Rabbit Points +1 (20pts)
  - Spawn Rate +0.5/s (25pts)
  - Save 15pts

Wave 2 (earn ~120pts with upgrades):
  - Combo Starter -1 (40pts)
  - Combo Multiplier +0.5x (50pts)
  - Combo Active +2 (45pts)
  
Result: 6pt rabbits, faster combos with 1.5x multiplier!
```

---

## 🐛 Troubleshooting

### "Nothing happens when I click Start"
- Check RoguelikeManager has MoleSpawner reference
- Check Start Button has OnClick → RoguelikeManager.StartGame()
- Check console for errors

### "No rabbits spawn"
- Check MoleSpawner is in scene
- Check MoleControllers are set up
- Check AnimalDatabase has Rabbit data
- Check RoguelikeManager.StartWave() is being called

### "Combo never starts"
- Check ComboSystem is attached
- Check ComboSystem.Initialize() is called
- Check MoleController is calling RoguelikeManager.OnAnimalHit()

### "Shop doesn't show upgrades"
- Check ShopManager is attached
- Check all ShopUpgradeButton fields are filled
- Check ShopManager.RefreshShop() is being called

### "Points don't decrease when buying"
- Check ShopManager.TryPurchase() logic
- Check currencyText is updating
- Check console for "Not enough points" warnings

---

## 📊 Default Balance

### Wave 1
- Target: 50 points
- Duration: 8 seconds
- Rabbits: 5pts each
- Spawn Rate: 1-3 per second
- Expected Spawns: ~16 rabbits
- Required Hits: 10 rabbits (63% accuracy)

### Wave 2
- Target: 100 points
- Duration: 8 seconds (upgradeable)
- Rabbits: 5pts each (upgradeable)
- Spawn Rate: 1-3 per second (upgradeable)
- Expected Spawns: ~16 rabbits
- Required Hits: 20 rabbits (125% - NEED UPGRADES!)

**Wave 2 is intentionally hard without upgrades!**

---

## 🔮 Next Steps

### Immediate Additions
1. Round 2+ with more waves
2. More animal types (meerkats, foxes, etc.)
3. Boss waves with special challenges
4. Persistent meta-progression

### Future Features
1. Different starting classes
2. More upgrade types
3. Power-ups during waves
4. Leaderboards
5. Daily challenges

---

## 💻 Code Integration

### Adding New Upgrades

```csharp
// In ShopManager.cs

[Header("New Upgrade")]
[SerializeField] private ShopUpgradeButton myNewButton;
[SerializeField] private int myNewCost = 50;

private void Start()
{
    // Setup button
    if (myNewButton != null)
    {
        myNewButton.SetupButton("My Upgrade Name", myNewCost, () => BuyMyUpgrade());
    }
}

private void BuyMyUpgrade()
{
    if (TryPurchase(myNewCost))
    {
        roguelikeManager.ApplyUpgrade(upgrades => 
        {
            upgrades.myNewStat += 10;
        });
        RefreshShop();
    }
}
```

### Adding New Waves

```csharp
// In RoguelikeManager.cs, GenerateWaveData()

if (round == 1 && wave == 3)
{
    data.pointTarget = 150;
    data.duration = playerUpgrades.waveDuration;
    data.minSpawnRate = playerUpgrades.minSpawnRate;
    data.maxSpawnRate = playerUpgrades.maxSpawnRate;
}
```

### Adding New Animal Types

```csharp
// In RoguelikeManager.cs, GetAnimalPoints()

case AnimalType.Meerkat:
    return playerUpgrades.basePer Meerkat;
```

Then add to PlayerUpgrades:
```csharp
public int basePointsPerMeerkat = 10;
```

---

## ✅ Checklist

Before testing:
- [ ] RoguelikeScene created
- [ ] RoguelikeManager GameObject created
- [ ] ComboSystem attached
- [ ] ShopManager attached
- [ ] All UI elements created and linked
- [ ] MoleSpawner in scene
- [ ] MoleControllers working
- [ ] Start button linked to StartGame()
- [ ] Shop continue button linked to CloseShop()
- [ ] All panels set to correct active state

Ready to play!

---

**Have fun building your roguelike! The system is designed to be expanded - add more rounds, animals, and upgrades as you go!** 🎮🐰

