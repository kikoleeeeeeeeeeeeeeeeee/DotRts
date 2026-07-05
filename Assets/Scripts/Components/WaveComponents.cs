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
}

public struct StartWaveEvent : IComponentData
{
    public int waveIndex;
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
