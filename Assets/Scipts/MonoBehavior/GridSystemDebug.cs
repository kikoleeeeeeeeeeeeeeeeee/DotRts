using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GridSystemDebug : MonoBehaviour
{
    public static GridSystemDebug Instance {  get; private set; }

    
    [SerializeField] private Transform debugprefab;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite arrowSprite;

    private bool isInit;

    private GridSystemDebugsingle[,] gridSystemDebugsingleArray;


    private void Awake()
    {
        Instance = this;
    }
    public  void InitializeGrid(GridSystem.GridSystemData gridSystemData)
    {
        if (isInit)
        {
            return; 
        }
        isInit = true;

        gridSystemDebugsingleArray = new GridSystemDebugsingle[gridSystemData.width, gridSystemData.height];

        for(int x = 0; x < gridSystemData.width; x++)
        {
            for(int y = 0; y < gridSystemData.height; y++)
            {
                Transform debugTransform = Instantiate(debugprefab);
                GridSystemDebugsingle gridSystemDebugsingle = debugTransform.GetComponent<GridSystemDebugsingle>();
                gridSystemDebugsingle.setup(x, y, gridSystemData.gridNodeSize);

                gridSystemDebugsingleArray[x,y] = gridSystemDebugsingle;
            }
        }
    }
    public void UpdateGrid(GridSystem.GridSystemData gridSystemData )
    {
        for (int x = 0; x < gridSystemData.width; x++)
        {
            for (int y = 0; y < gridSystemData.height; y++)
            {
                GridSystemDebugsingle gridSystemDebugsingle = gridSystemDebugsingleArray[x, y];

                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                int index = GridSystem.CalculateIndex(x, y, gridSystemData.width);

                int gridIndex = gridSystemData.nextGridIndex-1;

                if(gridIndex < 0)
                {
                    gridIndex = 0;
                }

                Entity gridNodeEntity = gridSystemData.gridMapArray[gridIndex].gridEntityArray[index];

                GridSystem.GridNode gridNode =  entityManager.GetComponentData<GridSystem.GridNode>(gridNodeEntity);

                if(gridNode.cost == 0)
                {
                    gridSystemDebugsingle.SetSprite(circleSprite);
                    gridSystemDebugsingle.SetColor(Color.green);
                }
                else
                {
                    if(gridNode.cost == GridSystem.WALL_COST)
                    {
                        gridSystemDebugsingle.SetSprite(circleSprite);
                        gridSystemDebugsingle.SetColor(Color.black);
                    }
                    else
                    {
                        gridSystemDebugsingle.SetSprite(arrowSprite);
                        gridSystemDebugsingle.SetColor(Color.white);
                        gridSystemDebugsingle.SetSpriteRotation(
                            Quaternion.LookRotation(new float3(gridNode.vector.x, 0, gridNode.vector.y), Vector3.up));
                    }
                    
                }


                //gridSystemDebugsingle.SetColor(gridNode.data == 0 ? Color.white: Color.blue);
            }
        } 
    }
}
 