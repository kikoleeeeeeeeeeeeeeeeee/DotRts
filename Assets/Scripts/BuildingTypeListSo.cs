using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BuildingTypeListSo : ScriptableObject
{
    public List<BuildingTypeSo> buildingTypeSoList;

    public BuildingTypeSo none;

    public BuildingTypeSo GetBuildingTypeSo(BuildingTypeSo.BuildingType buildingType)
    {
        foreach (BuildingTypeSo buildingTypeSo in buildingTypeSoList)
        {
            if (buildingTypeSo.buildingType == buildingType)
            {
                return buildingTypeSo;
            }
        }
        Debug.LogError("ÕÒ²»µ½½¨Öþ");
        return null;
    }
}
