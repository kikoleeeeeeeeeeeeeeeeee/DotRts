using System;
using System.Collections.Generic;
using UnityEngine;

public class ResrouceManager : MonoBehaviour
{

    public static ResrouceManager Instance {  get; private set; }

    public event EventHandler OnResouceAmountChanged;


    [SerializeField] private ResrouceTypeListSo resourceTypeListSO;
    private Dictionary<ResourceTypeSo.ResourceType, int> resourceTypeAmountDicitonary;

    /// <summary>资源获取倍率（由 BuffManager 的资源类 Buff 叠加），只影响正收益。</summary>
    [NonSerialized] public float GainMultiplier = 1f;


    private void Awake()
    {

        Instance = this;
        resourceTypeAmountDicitonary = new Dictionary<ResourceTypeSo.ResourceType, int>();

        foreach (ResourceTypeSo resrouceTypeSo in resourceTypeListSO.resrouceTySoList)
        {
            resourceTypeAmountDicitonary[resrouceTypeSo.resourcetype] = 0;
        }
        AddResourceAmount(ResourceTypeSo.ResourceType.Iron, 50);
        AddResourceAmount(ResourceTypeSo.ResourceType.Gold, 50);
        AddResourceAmount(ResourceTypeSo.ResourceType.Oil, 50);

    }

    public void AddResourceAmount(ResourceTypeSo.ResourceType resourceType, int amount)
    {
        int finalAmount = amount;
        if (amount > 0 && GainMultiplier > 1f)
        {
            finalAmount = Mathf.Max(1, Mathf.RoundToInt(amount * GainMultiplier));
        }

        resourceTypeAmountDicitonary[resourceType] += finalAmount;
        OnResouceAmountChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetResourceAmount(ResourceTypeSo.ResourceType resourceType)
    {
        return resourceTypeAmountDicitonary[resourceType];
    }
     


    public bool CanSpendResourceAmount(ResourceAmount resourceAmount)
    {
        return resourceTypeAmountDicitonary[resourceAmount.resourceType] >= resourceAmount.amount;
    }
    public bool CanSpendResourceAmount(ResourceAmount[] resourceAmountArray)
    {
        foreach (ResourceAmount resourceAmount in resourceAmountArray)
        {
            if (resourceTypeAmountDicitonary[resourceAmount.resourceType] < resourceAmount.amount)
            {
                return false;
            }
        }
        return true;
    }  
    public void SpendResourceAmount(ResourceAmount resourceAmount)
    {
        resourceTypeAmountDicitonary[resourceAmount.resourceType] -= resourceAmount.amount;
        OnResouceAmountChanged?.Invoke(this, EventArgs.Empty);
    }
    public void SpendResourceAmount(ResourceAmount[] resourceAmountArray)
    {
        foreach (ResourceAmount resourceAmount in resourceAmountArray)
        {
            resourceTypeAmountDicitonary[resourceAmount.resourceType] -= resourceAmount.amount;
        }
        OnResouceAmountChanged?.Invoke(this, EventArgs.Empty);

    }
}
