using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Wire : MonoBehaviour, IInteractable
{
    private LineRenderer _lineRenderer;
    
    public Connector StartConnector { get; private set; }
    public Connector EndConnector { get; private set; }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = 0.01f;
        _lineRenderer.endWidth = 0.01f;
        _lineRenderer.useWorldSpace = true;
    }

    /// <summary>
    /// Khởi tạo dây khi bắt đầu kéo.
    /// </summary>
    /// <param name="start">Connector bắt đầu.</param>
    public void Initialize(Connector start)
    {
        StartConnector = start;
        transform.position = start.transform.position;
        _lineRenderer.SetPosition(0, start.transform.position);
        _lineRenderer.SetPosition(1, start.transform.position);
        StartConnector.AddWire(this);
    }

    /// <summary>
    /// Cập nhật vị trí của đầu dây đang được kéo theo con trỏ.
    /// </summary>
    public void UpdateEndPosition(Vector3 worldPosition)
    {
        _lineRenderer.SetPosition(1, worldPosition);
    }
    /// <param name="end">Connector kết thúc.</param>
    public void Complete(Connector end)
    {
        EndConnector = end;
        _lineRenderer.SetPosition(1, end.transform.position);
        gameObject.name = $"Wire_{StartConnector.ParentComponent.name}_to_{EndConnector.ParentComponent.name}";
        EndConnector.AddWire(this);
    }

    /// <summary>
    /// Ngắt kết nối và tự hủy.
    /// </summary>
    public void DisconnectAndDestroy()
    {
        Destroy(gameObject);
    }

    public void OnHoverEnter() { /* Có thể đổi màu dây để highlight */ }
    public void OnHoverExit() { /* Trả lại màu cũ */ }
    public void OnSelectStart() {}
    public void OnSelectEnd() {}
}