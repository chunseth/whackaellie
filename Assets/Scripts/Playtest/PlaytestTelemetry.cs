using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Three-layer playtest logging: spawn events, board snapshots, animal outcomes, round summaries.
/// CSV files written to Application.persistentDataPath/playtest_logs/
/// </summary>
public class PlaytestTelemetry : MonoBehaviour
{
    public static PlaytestTelemetry Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool loggingEnabled = true;
    [SerializeField] private float snapshotInterval = 0.5f;

    public bool LoggingEnabled => loggingEnabled;
    public bool IsWaveActive => isWaveActive;
    public bool IsWaveEnding => isWaveEnding;

    private string sessionId;
    private string logDirectory;

    private int currentRoundId;
    private int currentWaveId;
    private float waveStartTime;
    private bool isWaveActive;
    private bool isWaveEnding;

    // Per-wave counters (reset each wave)
    private int waveSpawnBatchSize;

    // Per-round accumulators (reset at round start)
    private float roundDuration;
    private int roundTotalSpawned;
    private int roundTotalHit;
    private int roundTotalExpired;
    private int roundTotalTimerEnded;
    private int roundPeakActive;
    private int roundPeakVisible;
    private float roundSecondsAtCap;
    private float roundSecondsAtZero;
    private float roundSecondsAt7Plus;
    private int roundFinalScoreSum;
    private bool roundAllWavesPassed;
    private UpgradeSnapshot roundUpgradeSnapshot;

    private Coroutine snapshotCoroutine;

    private const string SpawnFile = "spawn_events.csv";
    private const string OutcomeFile = "animal_outcomes.csv";
    private const string SnapshotFile = "board_snapshots.csv";
    private const string UpgradeFile = "upgrade_snapshots.csv";
    private const string SummaryFile = "round_summary.csv";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logDirectory = Path.Combine(Application.persistentDataPath, "playtest_logs", sessionId);

        if (loggingEnabled)
        {
            Directory.CreateDirectory(logDirectory);
            Debug.Log($"[PlaytestTelemetry] Session {sessionId} → {logDirectory}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public float WaveElapsed => isWaveActive ? Time.time - waveStartTime : 0f;

    public void SetSpawnBatchSize(int batchSize)
    {
        waveSpawnBatchSize = batchSize;
    }

    /// <summary>
    /// Called at the start of a round (wave 1 of that round).
    /// </summary>
    public void BeginRound(int roundId, PlayerUpgrades upgrades)
    {
        if (!loggingEnabled) return;

        currentRoundId = roundId;
        roundDuration = 0f;
        roundTotalSpawned = 0;
        roundTotalHit = 0;
        roundTotalExpired = 0;
        roundTotalTimerEnded = 0;
        roundPeakActive = 0;
        roundPeakVisible = 0;
        roundSecondsAtCap = 0f;
        roundSecondsAtZero = 0f;
        roundSecondsAt7Plus = 0f;
        roundFinalScoreSum = 0;
        roundAllWavesPassed = true;

        roundUpgradeSnapshot = BuildUpgradeSnapshot(roundId, upgrades);
        LogUpgradeSnapshot(roundUpgradeSnapshot);
    }

    /// <summary>
    /// Called when wave gameplay begins (after pre-wave animation).
    /// </summary>
    public void BeginWave(int roundId, int waveId, int maxActiveAllowed)
    {
        if (!loggingEnabled) return;

        currentRoundId = roundId;
        currentWaveId = waveId;
        waveStartTime = Time.time;
        isWaveActive = true;
        isWaveEnding = false;
        waveSpawnBatchSize = 0;

        if (snapshotCoroutine != null)
        {
            StopCoroutine(snapshotCoroutine);
        }

        snapshotCoroutine = StartCoroutine(SnapshotLoop(maxActiveAllowed));
    }

    /// <summary>
    /// Called when a wave ends (before ForceHide on moles).
    /// </summary>
    public void PrepareWaveEnd()
    {
        if (!loggingEnabled) return;
        isWaveEnding = true;
    }

    /// <summary>
    /// Called after wave cleanup completes.
    /// </summary>
    public void EndWave(bool success, int finalScore)
    {
        if (!loggingEnabled) return;

        isWaveActive = false;
        isWaveEnding = false;

        if (snapshotCoroutine != null)
        {
            StopCoroutine(snapshotCoroutine);
            snapshotCoroutine = null;
        }

        float waveDuration = Time.time - waveStartTime;
        roundDuration += waveDuration;
        roundFinalScoreSum += finalScore;

        if (!success)
        {
            roundAllWavesPassed = false;
        }
    }

    /// <summary>
    /// Flush round summary CSV row when the round is complete or failed.
    /// </summary>
    public void EndRound(bool passed)
    {
        if (!loggingEnabled) return;

        var snap = roundUpgradeSnapshot;
        float duration = Mathf.Max(roundDuration, 0.001f);
        int hitPlusExpired = roundTotalHit + roundTotalExpired;
        float accuracy = hitPlusExpired > 0 ? roundTotalHit / (float)hitPlusExpired : 0f;
        float spawnRate = roundTotalSpawned / duration;
        float hitRate = roundTotalHit / duration;
        float missRate = roundTotalExpired / duration;

        var row = new StringBuilder();
        row.Append(currentRoundId).Append(',');
        row.Append(duration.ToString("F2")).Append(',');
        row.Append(snap.minSpawnRate.ToString("F1")).Append(',');
        row.Append(snap.maxSpawnRate.ToString("F1")).Append(',');
        row.Append(snap.maxActiveAnimals).Append(',');
        row.Append(roundTotalSpawned).Append(',');
        row.Append(roundTotalHit).Append(',');
        row.Append(roundTotalExpired).Append(',');
        row.Append(roundTotalTimerEnded).Append(',');
        row.Append(accuracy.ToString("F3")).Append(',');
        row.Append(spawnRate.ToString("F2")).Append(',');
        row.Append(hitRate.ToString("F2")).Append(',');
        row.Append(missRate.ToString("F2")).Append(',');
        row.Append(roundPeakActive).Append(',');
        row.Append(roundPeakVisible).Append(',');
        row.Append(roundSecondsAtCap.ToString("F2")).Append(',');
        row.Append(roundSecondsAtZero.ToString("F2")).Append(',');
        row.Append(roundSecondsAt7Plus.ToString("F2")).Append(',');
        row.Append(roundFinalScoreSum).Append(',');
        row.Append(passed && roundAllWavesPassed ? 1 : 0);

        AppendCsvLine(SummaryFile,
            "roundId,duration,minSpawn,maxSpawn,maxActive,totalSpawned,totalHit,totalExpired,totalTimerEnded,accuracy,spawnRate,hitRate,missRate,peakActive,peakVisible,secondsAtCap,secondsAtZero,secondsAt7Plus,finalScore,passed",
            row.ToString());

        Debug.Log($"[PlaytestTelemetry] Round {currentRoundId} summary → {logDirectory}/{SummaryFile}");
    }

    public void LogSpawn(float time, int activeBefore, int activeAfter, int maxActiveAllowed,
        bool wasBlockedByCap, string animalType)
    {
        if (!loggingEnabled || !isWaveActive) return;

        roundTotalSpawned++;

        var row = new StringBuilder();
        row.Append(sessionId).Append(',');
        row.Append(currentRoundId).Append(',');
        row.Append(currentWaveId).Append(',');
        row.Append(time.ToString("F3")).Append(',');
        row.Append(waveSpawnBatchSize).Append(',');
        row.Append(activeBefore).Append(',');
        row.Append(activeAfter).Append(',');
        row.Append(maxActiveAllowed).Append(',');
        row.Append(wasBlockedByCap ? 1 : 0).Append(',');
        row.Append(animalType);

        AppendCsvLine(SpawnFile,
            "sessionId,roundId,waveId,time,waveSize,activeBefore,activeAfter,maxActiveAllowed,wasBlockedByCap,animalType",
            row.ToString());
    }

    public void LogSpawnBlocked(float time, int activeBefore, int maxActiveAllowed, int attemptedWaveSize)
    {
        if (!loggingEnabled || !isWaveActive) return;

        var row = new StringBuilder();
        row.Append(sessionId).Append(',');
        row.Append(currentRoundId).Append(',');
        row.Append(currentWaveId).Append(',');
        row.Append(time.ToString("F3")).Append(',');
        row.Append(attemptedWaveSize).Append(',');
        row.Append(activeBefore).Append(',');
        row.Append(activeBefore).Append(',');
        row.Append(maxActiveAllowed).Append(',');
        row.Append(1).Append(',');
        row.Append("Blocked");

        AppendCsvLine(SpawnFile,
            "sessionId,roundId,waveId,time,waveSize,activeBefore,activeAfter,maxActiveAllowed,wasBlockedByCap,animalType",
            row.ToString());
    }

    public void LogAnimalOutcome(float spawnTime, float outcomeTime, string animalType, string outcome,
        int activeAtOutcome, float lifetime, bool countsTowardAccuracy)
    {
        if (!loggingEnabled) return;

        var row = new StringBuilder();
        row.Append(sessionId).Append(',');
        row.Append(currentRoundId).Append(',');
        row.Append(currentWaveId).Append(',');
        row.Append(spawnTime.ToString("F3")).Append(',');
        row.Append(outcomeTime.ToString("F3")).Append(',');
        row.Append(animalType).Append(',');
        row.Append(outcome).Append(',');
        row.Append(activeAtOutcome).Append(',');
        row.Append(lifetime.ToString("F3"));

        AppendCsvLine(OutcomeFile,
            "sessionId,roundId,waveId,spawnTime,outcomeTime,animalType,outcome,activeAtOutcome,lifetime",
            row.ToString());

        if (!countsTowardAccuracy) return;

        switch (outcome)
        {
            case "Hit":
                roundTotalHit++;
                break;
            case "Expired":
                roundTotalExpired++;
                break;
            case "RoundEnded":
                roundTotalTimerEnded++;
                break;
        }
    }

    public void LogBoardSnapshot(float time, int activeUnhit, int hitAnimating, int totalVisible, int maxActiveAllowed)
    {
        if (!loggingEnabled || !isWaveActive) return;

        roundPeakActive = Mathf.Max(roundPeakActive, activeUnhit);
        roundPeakVisible = Mathf.Max(roundPeakVisible, totalVisible);

        float dt = snapshotInterval;
        if (activeUnhit >= maxActiveAllowed)
        {
            roundSecondsAtCap += dt;
        }

        if (activeUnhit == 0)
        {
            roundSecondsAtZero += dt;
        }

        if (activeUnhit >= 7)
        {
            roundSecondsAt7Plus += dt;
        }

        var row = new StringBuilder();
        row.Append(sessionId).Append(',');
        row.Append(currentRoundId).Append(',');
        row.Append(currentWaveId).Append(',');
        row.Append(time.ToString("F3")).Append(',');
        row.Append(activeUnhit).Append(',');
        row.Append(hitAnimating).Append(',');
        row.Append(totalVisible).Append(',');
        row.Append(maxActiveAllowed);

        AppendCsvLine(SnapshotFile,
            "sessionId,roundId,waveId,time,activeUnhitAnimals,hitAnimatingAnimals,totalVisibleAnimals,maxActiveAllowed",
            row.ToString());
    }

    private IEnumerator SnapshotLoop(int maxActiveAllowed)
    {
        var wait = new WaitForSeconds(snapshotInterval);

        while (isWaveActive)
        {
            MoleSpawner spawner = FindObjectOfType<MoleSpawner>();
            if (spawner != null)
            {
                spawner.GetBoardTelemetryCounts(out int activeUnhit, out int hitAnimating, out int totalVisible);
                int cap = spawner.GetMaxActiveAllowed();
                if (cap <= 0) cap = maxActiveAllowed;
                LogBoardSnapshot(WaveElapsed, activeUnhit, hitAnimating, totalVisible, cap);
            }

            yield return wait;
        }
    }

    private static UpgradeSnapshot BuildUpgradeSnapshot(int roundId, PlayerUpgrades upgrades)
    {
        if (upgrades == null)
        {
            return new UpgradeSnapshot();
        }

        return new UpgradeSnapshot
        {
            roundId = roundId,
            spawnRatePurchases = upgrades.GetTierLevel(UpgradeType.SpawnRate),
            minSpawnRate = upgrades.minSpawnRate,
            maxSpawnRate = upgrades.maxSpawnRate,
            maxActiveAnimals = upgrades.maxActiveAnimals,
            roundDuration = upgrades.waveDuration,
            comboHitsToStart = upgrades.comboHitsToStart,
            comboMultiplier = upgrades.comboMultiplierIncrement,
            critChance = upgrades.critChance,
            perfectHitMultiplier = upgrades.perfectHitMultiplier,
            rabbitToMeerkat = upgrades.rabbitToMeerkatConversion,
            meerkatToFox = upgrades.meerkatToFoxConversion,
            foxToBadger = upgrades.foxToBadgerConversion,
            hasComboMissShield = upgrades.hasComboMissShield,
            comboInterestRate = upgrades.comboInterestRate,
            timeAddedPerCombo = upgrades.timeAddedPerCombo
        };
    }

    private void LogUpgradeSnapshot(UpgradeSnapshot snap)
    {
        var row = new StringBuilder();
        row.Append(sessionId).Append(',');
        row.Append(snap.roundId).Append(',');
        row.Append(snap.spawnRatePurchases).Append(',');
        row.Append(snap.minSpawnRate.ToString("F1")).Append(',');
        row.Append(snap.maxSpawnRate.ToString("F1")).Append(',');
        row.Append(snap.maxActiveAnimals).Append(',');
        row.Append(snap.roundDuration.ToString("F1")).Append(',');
        row.Append(snap.comboHitsToStart).Append(',');
        row.Append(snap.comboMultiplier.ToString("F1")).Append(',');
        row.Append(snap.critChance.ToString("F2")).Append(',');
        row.Append(snap.perfectHitMultiplier.ToString("F1")).Append(',');
        row.Append(snap.rabbitToMeerkat.ToString("F2")).Append(',');
        row.Append(snap.meerkatToFox.ToString("F2")).Append(',');
        row.Append(snap.foxToBadger.ToString("F2")).Append(',');
        row.Append(snap.hasComboMissShield ? 1 : 0).Append(',');
        row.Append(snap.comboInterestRate.ToString("F2")).Append(',');
        row.Append(snap.timeAddedPerCombo.ToString("F1"));

        AppendCsvLine(UpgradeFile,
            "sessionId,roundId,spawnRatePurchases,minSpawnRate,maxSpawnRate,maxActiveAnimals,waveDuration,comboHitsToStart,comboMultiplier,critChance,perfectHitMultiplier,rabbitToMeerkat,meerkatToFox,foxToBadger,hasComboMissShield,comboInterestRate,timeAddedPerCombo",
            row.ToString());
    }

    private void AppendCsvLine(string fileName, string header, string line)
    {
        string path = Path.Combine(logDirectory, fileName);
        bool writeHeader = !File.Exists(path);

        using (var writer = new StreamWriter(path, true, Encoding.UTF8))
        {
            if (writeHeader)
            {
                writer.WriteLine(header);
            }

            writer.WriteLine(line);
        }
    }

    [Serializable]
    public struct UpgradeSnapshot
    {
        public int roundId;
        public int spawnRatePurchases;
        public float minSpawnRate;
        public float maxSpawnRate;
        public int maxActiveAnimals;
        public float roundDuration;
        public int comboHitsToStart;
        public float comboMultiplier;
        public float critChance;
        public float perfectHitMultiplier;
        public float rabbitToMeerkat;
        public float meerkatToFox;
        public float foxToBadger;
        public bool hasComboMissShield;
        public float comboInterestRate;
        public float timeAddedPerCombo;
    }
}
