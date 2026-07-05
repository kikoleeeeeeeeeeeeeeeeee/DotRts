using System;
using UnityEngine;

[Serializable]
public struct WaveData
{
    // 波次编号
    public int waveNumber;
    
    // 波次持续时间（秒）
    public float waveDuration;
    
    // 波次开始前的准备时间（秒）
    public float preparationTime;
    
    // 基础僵尸生成数量
    public int baseZombieCount;
    
    // 每波额外增加的僵尸数量
    public int additionalZombiePerWave;
    
    // 僵尸生成间隔时间（秒）
    public float spawnInterval;
    
    // 僵尸生命值倍率
    public float zombieHealthMultiplier;
    
    // 僵尸伤害倍率
    public float zombieDamageMultiplier;
    
    // 特殊波次标记
    public bool isSpecialWave;
    
    // 特殊波次类型
    public SpecialWaveType specialWaveType;
    
    // BOSS波次配置
    public BossWaveConfig bossWaveConfig;
}

[Serializable]
public enum SpecialWaveType
{
    Normal,
    Boss,
    Swarm
}

[Serializable]
public struct BossWaveConfig
{
    public int bossHealth;
    public int bossDamage;
    public float bossSpawnDelay;
    public int minionsCount;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "WaveData")]
public class WaveDataSo : ScriptableObject
{
    // 全局配置
    public float globalPreparationTime = 5f;
    public float baseSpawnInterval = 1f;
    public float spawnIntervalReductionPerWave = 0.05f;
    
    // 波次配置
    public WaveData[] waves;
}
