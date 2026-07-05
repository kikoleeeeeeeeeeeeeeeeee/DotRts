using UnityEngine;

public class FogOfWarPersisent : MonoBehaviour
{
    [SerializeField] private RenderTexture fogOfWarRenderTexture;
    [SerializeField] private RenderTexture fogOfWarPersistentRenderTextTure;
    [SerializeField] private RenderTexture fogOfWarPersistentRenderTextTure2;
    [SerializeField] private Material fogOfWarPersisitentMaterial;

    private void Start()
    { 
        Graphics.Blit(fogOfWarRenderTexture, fogOfWarPersistentRenderTextTure);
        Graphics.Blit(fogOfWarRenderTexture, fogOfWarPersistentRenderTextTure2);
    }

    private void Update()
    {
        Graphics.Blit(fogOfWarRenderTexture, fogOfWarPersistentRenderTextTure, fogOfWarPersisitentMaterial, 0);

        Graphics.CopyTexture(fogOfWarPersistentRenderTextTure, fogOfWarPersistentRenderTextTure2);
    }
}
