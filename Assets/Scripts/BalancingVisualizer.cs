using UnityEngine;

/// <summary>
/// Helper script to visualize and export balancing data
/// Attach this to a GameObject with RoguelikeBalancer to see formatted output
/// </summary>
[RequireComponent(typeof(RoguelikeBalancer))]
public class BalancingVisualizer : MonoBehaviour
{
    private RoguelikeBalancer balancer;

    [Header("Export Options")]
    [SerializeField] private bool exportToJSON = false;
    [SerializeField] private bool exportToCSV = true;

    [ContextMenu("Generate Formatted Report")]
    public void GenerateFormattedReport()
    {
        // Get balancer reference
        if (balancer == null)
        {
            balancer = GetComponent<RoguelikeBalancer>();
        }
        
        if (balancer == null)
        {
            Debug.LogError("BalancingVisualizer: RoguelikeBalancer component not found! Please attach RoguelikeBalancer to the same GameObject.");
            return;
        }
        
        // Run calculations
        balancer.CalculateAllWaveBalancing();
        
        Debug.Log("\n\n╔════════════════════════════════════════════════════════════════╗");
        Debug.Log("║         WHACK-A-MOLE ROGUELIKE: BALANCING REPORT             ║");
        Debug.Log("╚════════════════════════════════════════════════════════════════╝\n");

        PrintDetailedBreakdown();
        
        if (exportToCSV)
        {
            ExportToCSV();
        }
    }

    private void PrintDetailedBreakdown()
    {
        for (int round = 1; round <= 8; round++)
        {
            Debug.Log($"\n┌─── ROUND {round} " + new string('─', 50));
            
            var waves = balancer.GetRoundWaves(round);
            
            if (waves == null || waves.Count == 0)
            {
                Debug.LogWarning($"│ No wave data found for Round {round}");
                Debug.Log("└" + new string('─', 60));
                continue;
            }
            
            int roundTotal = 0;
            foreach (var wave in waves)
            {
                if (wave != null)
                {
                    roundTotal += wave.pointThreshold;
                }
            }

            Debug.Log($"│ Round Total Points: {roundTotal}");
            if (waves[0] != null && waves[0].comboTier != null)
            {
                Debug.Log($"│ Combo Tier: {waves[0].comboTier.description}");
            }
            Debug.Log("│");

            foreach (var wave in waves)
            {
                if (wave != null)
                {
                    PrintWaveDetails(wave);
                }
            }
            
            Debug.Log("└" + new string('─', 60));
        }
    }

    private void PrintWaveDetails(RoguelikeBalancer.WaveData wave)
    {
        if (wave == null)
        {
            Debug.LogWarning("│   Wave data is null");
            return;
        }
        
        string waveLabel = wave.waveNumber switch
        {
            1 => "Wave 1 (Warm-up)",
            2 => "Wave 2 (Standard)",
            3 => "Wave 3 (BOSS)",
            _ => $"Wave {wave.waveNumber}"
        };

        Debug.Log($"│ {waveLabel}");
        Debug.Log($"│   ├─ Target Points: {wave.pointThreshold}");
        Debug.Log($"│   ├─ Duration: {wave.duration}s");
        Debug.Log($"│   ├─ Spawn Rate: {wave.minSpawnRate}-{wave.maxSpawnRate} animals/second");
        Debug.Log($"│   └─ Animals:");

        int totalCount = 0;
        int totalValue = 0;
        
        if (wave.animalCounts == null)
        {
            Debug.LogWarning("│       No animal count data");
            return;
        }
        
        foreach (var kvp in wave.animalCounts)
        {
            string animalName = kvp.Key.ToString().PadRight(10);
            int count = kvp.Value;
            int pointValue = GetAnimalPointValue(kvp.Key);
            int totalPoints = count * pointValue;
            
            totalCount += count;
            totalValue += totalPoints;
            
            string sign = pointValue >= 0 ? "+" : "";
            Debug.Log($"│       • {animalName}: {count,3}x @ {sign}{pointValue,3}pts = {totalPoints,5}pts");
        }
        
        Debug.Log($"│       ────────────────────────────────────");
        Debug.Log($"│       Total: {totalCount} animals, {totalValue} raw points");
        Debug.Log("│");
    }

    private int GetAnimalPointValue(AnimalType type)
    {
        return type switch
        {
            AnimalType.Rabbit => 5,
            AnimalType.Meerkat => 10,
            AnimalType.Fox => 15,
            AnimalType.Badger => 25,
            AnimalType.Snake => -10,
            _ => 0
        };
    }

    private void ExportToCSV()
    {
        if (balancer == null)
        {
            Debug.LogError("Cannot export CSV: Balancer is null");
            return;
        }
        
        System.Text.StringBuilder csv = new System.Text.StringBuilder();
        
        // Header
        csv.AppendLine("Round,Wave,Type,Target_Points,Duration_Sec,Min_Spawn_Rate,Max_Spawn_Rate,Rabbits,Meerkats,Foxes,Badgers,Snakes,Total_Animals,Raw_Points,Combo_Tier");

        for (int round = 1; round <= 8; round++)
        {
            var waves = balancer.GetRoundWaves(round);
            
            if (waves == null || waves.Count == 0)
            {
                Debug.LogWarning($"Skipping Round {round} in CSV export - no wave data");
                continue;
            }
            
            foreach (var wave in waves)
            {
                string waveType = wave.waveNumber == 3 ? "BOSS" : $"W{wave.waveNumber}";
                
                int rabbits = wave.animalCounts.ContainsKey(AnimalType.Rabbit) ? wave.animalCounts[AnimalType.Rabbit] : 0;
                int meerkats = wave.animalCounts.ContainsKey(AnimalType.Meerkat) ? wave.animalCounts[AnimalType.Meerkat] : 0;
                int foxes = wave.animalCounts.ContainsKey(AnimalType.Fox) ? wave.animalCounts[AnimalType.Fox] : 0;
                int badgers = wave.animalCounts.ContainsKey(AnimalType.Badger) ? wave.animalCounts[AnimalType.Badger] : 0;
                int snakes = wave.animalCounts.ContainsKey(AnimalType.Snake) ? wave.animalCounts[AnimalType.Snake] : 0;
                
                int totalAnimals = rabbits + meerkats + foxes + badgers + snakes;
                int rawPoints = (rabbits * 5) + (meerkats * 10) + (foxes * 15) + (badgers * 25) + (snakes * -10);
                
                csv.AppendLine($"{round},{wave.waveNumber},{waveType},{wave.pointThreshold},{wave.duration},{wave.minSpawnRate},{wave.maxSpawnRate},{rabbits},{meerkats},{foxes},{badgers},{snakes},{totalAnimals},{rawPoints},{wave.comboTier.tierLevel}");
            }
        }

        Debug.Log("\n=== CSV EXPORT ===");
        Debug.Log(csv.ToString());
        Debug.Log("=== END CSV ===\n");
        
        // Optionally write to file
        string path = Application.dataPath + "/balancing_export.csv";
        System.IO.File.WriteAllText(path, csv.ToString());
        Debug.Log($"Exported to: {path}");
    }

    [ContextMenu("Print Quick Reference")]
    public void PrintQuickReference()
    {
        // Get balancer reference
        if (balancer == null)
        {
            balancer = GetComponent<RoguelikeBalancer>();
        }
        
        if (balancer == null)
        {
            Debug.LogError("BalancingVisualizer: RoguelikeBalancer component not found!");
            return;
        }
        
        // Make sure calculations are done
        balancer.CalculateAllWaveBalancing();
        
        Debug.Log("\n╔════════════════════════════════════════════════════════════════╗");
        Debug.Log("║                    QUICK REFERENCE GUIDE                      ║");
        Debug.Log("╚════════════════════════════════════════════════════════════════╝\n");

        Debug.Log("ANIMAL VALUES:");
        Debug.Log("  Rabbit   :   +5pt  (Fast, common)");
        Debug.Log("  Meerkat  :  +10pt  (Medium)");
        Debug.Log("  Fox      :  +15pt  (Quick, valuable)");
        Debug.Log("  Badger   :  +25pt  (Slow, high value)");
        Debug.Log("  Snake    :  -10pt  (PENALTY!)");

        Debug.Log("\nCOMBO PROGRESSION:");
        Debug.Log("  After Boss 1: 3 hits → 2.0x mult (5 hits)");
        Debug.Log("  After Boss 2: 2 hits → 2.5x mult (8 hits)");
        Debug.Log("  After Boss 3: 2 hits → 3.0x mult (12 hits)");
        Debug.Log("  After Boss 4: 1 hit  → 3.5x mult (15 hits)");

        Debug.Log("\nPOINT THRESHOLDS BY ROUND:");
        for (int round = 1; round <= 8; round++)
        {
            var waves = balancer.GetRoundWaves(round);
            if (waves != null && waves.Count == 3)
            {
                Debug.Log($"  Round {round}: {waves[0].pointThreshold}pt → {waves[1].pointThreshold}pt → {waves[2].pointThreshold}pt (Boss)");
            }
        }

        Debug.Log("\nDIFFICULTY SCALING:");
        Debug.Log("  • Each round multiplies difficulty by 1.5x");
        Debug.Log("  • Boss waves are 30% harder than standard waves");
        Debug.Log("  • Assumes 75% player accuracy");
        Debug.Log("  • Later rounds introduce harder animals");
    }
}

