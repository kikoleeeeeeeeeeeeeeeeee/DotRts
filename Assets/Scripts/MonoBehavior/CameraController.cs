using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float fieldOfViewMin;
    [SerializeField] private float fieldOfViewMax;

    [Header("Map Bounds")]
    [Tooltip("相机在水平方向离地图边缘的最小距离，避免镜头跑出可玩区域")]
    [SerializeField] private float mapEdgeMargin = 5f;

    private float targetFieldOfView;

    private void Awake()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
        }

        if (cinemachineCamera != null)
        {
            targetFieldOfView = cinemachineCamera.Lens.FieldOfView;
        }
    }

    private void Update()
    {
        Vector3 moveDir = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDir.z = +1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDir.z = -1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDir.x = -1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDir.x = +1f;
        }

        Transform cameraTransform = Camera.main.transform;
        moveDir = cameraTransform.forward *moveDir.z+cameraTransform.right *moveDir.x;
        moveDir.y = 0f;
        moveDir.Normalize();

        float moveSpeed = 30f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        ClampPositionToMapBounds();


        float rotationAmount = 0f;
        if (Input.GetKey(KeyCode.Q))
        {
            rotationAmount = +1f;
        }

        if (Input.GetKey(KeyCode.E))
        {
            rotationAmount =-1f;
        }
        float rotationSpeed = 200f;
        transform.eulerAngles += new Vector3(0, rotationAmount * rotationSpeed * Time.deltaTime, 0);

        float zommAmount = 4f;

        if (Input.mouseScrollDelta.y > 0)
        {
            targetFieldOfView -=zommAmount; 
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            targetFieldOfView += zommAmount;
        }

        targetFieldOfView = Mathf.Clamp(targetFieldOfView,fieldOfViewMin,fieldOfViewMax);

        float zoomSpeed = 10f;
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, targetFieldOfView, zoomSpeed*Time.deltaTime);
        }
    }

    /// <summary>相机水平位置钳制在地图边界内，避免镜头推到地图外只剩空背景。</summary>
    private void ClampPositionToMapBounds()
    {
        if (MapManager.Instance == null) return;

        Bounds bounds = MapManager.Instance.GetMapBounds();
        Vector3 position = transform.position;

        float minX = bounds.min.x + mapEdgeMargin;
        float maxX = bounds.max.x - mapEdgeMargin;
        float minZ = bounds.min.z + mapEdgeMargin;
        float maxZ = bounds.max.z - mapEdgeMargin;

        // 地图小于两倍 margin 时区间会反转，此时直接钉在中心。
        position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : bounds.center.x;
        position.z = minZ <= maxZ ? Mathf.Clamp(position.z, minZ, maxZ) : bounds.center.z;

        transform.position = position;
    }
}
