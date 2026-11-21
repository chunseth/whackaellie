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
    private bool hasComboMissShield = false; // If true, miss reduces combo by 1 instead of resetting
    
    [Header("Audio")]
    [SerializeField] private AudioClip regularBoinkSound;
    [SerializeField] private AudioClip combo1BoinkSound;
    [SerializeField] private AudioClip combo2BoinkSound;
    [SerializeField] private AudioClip combo3BoinkSound;
    private AudioSource audioSource;
    
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
            return;
        }
        
        // Get or add AudioSource component for sound effects
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
    }
    
    /// <summary>
    /// Initialize combo system with player upgrades
    /// </summary>
    public void Initialize(PlayerUpgrades upgrades)
    {
        hitsToTriggerCombo = upgrades.comboHitsToStart;
        
        // Apply base multiplier + any accumulated interest
        comboMultiplier = upgrades.comboMultiplierIncrement + upgrades.comboInterestAccumulated;
        hasComboMissShield = upgrades.hasComboMissShield;
        
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
        
        
        // Check if combo threshold reached
        if (currentComboCount >= hitsToTriggerCombo)
        {
            // Trigger combo! Calculate bonus
            int totalWithMultiplier = Mathf.RoundToInt(accumulatedPoints * comboMultiplier);
            int bonusPoints = totalWithMultiplier - accumulatedPoints;
            
            
            // Play combo completion sounds: combo 1 and 3 boink simultaneously
            PlayComboCompletionSound();
            
            OnComboChanged?.Invoke(currentComboCount);
            
            // Reset for next combo
            ResetCombo();
            
            // Return base points + bonus from combo
            return (basePoints, bonusPoints);
        }
        else
        {
            // Still accumulating - play combo build-up sounds
            PlayComboHitSound(currentComboCount);
            
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
        if (hasComboMissShield && currentComboCount > 0)
        {
            // With shield: reduce combo by 1 instead of resetting
            currentComboCount = Mathf.Max(0, currentComboCount - 1);
            
            // Recalculate accumulated points (lose 1 hit worth of points)
            // This is an approximation - we lose roughly the average point value
            if (currentComboCount > 0 && accumulatedPoints > 0)
            {
                // Lose points proportionally
                int pointsPerHit = accumulatedPoints / (currentComboCount + 1); // +1 because we just reduced count
                accumulatedPoints = Mathf.Max(0, accumulatedPoints - pointsPerHit);
            }
            else
            {
                accumulatedPoints = 0;
            }
            
            Debug.Log($"[Combo] Miss! Shield activated: -1 combo ({currentComboCount}/{hitsToTriggerCombo}, {accumulatedPoints}pts remaining)");
            OnComboChanged?.Invoke(currentComboCount);
        }
        else
        {
            // No shield or combo already at 0: reset completely
            Debug.Log($"[Combo] Miss! Lost {accumulatedPoints} accumulated points (was at {currentComboCount}/{hitsToTriggerCombo})");
            ResetCombo();
        }
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
    
    /// <summary>
    /// Play sound effect for combo build-up
    /// Hit 1 → regular boink, Hit 2 → combo1, Hit 3 → combo2, Hit 4 → combo3
    /// </summary>
    private void PlayComboHitSound(int comboCount)
    {
        if (audioSource == null) return;
        
        AudioClip soundToPlay = null;
        
        switch (comboCount)
        {
            case 1:
                soundToPlay = regularBoinkSound;
                break;
            case 2:
                soundToPlay = combo1BoinkSound;
                break;
            case 3:
                soundToPlay = combo2BoinkSound;
                break;
            case 4:
                soundToPlay = combo3BoinkSound;
                break;
            default:
                // For any other count, play regular boink as fallback
                soundToPlay = regularBoinkSound;
                break;
        }
        
        if (soundToPlay != null)
        {
            // Pitch climbs a little with each hit so the build-up toward a combo is audible
            float progress = hitsToTriggerCombo > 1 ? (comboCount - 1) / (float)(hitsToTriggerCombo - 1) : 0f;
            audioSource.pitch = 1f + 0.12f * Mathf.Clamp01(progress);
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    
    /// <summary>
    /// Play combo completion sound: combo 1 and 3 boink simultaneously
    /// </summary>
    private void PlayComboCompletionSound()
    {
        if (audioSource == null) return;
        
        audioSource.pitch = 1.15f;
        
        // Let the combo sound cut through the music
        if (MusicPlayer.Instance != null)
        {
            MusicPlayer.Instance.Duck(0.5f, 0.3f);
        }
        
        // Play combo 1 and combo 3 boinks at the same time
        if (combo1BoinkSound != null)
        {
            audioSource.PlayOneShot(combo1BoinkSound);
        }
        
        if (combo3BoinkSound != null)
        {
            audioSource.PlayOneShot(combo3BoinkSound);
        }
    }
    
}

