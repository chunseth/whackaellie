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
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Button continueButton;
    
    [Header("Upgrade Buttons")]
    [SerializeField] private ShopUpgradeButton rabbitPointsButton;
    [SerializeField] private ShopUpgradeButton roundLengthButton;
    [SerializeField] private ShopUpgradeButton spawnRateButton;
    [SerializeField] private ShopUpgradeButton comboStarterButton;
    [SerializeField] private ShopUpgradeButton comboMultiplierButton;
    
    [Header("Upgrade Costs")]
    [SerializeField] private int rabbitPointsCost = 20;
    [SerializeField] private int roundLengthCost = 30;
    [SerializeField] private int spawnRateCost = 25;
    [SerializeField] private int comboStarterCost = 40;
    [SerializeField] private int comboMultiplierCost = 50;
    
    private PlayerUpgrades currentUpgrades;
    private bool hasBoughtRoundLengthThisWave = false;  // Track if round length was purchased this wave
    
    private void Start()
    {
        if (roguelikeManager == null)
        {
            roguelikeManager = FindObjectOfType<RoguelikeManager>();
        }
        
        // Setup button listeners
        if (rabbitPointsButton != null)
        {
            rabbitPointsButton.SetupButton("Animal Points +1", rabbitPointsCost, () => BuyRabbitPoints());
        }
        
        if (roundLengthButton != null)
        {
            roundLengthButton.SetupButton("Round Length +2s", roundLengthCost, () => BuyRoundLength());
        }
        
        if (spawnRateButton != null)
        {
            spawnRateButton.SetupButton("Spawn Rate +1.0/s", spawnRateCost, () => BuySpawnRate());
        }
        
        if (comboStarterButton != null)
        {
            comboStarterButton.SetupButton("Combo Starter -1", comboStarterCost, () => BuyComboStarter());
        }
        
        if (comboMultiplierButton != null)
        {
            comboMultiplierButton.SetupButton("Combo Mult +0.5x", comboMultiplierCost, () => BuyComboMultiplier());
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueToNextWave);
        }
    }
    
    private void OnEnable()
    {
        // Reset per-wave purchase trackers
        hasBoughtRoundLengthThisWave = false;
        
        RefreshShop();
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
    }
    
    /// <summary>
    /// Update which buttons can be afforded
    /// </summary>
    private void UpdateButtonStates()
    {
        int currency = currentUpgrades.currentGold;
        
        if (rabbitPointsButton != null)
            rabbitPointsButton.SetAffordable(currency >= rabbitPointsCost);
            
        if (roundLengthButton != null)
            roundLengthButton.SetAffordable(currency >= roundLengthCost && !hasBoughtRoundLengthThisWave);
            
        if (spawnRateButton != null)
            spawnRateButton.SetAffordable(currency >= spawnRateCost);
            
        if (comboStarterButton != null)
            comboStarterButton.SetAffordable(currency >= comboStarterCost && currentUpgrades.comboHitsToStart > 1);
            
        if (comboMultiplierButton != null)
            comboMultiplierButton.SetAffordable(currency >= comboMultiplierCost);
            
    }
    
    /// <summary>
    /// Purchase upgrade: Animal Points +1 (applies to all animals except snake)
    /// </summary>
    private void BuyRabbitPoints()
    {
        if (TryPurchase(rabbitPointsCost))
        {
            roguelikeManager.ApplyUpgrade(upgrades =>
            {
                upgrades.basePointsPerRabbit += 1;
                upgrades.basePointsPerMeerkat += 1;
            });
            Debug.Log($"[Shop] Purchased Animal Points! Rabbit: {currentUpgrades.basePointsPerRabbit}pt, Meerkat: {currentUpgrades.basePointsPerMeerkat}pt");
            RefreshShop();
        }
    }
    
    /// <summary>
    /// Purchase upgrade: Round Length +2s (limited to once per wave)
    /// </summary>
    private void BuyRoundLength()
    {
        if (hasBoughtRoundLengthThisWave)
        {
            Debug.LogWarning("[Shop] Round Length can only be purchased once per wave!");
            return;
        }
        
        if (TryPurchase(roundLengthCost))
        {
            roguelikeManager.ApplyUpgrade(upgrades => upgrades.waveDuration += 2f);
            hasBoughtRoundLengthThisWave = true;
            Debug.Log($"[Shop] Purchased Round Length! Now: {currentUpgrades.waveDuration}s (limited upgrade used)");
            RefreshShop();
        }
    }
    
    /// <summary>
    /// Purchase upgrade: Spawn Rate +1.0/s
    /// </summary>
    private void BuySpawnRate()
    {
        if (TryPurchase(spawnRateCost))
        {
            roguelikeManager.ApplyUpgrade(upgrades =>
            {
                upgrades.minSpawnRate += 1.0f;
                upgrades.maxSpawnRate += 1.0f;
                upgrades.maxActiveAnimals += 2;  // Also increase max animals on screen
            });
            Debug.Log($"[Shop] Purchased Spawn Rate! Now: {currentUpgrades.minSpawnRate}-{currentUpgrades.maxSpawnRate}/s, Max animals: {currentUpgrades.maxActiveAnimals}");
            RefreshShop();
        }
    }
    
    /// <summary>
    /// Purchase upgrade: Combo Starter -1 hit
    /// </summary>
    private void BuyComboStarter()
    {
        if (currentUpgrades.comboHitsToStart <= 1)
        {
            Debug.LogWarning("[Shop] Cannot reduce combo starter below 1!");
            return;
        }
        
        if (TryPurchase(comboStarterCost))
        {
            roguelikeManager.ApplyUpgrade(upgrades => upgrades.comboHitsToStart -= 1);
            Debug.Log($"[Shop] Purchased Combo Starter! Now: {currentUpgrades.comboHitsToStart} hits to start");
            RefreshShop();
        }
    }
    
    /// <summary>
    /// Purchase upgrade: Combo Multiplier +0.5x
    /// </summary>
    private void BuyComboMultiplier()
    {
        if (TryPurchase(comboMultiplierCost))
        {
            roguelikeManager.ApplyUpgrade(upgrades => upgrades.comboMultiplierIncrement += 0.5f);
            Debug.Log($"[Shop] Purchased Combo Multiplier! Now: {currentUpgrades.comboMultiplierIncrement}x");
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
    /// Continue to next wave
    /// </summary>
    private void ContinueToNextWave()
    {
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
    
    [Header("Colors")]
    public Color affordableColor = Color.green;
    public Color unaffordableColor = Color.gray;
    
    private System.Action onClick;
    private int cost;
    
    public void SetupButton(string upgradeName, int upgradeCost, System.Action onClickAction)
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
        
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
    
    public void SetAffordable(bool affordable)
    {
        if (button != null)
        {
            button.interactable = affordable;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = affordable ? affordableColor : unaffordableColor;
        }
    }
}

