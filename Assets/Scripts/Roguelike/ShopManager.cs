using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the shop UI and upgrade purchases
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoguelikeManager roguelikeManager;
    [SerializeField] private UpgradeDatabase upgradeDatabase;
    [SerializeField] private bool loadDatabaseFromResources = true;
    [SerializeField] private string databaseResourcePath = "UpgradeDatabase";
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private GameObject backgroundPanel;
    
    [Header("Upgrade Buttons")]
    [SerializeField] private ShopUpgradeButton upgradeButton1;
    [SerializeField] private ShopUpgradeButton upgradeButton2;
    [SerializeField] private ShopUpgradeButton upgradeButton3;
    [SerializeField] private ShopUpgradeButton upgradeButton4;
    
    [Header("Tier Visuals")]
    [SerializeField] private Sprite tierEmptySprite;
    [SerializeField] private Sprite tierBoughtSprite;
    
    [Header("Audio")]
    [SerializeField] private AudioClip cashRegisterSound;
    [SerializeField] private AudioClip buttonPressSound;
    private AudioSource audioSource;
    
    [Header("Description Panels")]
    [SerializeField] private GameObject upgradeDescription1;
    [SerializeField] private GameObject upgradeDescription2;
    [SerializeField] private GameObject upgradeDescription3;
    [SerializeField] private GameObject upgradeDescription4;
    
    [Header("Buy Buttons")]
    [SerializeField] private Button buyButton1;
    [SerializeField] private Button buyButton2;
    [SerializeField] private Button buyButton3;
    [SerializeField] private Button buyButton4;
    
    // Arrays to store references for easier access
    private GameObject[] descriptionPanels;
    private Button[] buyButtons;
    
    [Header("Randomization")]
    [SerializeField] private int numberOfUpgradesToShow = 4;
    [SerializeField] private int rerollCost = 2;
    [SerializeField] private bool useRandomSeed = false;
    [SerializeField] private int randomSeed = 0;
    
    // Currently displayed upgrade types (randomized each shop visit)
    private UpgradeType[] currentUpgradeTypes;
    
    private PlayerUpgrades currentUpgrades;
    private System.Collections.Generic.HashSet<UpgradeType> purchasedLimitedUpgradesThisWave = new System.Collections.Generic.HashSet<UpgradeType>();  // Track which limited upgrades were purchased this wave
    private System.Random shopRandom;
    private int lastRandomizedWave = -1;  // Track which wave we last randomized for
    private int lastShopOpenedWave = -1;  // Track actual currentWave when shop last opened (for clearing limited purchases)
    
    private void Start()
    {
        if (roguelikeManager == null)
        {
            roguelikeManager = FindObjectOfType<RoguelikeManager>();
        }
        
        // Initialize AudioSource for sound effects
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
        
        // Load upgrade database from Resources if needed (for builds)
        if (loadDatabaseFromResources)
        {
            upgradeDatabase = Resources.Load<UpgradeDatabase>(databaseResourcePath);
            if (upgradeDatabase == null)
            {
                Debug.LogError($"[Shop] Failed to load UpgradeDatabase from Resources/{databaseResourcePath}! Make sure the asset is in a Resources folder.");
            }
            else
            {
                Debug.Log($"[Shop] Successfully loaded UpgradeDatabase from Resources.");
            }
        }
        else if (upgradeDatabase == null)
        {
            Debug.LogWarning("[Shop] UpgradeDatabase is not assigned and loadDatabaseFromResources is false!");
        }
        
        // Initialize arrays
        descriptionPanels = new GameObject[] { upgradeDescription1, upgradeDescription2, upgradeDescription3, upgradeDescription4 };
        buyButtons = new Button[] { buyButton1, buyButton2, buyButton3, buyButton4 };
        
        // Setup background panel to close descriptions
        if (backgroundPanel != null)
        {
            Button bgButton = backgroundPanel.GetComponent<Button>();
            if (bgButton != null)
            {
                bgButton.onClick.AddListener(() => CloseAllDescriptions());
            }
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueToNextWave);
        }
        
        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(RerollShop);
        }
        
        // Ensure all descriptions and background start inactive
        CloseAllDescriptions();
    }
    
    private void OnEnable()
    {
        // Close all descriptions when shop opens
        CloseAllDescriptions();
        
        // Clear limited purchases tracker if this is a new wave (use absolute wave number)
        if (roguelikeManager != null)
        {
            int absoluteWave = roguelikeManager.GetAbsoluteWaveNumber();
            if (absoluteWave != lastShopOpenedWave)
            {
                Debug.Log($"[Shop] New shop session detected (absolute wave {lastShopOpenedWave} → {absoluteWave}) - Clearing limited purchase tracker");
                purchasedLimitedUpgradesThisWave.Clear();
                lastShopOpenedWave = absoluteWave;
            }
        }
        
        // Randomize which upgrades appear in the shop
        RandomizeShopUpgrades();
        
        RefreshShop();
    }
    
    /// <summary>
    /// Randomize which upgrades appear in the shop
    /// </summary>
    private void RandomizeShopUpgrades()
    {
        if (upgradeDatabase == null)
        {
            Debug.LogWarning("[Shop] UpgradeDatabase is not assigned!");
            return;
        }
        
        if (roguelikeManager == null)
        {
            Debug.LogWarning("[Shop] RoguelikeManager is not assigned!");
            return;
        }
        
        // Get absolute wave number (across all rounds) from the roguelike manager
        int absoluteWave = roguelikeManager.GetAbsoluteWaveNumber();
        
        // Shop shows upgrades for the wave that was just COMPLETED, not the upcoming wave
        // So we use absoluteWave - 1 for shop upgrades (wave just finished)
        int shopWave = Mathf.Max(1, absoluteWave - 1);
        
        // Check if we're on a new wave - if so, reinitialize the random generator
        if (shopWave != lastRandomizedWave)
        {
            Debug.Log($"[Shop] Shop for completed wave {shopWave} (absolute wave: {absoluteWave}, previous shop wave: {lastRandomizedWave})");
            
            // Reset per-wave purchase trackers for the new wave
            purchasedLimitedUpgradesThisWave.Clear();
            
            // Initialize/reinitialize random generator for the new wave
            if (useRandomSeed)
            {
                // Use wave-specific seed for reproducibility
                shopRandom = new System.Random(randomSeed + shopWave);
                Debug.Log($"[Shop] Wave {shopWave} shop - Using seed: {randomSeed + shopWave}");
            }
            else
            {
                // Use time-based seed for true randomness
                shopRandom = new System.Random((int)(System.DateTime.Now.Ticks % int.MaxValue) + shopWave);
                Debug.Log($"[Shop] Wave {shopWave} shop - Using time-based random seed");
            }
            
            lastRandomizedWave = shopWave;
        }
        
        // Get random upgrades from the database based on the completed wave
        // Pass currentUpgrades to filter out maxed upgrades
        PlayerUpgrades upgrades = roguelikeManager.GetPlayerUpgrades();
        currentUpgradeTypes = upgradeDatabase.GetRandomUpgrades(numberOfUpgradesToShow, shopWave, upgrades, shopRandom);
        
        Debug.Log($"[Shop] Absolute wave {absoluteWave} (Round {roguelikeManager.GetCurrentWaveNumber()}) - Randomized upgrades: {string.Join(", ", currentUpgradeTypes)}");
        
        // Setup upgrade buttons with the randomized upgrades
        for (int i = 0; i < numberOfUpgradesToShow && i < currentUpgradeTypes.Length; i++)
        {
            ShopUpgradeButton upgradeButton = GetUpgradeButton(i);
            Button buyButton = GetBuyButton(i);
            
            if (upgradeButton != null)
            {
                SetupUpgradeButton(upgradeButton, currentUpgradeTypes[i], i);
                UpdateTierVisuals(upgradeButton, currentUpgradeTypes[i]);
            }
            
            if (buyButton != null)
            {
                SetupBuyButton(buyButton, currentUpgradeTypes[i]);
            }
        }
    }
    
    /// <summary>
    /// Update tier visuals for an upgrade button
    /// </summary>
    private void UpdateTierVisuals(ShopUpgradeButton upgradeButton, UpgradeType upgradeType)
    {
        if (upgradeButton == null || upgradeButton.button == null || upgradeDatabase == null || currentUpgrades == null) return;
        
        UpgradeDefinition upgrade = upgradeDatabase.GetUpgrade(upgradeType);
        if (upgrade == null) return;
        
        // Find TierParent GameObject (sibling of the button within UpgradeN)
        // Hierarchy: Shop > UpgradeN > Button & TierParent (siblings)
        Transform upgradeParent = upgradeButton.button.transform.parent;
        if (upgradeParent == null)
        {
            Debug.LogWarning($"[Shop] Upgrade parent not found for button");
            return;
        }
        
        Transform tierParent = upgradeParent.Find("TierParent");
        if (tierParent == null)
        {
            Debug.LogWarning($"[Shop] TierParent not found in {upgradeParent.name}");
            return;
        }
        
        int currentTier = currentUpgrades.GetTierLevel(upgradeType);
        
        Debug.Log($"[Shop] Updating tier visuals for {upgradeType} - Tier: {currentTier}, IsPointsUpgrade: {upgrade.isPointsUpgrade}");
        
        // Check if this is a points upgrade (special case: show text instead of images)
        if (upgrade.isPointsUpgrade)
        {
            UpdatePointsTierDisplay(tierParent, upgradeType, currentTier);
        }
        else
        {
            UpdateImageTierDisplay(tierParent, currentTier, upgrade.maxTiers);
        }
    }
    
    /// <summary>
    /// Update tier display for points upgrades (shows "X + Y" text)
    /// </summary>
    private void UpdatePointsTierDisplay(Transform tierParent, UpgradeType upgradeType, int tiersPurchased)
    {
        Debug.Log($"[Shop] UpdatePointsTierDisplay for {upgradeType} - TierParent has {tierParent.childCount} children");
        
        // Hide all tier images (set both enabled and active to false)
        for (int i = 0; i < tierParent.childCount; i++)
        {
            Transform child = tierParent.GetChild(i);
            UnityEngine.UI.Image img = child.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.enabled = false;
                child.gameObject.SetActive(false);
                Debug.Log($"[Shop] Disabled tier image {i} for points upgrade");
            }
        }
        
        // Find or create TextMeshProUGUI component
        TextMeshProUGUI tierText = tierParent.GetComponent<TextMeshProUGUI>();
        if (tierText == null)
        {
            tierText = tierParent.gameObject.AddComponent<TextMeshProUGUI>();
            tierText.alignment = TextAlignmentOptions.Center;
            tierText.fontSize = 48;
            Debug.Log($"[Shop] Created new TierText component with font size 48");
        }
        else
        {
            tierText.fontSize = 48; // Ensure font size is correct
        }
        
        tierText.enabled = true;
        
        // Get default base points (before any upgrades)
        int defaultBase = 0;
        if (upgradeType == UpgradeType.RabbitPoints1 || upgradeType == UpgradeType.RabbitPoints3)
        {
            defaultBase = 5; // Default rabbit points
        }
        else if (upgradeType == UpgradeType.MeerkatPoints1 || upgradeType == UpgradeType.MeerkatPoints3)
        {
            defaultBase = 10; // Default meerkat points
        }
        
        // Get current total points (from PlayerUpgrades)
        int currentTotalPoints = 0;
        if (upgradeType == UpgradeType.RabbitPoints1 || upgradeType == UpgradeType.RabbitPoints3)
        {
            currentTotalPoints = currentUpgrades.basePointsPerRabbit;
        }
        else if (upgradeType == UpgradeType.MeerkatPoints1 || upgradeType == UpgradeType.MeerkatPoints3)
        {
            currentTotalPoints = currentUpgrades.basePointsPerMeerkat;
        }
        
        // Calculate bonus (total - default base)
        int bonusPoints = currentTotalPoints - defaultBase;
        
        tierText.text = $"{defaultBase} + {bonusPoints}";
        Debug.Log($"[Shop] Points tier text set to: '{tierText.text}' (default: {defaultBase}, current: {currentTotalPoints}, bonus: {bonusPoints})");
    }
    
    /// <summary>
    /// Update tier display for non-points upgrades (shows tier images)
    /// </summary>
    private void UpdateImageTierDisplay(Transform tierParent, int currentTier, int maxTiers)
    {
        Debug.Log($"[Shop] UpdateImageTierDisplay - CurrentTier: {currentTier}, MaxTiers: {maxTiers}, Children: {tierParent.childCount}");
        
        // Disable any text component
        TextMeshProUGUI tierText = tierParent.GetComponent<TextMeshProUGUI>();
        if (tierText != null)
        {
            tierText.enabled = false;
        }
        
        // Update tier images (assuming 5 image children in TierParent)
        for (int i = 0; i < Mathf.Min(5, tierParent.childCount); i++)
        {
            Transform child = tierParent.GetChild(i);
            UnityEngine.UI.Image tierImage = child.GetComponent<UnityEngine.UI.Image>();
            
            if (tierImage != null)
            {
                // Show/hide based on maxTiers
                if (i < maxTiers)
                {
                    child.gameObject.SetActive(true);
                    tierImage.enabled = true;
                    
                    // Set sprite based on whether tier is purchased
                    if (i < currentTier)
                    {
                        tierImage.sprite = tierBoughtSprite;
                        Debug.Log($"[Shop] Tier {i}: BOUGHT (sprite: {tierBoughtSprite?.name})");
                    }
                    else
                    {
                        tierImage.sprite = tierEmptySprite;
                        Debug.Log($"[Shop] Tier {i}: EMPTY (sprite: {tierEmptySprite?.name})");
                    }
                }
                else
                {
                    // Hide images beyond maxTiers
                    child.gameObject.SetActive(false);
                    tierImage.enabled = false;
                    Debug.Log($"[Shop] Tier {i}: HIDDEN (beyond max tiers)");
                }
            }
            else
            {
                Debug.LogWarning($"[Shop] Child {i} of TierParent has no Image component");
            }
        }
    }
    
    /// <summary>
    /// Get upgrade button by index
    /// </summary>
    private ShopUpgradeButton GetUpgradeButton(int index)
    {
        switch (index)
        {
            case 0: return upgradeButton1;
            case 1: return upgradeButton2;
            case 2: return upgradeButton3;
            case 3: return upgradeButton4;
            default: return null;
        }
    }
    
    /// <summary>
    /// Get buy button by index
    /// </summary>
    private Button GetBuyButton(int index)
    {
        switch (index)
        {
            case 0: return buyButton1;
            case 1: return buyButton2;
            case 2: return buyButton3;
            case 3: return buyButton4;
            default: return null;
        }
    }
    
    /// <summary>
    /// Refresh shop display
    /// </summary>
    public void RefreshShop()
    {
        if (roguelikeManager == null)
        {
            Debug.LogWarning("[Shop] RoguelikeManager is null!");
            return;
        }
        
        // Check if we're on a new wave and need to re-randomize upgrades
        int absoluteWave = roguelikeManager.GetAbsoluteWaveNumber();
        int shopWave = Mathf.Max(1, absoluteWave - 1);  // Shop is for the wave just completed
        
        if (shopWave != lastRandomizedWave)
        {
            Debug.Log($"[Shop] Detected wave change - Shop wave changed from {lastRandomizedWave} to {shopWave} (absolute wave: {absoluteWave}) - Re-randomizing shop!");
            RandomizeShopUpgrades();
        }
        
        currentUpgrades = roguelikeManager.GetPlayerUpgrades();
        
        if (currentUpgrades == null)
        {
            Debug.LogWarning("[Shop] currentUpgrades is null!");
            return;
        }
        
        // Update currency display with numeric amount
        if (currencyText != null)
        {
            int gold = currentUpgrades.currentGold;
            currencyText.text = $"${gold}";
            
            Debug.Log($"[Shop] Currency text updated: '{currencyText.text}' (gold={gold})");
        }
        else
        {
            Debug.LogWarning("[Shop] currencyText is null! Please assign it in the Inspector.");
        }
        
        // Update button affordability
        UpdateButtonStates();
        
        // Update reroll button affordability
        UpdateRerollButton();
        
        // Update tier visuals after affordability check
        UpdateAllTierVisuals();
    }
    
    /// <summary>
    /// Update reroll button affordability based on player's gold
    /// </summary>
    private void UpdateRerollButton()
    {
        if (rerollButton != null && currentUpgrades != null)
        {
            bool canAfford = currentUpgrades.currentGold >= rerollCost;
            rerollButton.interactable = canAfford;
            
            // Optionally update button text to show cost
            TextMeshProUGUI buttonText = rerollButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"Reroll (${rerollCost})";
            }
        }
    }
    
    /// <summary>
    /// Update tier visuals for all currently displayed upgrades
    /// </summary>
    private void UpdateAllTierVisuals()
    {
        if (currentUpgradeTypes == null) return;
        
        for (int i = 0; i < numberOfUpgradesToShow && i < currentUpgradeTypes.Length; i++)
        {
            ShopUpgradeButton upgradeButton = GetUpgradeButton(i);
            if (upgradeButton != null)
            {
                UpdateTierVisuals(upgradeButton, currentUpgradeTypes[i]);
            }
        }
    }
    
    /// <summary>
    /// Update which buy buttons can be afforded (upgrade buttons always stay at default color)
    /// </summary>
    private void UpdateButtonStates()
    {
        if (currentUpgradeTypes == null || currentUpgradeTypes.Length == 0)
        {
            return;
        }
        
        int currency = currentUpgrades.currentGold;
        
        // Update buy buttons based on their assigned upgrade types
        for (int i = 0; i < currentUpgradeTypes.Length && i < numberOfUpgradesToShow; i++)
        {
            Button buyButton = GetBuyButton(i);
            if (buyButton != null)
            {
                UpdateBuyButtonAffordability(buyButton, currentUpgradeTypes[i], currency);
            }
        }
    }
    
    /// <summary>
    /// Update affordability for a specific buy button based on its upgrade type
    /// </summary>
    private void UpdateBuyButtonAffordability(Button buyButton, UpgradeType upgradeType, int currency)
    {
        if (buyButton == null || upgradeDatabase == null || roguelikeManager == null) return;
        
        // Check if this specific upgrade was already purchased this wave
        bool purchasedThisWave = purchasedLimitedUpgradesThisWave.Contains(upgradeType);
        
        int currentRound = roguelikeManager.GetCurrentRound();
        bool affordable = upgradeDatabase.CanAfford(upgradeType, currency, currentUpgrades, purchasedThisWave, currentRound);
        buyButton.interactable = affordable;
    }
    
    /// <summary>
    /// Setup an upgrade button based on its assigned upgrade type
    /// </summary>
    private void SetupUpgradeButton(ShopUpgradeButton upgradeButton, UpgradeType upgradeType, int buttonIndex)
    {
        if (upgradeButton == null) return;
        
        string upgradeName = GetUpgradeName(upgradeType);
        int upgradeCost = GetUpgradeCost(upgradeType);
        Sprite upgradeSprite = GetUpgradeSprite(upgradeType);
        
        upgradeButton.SetupButton(upgradeName, upgradeCost, upgradeSprite, () => ShowUpgradeDescription(buttonIndex));
    }
    
    /// <summary>
    /// Setup a buy button based on its assigned upgrade type
    /// </summary>
    private void SetupBuyButton(Button buyButton, UpgradeType upgradeType)
    {
        if (buyButton == null) return;
        
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => PurchaseUpgrade(upgradeType));
    }
    
    /// <summary>
    /// Show description for an upgrade by button index
    /// </summary>
    private void ShowUpgradeDescription(int buttonIndex)
    {
        if (buttonIndex >= 0 && buttonIndex < descriptionPanels.Length && buttonIndex < buyButtons.Length)
        {
            UpgradeType upgradeType = GetUpgradeTypeForButton(buttonIndex);
            ShowDescription(descriptionPanels[buttonIndex], buyButtons[buttonIndex], upgradeType);
        }
        else
        {
            Debug.LogWarning($"[Shop] Invalid button index: {buttonIndex}");
        }
    }
    
    /// <summary>
    /// Get the display name for an upgrade type
    /// </summary>
    private string GetUpgradeName(UpgradeType upgradeType)
    {
        if (upgradeDatabase == null) return "Unknown Upgrade";
        UpgradeDefinition upgrade = upgradeDatabase.GetUpgrade(upgradeType);
        return upgrade != null ? upgrade.displayName : "Unknown Upgrade";
    }
    
    /// <summary>
    /// Get the cost for an upgrade type (scaled by current round)
    /// </summary>
    private int GetUpgradeCost(UpgradeType upgradeType)
    {
        if (upgradeDatabase == null || roguelikeManager == null) return 0;
        UpgradeDefinition upgrade = upgradeDatabase.GetUpgrade(upgradeType);
        if (upgrade == null) return 0;
        
        int currentRound = roguelikeManager.GetCurrentRound();
        return upgrade.GetCostForRound(currentRound);
    }
    
    /// <summary>
    /// Get the sprite for an upgrade type
    /// </summary>
    private Sprite GetUpgradeSprite(UpgradeType upgradeType)
    {
        if (upgradeDatabase == null) return null;
        UpgradeDefinition upgrade = upgradeDatabase.GetUpgrade(upgradeType);
        return upgrade != null ? upgrade.icon : null;
    }
    
    /// <summary>
    /// Get the description for an upgrade type
    /// </summary>
    private string GetUpgradeDescription(UpgradeType upgradeType)
    {
        if (upgradeDatabase == null) return "";
        UpgradeDefinition upgrade = upgradeDatabase.GetUpgrade(upgradeType);
        return upgrade != null ? upgrade.description : "";
    }
    
    /// <summary>
    /// A short "before -> after" line for the description panel, plus which upgrades this one locks out.
    /// </summary>
    private string GetUpgradePreview(UpgradeType upgradeType)
    {
        PlayerUpgrades u = currentUpgrades;
        if (u == null) return "";
        
        int tier = u.GetTierLevel(upgradeType);
        string change = "";
        switch (upgradeType)
        {
            case UpgradeType.RabbitPoints1:
                change = $"Rabbit: {u.basePointsPerRabbit} -> {u.basePointsPerRabbit + 1} pts"; break;
            case UpgradeType.RabbitPoints3:
                change = $"Rabbit: {u.basePointsPerRabbit} -> {u.basePointsPerRabbit + 3} pts"; break;
            case UpgradeType.MeerkatPoints1:
                change = $"Meerkat: {u.basePointsPerMeerkat} -> {u.basePointsPerMeerkat + 1} pts"; break;
            case UpgradeType.MeerkatPoints3:
                change = $"Meerkat: {u.basePointsPerMeerkat} -> {u.basePointsPerMeerkat + 3} pts"; break;
            case UpgradeType.SpawnRate:
                change = $"Max animals: {u.maxActiveAnimals} -> {u.maxActiveAnimals + 2}"; break;
            case UpgradeType.RoundLength:
                change = $"Wave time: {u.waveDuration:0.#}s -> {u.waveDuration + 2f:0.#}s"; break;
            case UpgradeType.ComboStarter:
                change = $"Combo: {u.comboHitsToStart} hits -> {u.comboHitsToStart - 1} hits\nGold per combo: {u.goldPerCombo:0.#} -> {u.goldPerCombo - 0.5f:0.#}"; break;
            case UpgradeType.ComboMultiplier:
                change = $"Combo multiplier: {u.comboMultiplierIncrement:0.#}x -> {u.comboMultiplierIncrement + 0.5f:0.#}x"; break;
            case UpgradeType.RabbitToMeerkat:
                change = $"Rabbits become meerkats: {u.rabbitToMeerkatConversion * 100f:0}% -> {(u.rabbitToMeerkatConversion + 0.15f) * 100f:0}%"; break;
            case UpgradeType.MeerkatToFox:
                change = $"Meerkats become foxes: {u.meerkatToFoxConversion * 100f:0}% -> {(u.meerkatToFoxConversion + 0.15f) * 100f:0}%"; break;
            case UpgradeType.FoxToBadger:
                change = $"Foxes become badgers: {u.foxToBadgerConversion * 100f:0}% -> {(u.foxToBadgerConversion + 0.15f) * 100f:0}%"; break;
            case UpgradeType.PerfectHitBonus:
                change = $"Perfect hit: {u.perfectHitMultiplier:0.#}x -> {u.perfectHitMultiplier + 0.5f:0.#}x"; break;
            case UpgradeType.CritChance:
                change = $"Crit chance: {u.critChance * 100f:0}% -> {(u.critChance + 0.05f) * 100f:0}%"; break;
            case UpgradeType.TimePerCombo:
            {
                float next = tier == 0 ? 0.5f : (tier == 1 ? 1f : 2f);
                change = $"Time per combo: +{u.timeAddedPerCombo:0.#}s -> +{next:0.#}s\nCombo: {u.comboHitsToStart} hits -> {u.comboHitsToStart + 1} hits";
                break;
            }
            case UpgradeType.ComboInterest:
            {
                float next = tier == 0 ? 0.5f : (tier == 1 ? 0.75f : 1f);
                change = $"Multiplier growth per combo: +{u.comboInterestRate:0.##}x -> +{next:0.##}x";
                break;
            }
        }
        
        string result = change.Length > 0 ? $"\n\n<size=85%><color=#FFD54A>{change}</color></size>" : "";
        
        // Mutually exclusive upgrades are hidden from the shop once locked, so say up front what this one closes off
        UpgradeDefinition definition = upgradeDatabase != null ? upgradeDatabase.GetUpgrade(upgradeType) : null;
        if (definition != null && definition.locksOutUpgrades != null && definition.locksOutUpgrades.Length > 0 && tier == 0)
        {
            System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
            foreach (UpgradeType locked in definition.locksOutUpgrades)
            {
                if (u.IsUpgradeLocked(locked)) continue;
                names.Add(GetUpgradeName(locked));
            }
            if (names.Count > 0)
            {
                result += $"\n<size=80%><color=#FF8A80>Locks out: {string.Join(", ", names)}</color></size>";
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Get the upgrade type assigned to a button index
    /// </summary>
    private UpgradeType GetUpgradeTypeForButton(int buttonIndex)
    {
        if (currentUpgradeTypes != null && buttonIndex >= 0 && buttonIndex < currentUpgradeTypes.Length)
        {
            return currentUpgradeTypes[buttonIndex];
        }
        
        Debug.LogWarning($"[Shop] Invalid button index {buttonIndex} or currentUpgradeTypes not initialized!");
        return UpgradeType.RabbitPoints1;
    }
    
    /// <summary>
    /// Purchase an upgrade based on its type
    /// </summary>
    private void PurchaseUpgrade(UpgradeType upgradeType)
    {
        if (upgradeDatabase == null)
        {
            Debug.LogWarning("[Shop] UpgradeDatabase is not assigned!");
            return;
        }
        
        UpgradeDefinition upgrade = upgradeDatabase.GetUpgrade(upgradeType);
        if (upgrade == null) return;
        
        // Get cost for current round
        int currentRound = roguelikeManager.GetCurrentRound();
        int cost = upgrade.GetCostForRound(currentRound);
        
        // Try to purchase
        if (TryPurchase(cost))
        {
            // Play cash register sound before applying the upgrade
            PlayCashRegisterSound();
            
            // Apply the upgrade effect
            upgradeDatabase.ApplyUpgradeEffect(upgradeType, roguelikeManager);
            
            // Increment tier level for this upgrade
            roguelikeManager.ApplyUpgrade(upgrades => upgrades.IncrementTier(upgradeType));
            
            // Lock out mutually exclusive upgrades (only on first purchase, tier 1)
            if (upgrade.locksOutUpgrades != null && upgrade.locksOutUpgrades.Length > 0)
            {
                int currentTier = currentUpgrades.GetTierLevel(upgradeType);
                
                // Only lock out on first purchase (when moving from tier 0 to tier 1)
                if (currentTier == 1)
                {
                    foreach (UpgradeType lockedType in upgrade.locksOutUpgrades)
                    {
                        roguelikeManager.ApplyUpgrade(upgrades => upgrades.LockUpgrade(lockedType));
                        Debug.Log($"[Shop] {upgradeType} locked out {lockedType}");
                    }
                }
            }
            
            // Track limited per wave upgrades
            if (upgrade.limitedPerWave)
            {
                purchasedLimitedUpgradesThisWave.Add(upgradeType);
                Debug.Log($"[Shop] Added {upgradeType} to limited upgrades purchased this wave");
            }
            
            int newTier = currentUpgrades.GetTierLevel(upgradeType) + 1;
            Debug.Log($"[Shop] Purchased {upgrade.displayName}! Now at tier {newTier}/{upgrade.maxTiers}");
            
            CloseAllDescriptions();
            RefreshShop();
        }
    }
    
    
    /// <summary>
    /// Try to purchase something for the given cost
    /// </summary>
    private bool TryPurchase(int cost)
    {
        if (currentUpgrades.currentGold >= cost)
        {
            roguelikeManager.ApplyUpgrade(upgrades => upgrades.currentGold -= cost);
            currentUpgrades = roguelikeManager.GetPlayerUpgrades(); // Refresh
            return true;
        }
        else
        {
            Debug.LogWarning($"[Shop] Not enough gold! Need ${cost}, have ${currentUpgrades.currentGold}");
            return false;
        }
    }
    
    /// <summary>
    /// Show a description panel and close all others
    /// </summary>
    private void ShowDescription(GameObject panelToShow, Button buyButton, UpgradeType upgradeType)
    {
        // Close all descriptions first
        CloseAllDescriptions();
        
        // Show the selected panel
        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            
            // Update the description text
            UpdateDescriptionPanel(panelToShow, upgradeType);
        }
        
        // Show the background panel
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(true);
        }
        
        // Update button affordability for all buy buttons
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Update the description panel with upgrade information
    /// </summary>
    private void UpdateDescriptionPanel(GameObject descriptionPanel, UpgradeType upgradeType)
    {
        if (descriptionPanel == null) return;
        
        // Find the UpgradeName text component (directly under the description panel)
        TextMeshProUGUI upgradeNameText = descriptionPanel.transform.Find("UpgradeName")?.GetComponent<TextMeshProUGUI>();
        if (upgradeNameText != null)
        {
            upgradeNameText.text = GetUpgradeName(upgradeType);
        }
        else
        {
            Debug.LogWarning($"[Shop] Could not find UpgradeName in {descriptionPanel.name}");
        }
        
        // Find the UpgradeDescription text component (nested under textBackground)
        TextMeshProUGUI upgradeDescriptionText = descriptionPanel.transform.Find("TextBackground/UpgradeDescription")?.GetComponent<TextMeshProUGUI>();
        if (upgradeDescriptionText != null)
        {
            upgradeDescriptionText.text = GetUpgradeDescription(upgradeType) + GetUpgradePreview(upgradeType);
        }
        else
        {
            Debug.LogWarning($"[Shop] Could not find TextBackground/UpgradeDescription in {descriptionPanel.name}");
        }
    }
    
    /// <summary>
    /// Close all description panels
    /// </summary>
    private void CloseAllDescriptions()
    {
        if (descriptionPanels != null)
        {
            foreach (GameObject panel in descriptionPanels)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }
        
        // Hide the background panel
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Reroll the shop to get new upgrade options
    /// </summary>
    private void RerollShop()
    {
        if (upgradeDatabase == null || roguelikeManager == null)
        {
            Debug.LogWarning("[Shop] Cannot reroll - missing references!");
            return;
        }
        
        // Check if player can afford the reroll
        if (currentUpgrades == null || currentUpgrades.currentGold < rerollCost)
        {
            Debug.LogWarning($"[Shop] Cannot afford reroll! Need ${rerollCost}, have ${currentUpgrades?.currentGold ?? 0}");
            return;
        }
        
        // Deduct reroll cost
        if (!TryPurchase(rerollCost))
        {
            return;
        }
        
        // Refresh current upgrades reference after purchase
        currentUpgrades = roguelikeManager.GetPlayerUpgrades();
        
        Debug.Log($"[Shop] Reroll purchased for ${rerollCost}. Remaining gold: ${currentUpgrades.currentGold}");
        
        // Close any open descriptions
        CloseAllDescriptions();
        
        // Get absolute wave for shop (the completed wave)
        int absoluteWave = roguelikeManager.GetAbsoluteWaveNumber();
        int shopWave = Mathf.Max(1, absoluteWave - 1);
        
        // Re-randomize upgrades for the same wave
        // Pass currentUpgrades to filter out maxed upgrades
        currentUpgradeTypes = upgradeDatabase.GetRandomUpgrades(numberOfUpgradesToShow, shopWave, currentUpgrades, shopRandom);
        
        Debug.Log($"[Shop] Rerolled! Wave {shopWave} - New upgrades: {string.Join(", ", currentUpgradeTypes)}");
        
        // Setup upgrade buttons with the new randomized upgrades
        for (int i = 0; i < numberOfUpgradesToShow && i < currentUpgradeTypes.Length; i++)
        {
            ShopUpgradeButton upgradeButton = GetUpgradeButton(i);
            Button buyButton = GetBuyButton(i);
            
            if (upgradeButton != null)
            {
                SetupUpgradeButton(upgradeButton, currentUpgradeTypes[i], i);
                UpdateTierVisuals(upgradeButton, currentUpgradeTypes[i]);
            }
            
            if (buyButton != null)
            {
                SetupBuyButton(buyButton, currentUpgradeTypes[i]);
            }
        }
        
        // Refresh the currency display
        if (currencyText != null && currentUpgrades != null)
        {
            int gold = currentUpgrades.currentGold;
            currencyText.text = $"${gold}";
            Debug.Log($"[Shop] Currency updated after reroll: ${gold}");
        }
        
        // Refresh affordability
        UpdateButtonStates();
        UpdateAllTierVisuals();
    }
    
    /// <summary>
    /// Play cash register sound effect
    /// </summary>
    private void PlayCashRegisterSound()
    {
        if (audioSource != null && cashRegisterSound != null)
        {
            audioSource.PlayOneShot(cashRegisterSound);
        }
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
    /// Continue to next wave
    /// </summary>
    private void ContinueToNextWave()
    {
        // Play button press sound
        PlayButtonPressSound();
        
        roguelikeManager.CloseShop();
    }
}

/// <summary>
/// Helper component for shop upgrade buttons
/// </summary>
[System.Serializable]
public class ShopUpgradeButton
{
    public Button button;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Image backgroundImage;
    public Image iconImage;  // The upgrade icon/sprite
    
    private System.Action onClick;
    private int cost;
    
    public void SetupButton(string upgradeName, int upgradeCost, Sprite upgradeIcon, System.Action onClickAction)
    {
        cost = upgradeCost;
        onClick = onClickAction;
        
        if (nameText != null)
        {
            nameText.text = upgradeName;
        }
        
        if (costText != null)
        {
            costText.text = $"${cost}";
        }
        
        if (iconImage != null && upgradeIcon != null)
        {
            iconImage.sprite = upgradeIcon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }
        
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
            // Always keep upgrade buttons interactable (they now show descriptions instead of buying)
            button.interactable = true;
        }
    }
}

