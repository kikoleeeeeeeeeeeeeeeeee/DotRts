using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class ResrouceManagerUi : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private Transform template;
    [SerializeField] private ResrouceTypeListSo resrouceTypeListSo;


   
    private Dictionary<ResourceTypeSo.ResourceType,ResrouceManagerUi_Single> resrouceTypeSingleDictionary;


    private void Awake()
    {
        template.gameObject.SetActive(false);
    }

    private void Start()
    {
        ResrouceManager.Instance.OnResouceAmountChanged += ResourceManager_OnResouceAmountChanged;
        SetUp();
        UpdateAmounts();
    }

    private void ResourceManager_OnResouceAmountChanged(object sender, System.EventArgs e)
    {
        UpdateAmounts();
    }

    private void SetUp()
    {
        foreach(Transform child in container)
        {
            if(child == template)
            {
                continue;

            }
            Destroy(child.gameObject);
        }

        resrouceTypeSingleDictionary = new Dictionary<ResourceTypeSo.ResourceType, ResrouceManagerUi_Single> ();

        foreach (ResourceTypeSo resrouceTypeSo in resrouceTypeListSo.resrouceTySoList)
        {
            Transform resourceTransform = Instantiate(template, container);
            resourceTransform.gameObject.SetActive (true);
            ResrouceManagerUi_Single resrouceManagerUi_Single = resourceTransform.GetComponent<ResrouceManagerUi_Single>();
            resrouceManagerUi_Single.SetUp(resrouceTypeSo);


            resrouceTypeSingleDictionary[resrouceTypeSo.resourcetype] = resrouceManagerUi_Single;
        }
    }

    private void UpdateAmounts()
    {
        foreach(ResourceTypeSo resrouceTypeSo in resrouceTypeListSo.resrouceTySoList)
        {
            resrouceTypeSingleDictionary[resrouceTypeSo.resourcetype].
                UpdateAmount(ResrouceManager.Instance.GetResourceAmount(resrouceTypeSo.resourcetype ));
        }
    }

}
