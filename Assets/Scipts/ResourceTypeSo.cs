using UnityEngine;

[CreateAssetMenu()]
public class ResourceTypeSo : ScriptableObject{

    public enum ResourceType
    {
        None,
        Iron,
        Gold,
        Oil,
    }
    public ResourceType resourcetype;
    public Sprite sprite;
    
}
