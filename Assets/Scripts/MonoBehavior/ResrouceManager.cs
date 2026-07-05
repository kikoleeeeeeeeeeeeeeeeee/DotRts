using System;
using System.Collections.Generic;
using UnityEngine;

public class ResrouceManager : MonoBehaviour
{

    public static ResrouceManager Instance {  get; private set; }

    public event EventHandler OnResouceAmountChanged;


    [SerializeField] private ResrouceTypeListSo resourceTypeListSO;
    private Dictionary<ResourceTypeSo.ResourceType, int> resourceTypeAmountDicitonary;


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
        resourceTypeAmountDicitonary[resourceType] += amount;
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
