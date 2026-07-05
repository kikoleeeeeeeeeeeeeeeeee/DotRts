using Mono.Cecil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResrouceManagerUi_Single : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI textMesh;


    public void SetUp(ResourceTypeSo resrouceTypeSo)
    {
        image.sprite = resrouceTypeSo.sprite;
        textMesh.text = "0";
    }

    public void UpdateAmount(int amount)
    {
        textMesh.text =amount.ToString();
    }
}
