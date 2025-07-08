using System.Collections.Generic;
using UnityEngine;

public class BuildingPlaceMentManagerUi : MonoBehaviour
{
    [SerializeField] private RectTransform buildingContainer;
    [SerializeField] private RectTransform buildingTemplate;
    [SerializeField] private BuildingTypeListSo buildingTypeListSo;

    private Dictionary<BuildingTypeSo, BuildingPlaceMentManangerUi_ButtonSingle> buildingButtonDictionary;


    private void Awake()
    {

        buildingTemplate.gameObject.SetActive(false);

        buildingButtonDictionary = new Dictionary<BuildingTypeSo, BuildingPlaceMentManangerUi_ButtonSingle>();
        foreach(BuildingTypeSo buildingTypeSo in buildingTypeListSo.buildingTypeSoList)
        {
            if (!buildingTypeSo.showInBuildingPlacementManagerUi)
            {
                continue;
            }
            RectTransform buildingRectTransfrom = Instantiate(buildingTemplate, buildingContainer);
            buildingRectTransfrom.gameObject.SetActive(true);

            BuildingPlaceMentManangerUi_ButtonSingle buttonSingle =
                buildingRectTransfrom.GetComponent<BuildingPlaceMentManangerUi_ButtonSingle>();

            buildingButtonDictionary[buildingTypeSo] = buttonSingle;

            buttonSingle.Setup(buildingTypeSo);
        }
    }
    private void Start()
    {
        BuildingPlaceMentMananger.Instance.OnActiveBuildingTypeSOchanged += BuildingPlaceManager_OnActiveBuildingTypeSOchanged;
        UpdateSelectedVisual();
    }

    private void BuildingPlaceManager_OnActiveBuildingTypeSOchanged(object sender, System.EventArgs e)
    {
        UpdateSelectedVisual();
    }

    private void UpdateSelectedVisual()
    {
        foreach(BuildingTypeSo buildingTypeSo in buildingButtonDictionary.Keys)
        {
            buildingButtonDictionary[buildingTypeSo].HideSelected();
        }

        buildingButtonDictionary[BuildingPlaceMentMananger.Instance.GetActiveBuidlingTypeSO()].ShowSelected();

    }
}
