using Unity.Entities;
using Unity.Mathematics;

public struct WaveState : IComponentData
{
    public int currentWaveIndex;
    public bool isWaveActive;
    public bool isPreparationPhase;
    public bool isBasePlaced;
    public int zombiesKilledInCurrentWave;
    public float waveTimer;
    public float preparationTimer;
    public float spawnTimer;
    public int zombiesSpawnedInCurrentWave;
    public int totalZombiesToSpawn;
    public float spawnInterval;

    // 本波僵尸属性倍率（来自 WaveData）
    public float zombieHealthMultiplier;
    public float zombieDamageMultiplier;

    // Boss 波：延迟生成一只 Boss
    public bool isBossWave;
    public bool bossSpawned;
    public float bossSpawnTimer;
    public int bossHealth;
    public int bossDamage;
}

public struct StartWaveEvent : IComponentData
{
    public int waveIndex;
}

/// <summary>
/// 由 WaveManager 在波次结束时发出：销毁场上所有带 EnemyAttackHQ 的运行时敌人，避免僵尸跨波累积。
/// </summary>
public struct ClearFieldEnemiesEvent : IComponentData
{
}

public struct SpawnZombieBatchEvent : IComponentData
{
    public int zombieCount;
    public float spawnInterval;
}

public struct WaveConfiguration : ISharedComponentData
{
    public float globalPreparationTime;
    public float baseSpawnInterval;
    public float spawnIntervalReductionPerWave;
}

public struct WaveDataBuffer : IBufferElementData
{
    public int waveNumber;
    public float waveDuration;
    public float preparationTime;
    public int baseZombieCount;
    public int additionalZombiePerWave;
    public float spawnInterval;
    public float zombieHealthMultiplier;
    public float zombieDamageMultiplier;
    public bool isSpecialWave;
    public SpecialWaveType specialWaveType;
    public BossWaveConfig bossWaveConfig;
}
