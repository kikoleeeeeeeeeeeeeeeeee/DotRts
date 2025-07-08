using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu()]
public class UnitTypeSoList : ScriptableObject
{
    public List<UnitTypeSo> unitTypeSOlist;


     
     
    public UnitTypeSo GetUnitTypeSO(UnitTypeSo.UnitType unitType)
    {
        foreach (UnitTypeSo unitTypeSo in unitTypeSOlist)
        {
            if(unitTypeSo.unitType == unitType)
            {
                return unitTypeSo; 
            }
        }
        UnityEngine.Debug.Log("û�ҵ�unit����");
        return null;
    }
}
