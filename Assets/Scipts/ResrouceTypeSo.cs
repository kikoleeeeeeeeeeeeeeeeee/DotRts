using UnityEngine;

[CreateAssetMenu()]
public class ResrouceTypeSo : ScriptableObject{

    public enum ResourceType
    {
        None,
        Iron,
        Gold,
        Oil,
    }
    public ResourceType type;
    public Sprite sprite;
    
}
