using UnityEngine;
using UnityEngine.InputSystem;
using System;

public enum WiringState { Idle, Wiring }

public class WiringManager : MonoBehaviour
{
    public static WiringManager Instance { get; private set; }

    [SerializeField]
    [Tooltip("Prefab của đối tượng Wire để tạo ra khi kéo dây.")]
    private GameObject wirePrefab;

    public WiringState CurrentState { get; private set; } = WiringState.Idle;

    // Sự kiện được phát khi một kết nối dây hoàn tất
    public static event Action<Connector, Connector> OnWireConnected;

    private Wire _currentDrawingWire;
    private Connector _startConnector;
    private Camera _mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (CurrentState == WiringState.Wiring)
        {
            UpdateWireEndpointToCursor();
        }
    }

    /// <summary>
    /// Phương thức trung tâm xử lý click vào connector, được gọi bởi InteractionController.
    /// </summary>
    public void HandleConnectorClick(Connector clickedConnector)
    {
        if (CurrentState == WiringState.Idle)
        {
            StartWiring(clickedConnector);
        }
        else
        {
            // Ngăn việc nối một connector vào chính nó
            if (clickedConnector != _startConnector)
            {
                EndWiring(clickedConnector);
            }
        }
    }

    private void StartWiring(Connector startPoint)
    {
        if (wirePrefab == null)
        {
            Debug.LogError("Chưa gán Wire Prefab cho WiringManager!");
            return;
        }

        CurrentState = WiringState.Wiring;
        _startConnector = startPoint;

        GameObject wireObject = Instantiate(wirePrefab, startPoint.transform.position, Quaternion.identity);
        _currentDrawingWire = wireObject.GetComponent<Wire>();
        _currentDrawingWire.Initialize(startPoint);
        
        Debug.Log($"Bắt đầu kéo dây từ: {startPoint.name}");
    }

    private void EndWiring(Connector endPoint)
    {
        _currentDrawingWire.Complete(endPoint);
        Debug.Log($"Hoàn thành kéo dây đến: {endPoint.name}");
        
        // Phát sự kiện để CircuitManager biết
        OnWireConnected?.Invoke(_startConnector, endPoint);

        ResetWiringState();
    }

    public void CancelWiring()
    {
        if (CurrentState == WiringState.Wiring)
        {
            Debug.Log("Hủy thao tác kéo dây.");
            Destroy(_currentDrawingWire.gameObject);
            ResetWiringState();
        }
    }

    private void ResetWiringState()
    {
        CurrentState = WiringState.Idle;
        _startConnector = null;
        _currentDrawingWire = null;
    }

    private void UpdateWireEndpointToCursor()
    {
        // Raycast ra từ camera để tìm vị trí con trỏ trong không gian 3D
        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Vector3 endPoint = ray.GetPoint(10f); // Mặc định điểm cuối ở xa 10m

        // Ưu tiên snap vào một connector khác nếu có
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Connectors")))
        {
            endPoint = hit.point;
        }

        _currentDrawingWire.UpdateEndPosition(endPoint);
    }
}