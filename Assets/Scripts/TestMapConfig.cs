using UnityEngine;

public class TestMapConfig : MonoBehaviour
{
    public MapConfigSo mapConfigSo;
    
    private void Start()
    {
        if (mapConfigSo != null)
        {
            Debug.Log("MapConfigSo is properly referenced!");
            Debug.Log($"Map size: {mapConfigSo.mapConfig.mapWidth}x{mapConfigSo.mapConfig.mapHeight}");
        }
        else
        {
            Debug.LogError("MapConfigSo is not referenced!");
        }
    }
}