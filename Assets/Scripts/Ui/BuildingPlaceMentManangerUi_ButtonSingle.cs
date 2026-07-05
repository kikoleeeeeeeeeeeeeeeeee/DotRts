using UnityEngine;
using UnityEngine.UI;

public class BuildingPlaceMentManangerUi_ButtonSingle : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedImage;

    private BuildingTypeSo buildingTypeSo;
    
    public void Setup(BuildingTypeSo buildingTypeSo)
    {
        this.buildingTypeSo = buildingTypeSo;

        GetComponent<Button>().onClick.AddListener(() =>
        {
            BuildingPlaceMentMananger.Instance.SetActiveBuildingTypeSO(buildingTypeSo);
        });

        iconImage.sprite = buildingTypeSo.sprite;
    }
    public void ShowSelected()
    {
        selectedImage.enabled = true;
    }
    public void HideSelected()
    {
        selectedImage.enabled=false;
    }
}
