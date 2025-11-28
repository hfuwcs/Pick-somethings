using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Linq;

public enum WiringState { Idle, Wiring }

public class WiringManager : MonoBehaviour
{
    public static WiringManager Instance { get; private set; }

    [SerializeField]
    [Tooltip("Prefab của đối tượng Wire để tạo ra khi kéo dây.")]
    private GameObject wirePrefab;
    [Header("Wire Materials")]
    [SerializeField] private Material redWireMat;
    [SerializeField] private Material blueWireMat;
    public WiringState CurrentState { get; private set; } = WiringState.Idle;
    public bool IsDrawing => _currentDrawingWire != null;
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

        Material selectedMat = blueWireMat;

        if (startPoint.ParentComponent != null)
        {
            if (startPoint == startPoint.ParentComponent.ConnectorA)
            {
                selectedMat = redWireMat;
            }
        }

        _currentDrawingWire.SetColor(selectedMat);
        _currentDrawingWire.Initialize(startPoint);

        Debug.Log($"Bắt đầu kéo dây từ: {startPoint.name}");
    }

    private void EndWiring(Connector endPoint)
    {
        _currentDrawingWire.Complete(endPoint);
        Debug.Log($"Hoàn thành kéo dây đến: {endPoint.name}");

        OnWireConnected?.Invoke(_startConnector, endPoint);
        if (AudioManager.Instance) AudioManager.Instance.PlayWire();
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
        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Vector3 endPoint = ray.GetPoint(10f);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Connectors")))
        {
            endPoint = hit.point;
        }

        _currentDrawingWire.UpdateEndPosition(endPoint);
    }
    public void RemoveWiresFromConnector(Connector connector)
    {
        if (connector.ConnectedWires.Count == 0) return;

        Debug.Log($"[Wiring] Đang gỡ {connector.ConnectedWires.Count} dây khỏi {connector.name}");


        var wiresToRemove = connector.ConnectedWires.ToList();

        foreach (var wire in wiresToRemove)
        {
            wire.DisconnectAndDestroy();
        }

        // Sau khi xóa hết dây, tính lại mạch điện
        CircuitManager.Instance.RecalculateCircuit();
    }
}