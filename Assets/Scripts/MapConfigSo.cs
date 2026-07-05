using UnityEngine;

[CreateAssetMenu(menuName = "MapConfig")]
public class MapConfigSo : ScriptableObject
{
    public MapConfig mapConfig;

    public GameObject[] obstaclePrefabs;

    public GameObject[] resourcePrefabs;

    public Material[] terrainMaterials;
}