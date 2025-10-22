using UnityEngine;
using System;

/// <summary>
/// Manages combo tracking and multiplier application
/// </summary>
public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance { get; private set; }
    
    [Header("Combo State")]
    [SerializeField] private int currentComboCount = 0;
    [SerializeField] private int accumulatedPoints = 0;  // Points stored until combo triggers
    
    [Header("Combo Settings (from upgrades)")]
    private int hitsToTriggerCombo = 3;      // Number of hits needed to trigger combo
    private float comboMultiplier = 2.0f;    // Multiplier applied when combo triggers
    
    // Events
    public event Action<int> OnComboChanged;  // Fires when combo count changes
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Initialize combo system with player upgrades
    /// </summary>
    public void Initialize(PlayerUpgrades upgrades)
    {
        hitsToTriggerCombo = upgrades.comboHitsToStart;
        comboMultiplier = upgrades.comboMultiplierIncrement;
        
        ResetCombo();
    }
    
    /// <summary>
    /// Call when player hits an animal
    /// Returns (basePoints, bonusPoints) - always awards base, bonus only on combo trigger
    /// </summary>
    public (int basePoints, int bonusPoints) OnAnimalHit(int basePoints)
    {
        currentComboCount++;
        accumulatedPoints += basePoints;
        
        Debug.Log($"[Combo] Hit {currentComboCount}/{hitsToTriggerCombo} - Accumulated: {accumulatedPoints}pts");
        
        // Check if combo threshold reached
        if (currentComboCount >= hitsToTriggerCombo)
        {
            // Trigger combo! Calculate bonus
            int totalWithMultiplier = Mathf.RoundToInt(accumulatedPoints * comboMultiplier);
            int bonusPoints = totalWithMultiplier - accumulatedPoints;
            
            Debug.Log($"[Combo] TRIGGERED! {accumulatedPoints}pts × {comboMultiplier}x = {totalWithMultiplier}pts (bonus: {bonusPoints}pts)");
            
            OnComboChanged?.Invoke(currentComboCount);
            
            // Reset for next combo
            ResetCombo();
            
            // Return base points + bonus from combo
            return (basePoints, bonusPoints);
        }
        else
        {
            // Still accumulating, just award base points
            OnComboChanged?.Invoke(currentComboCount);
            return (basePoints, 0);
        }
    }
    
    /// <summary>
    /// Call when player misses an animal
    /// </summary>
    public void OnAnimalMissed()
    {
        Debug.Log($"[Combo] Miss! Lost {accumulatedPoints} accumulated points (was at {currentComboCount}/{hitsToTriggerCombo})");
        ResetCombo();
    }
    
    /// <summary>
    /// Reset combo completely
    /// </summary>
    public void ResetCombo()
    {
        currentComboCount = 0;
        accumulatedPoints = 0;
        OnComboChanged?.Invoke(0);
    }
    
    /// <summary>
    /// Get current combo multiplier
    /// </summary>
    public float GetCurrentMultiplier()
    {
        return comboMultiplier;
    }
    
    /// <summary>
    /// Get current combo information
    /// </summary>
    public (int currentCount, int requiredCount, int accumulatedPoints) GetComboInfo()
    {
        return (currentComboCount, hitsToTriggerCombo, accumulatedPoints);
    }
    
}

