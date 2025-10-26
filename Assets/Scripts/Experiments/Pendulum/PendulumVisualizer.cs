using UnityEngine;

/// <summary>
/// Chịu trách nhiệm hiển thị trực quan sợi dây của con lắc bằng Line Renderer.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PendulumVisualizer : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private Transform _bobModelTransform; // Transform của quả nặng để vẽ đến

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        // Cấu hình Line Renderer cơ bản
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = 0.05f;
        _lineRenderer.endWidth = 0.05f;
        // Đảm bảo Line Renderer sử dụng tọa độ thế giới
        _lineRenderer.useWorldSpace = true;

        // Tắt visualizer ban đầu
        this.enabled = false;
        _lineRenderer.enabled = false;
    }

    /// <summary>
    /// Bắt đầu hiển thị sợi dây.
    /// </summary>
    /// <param name="bobModel">Transform của model quả nặng.</param>
    public void StartVisualizing(Transform bobModel)
    {
        _bobModelTransform = bobModel;
        this.enabled = true;
        _lineRenderer.enabled = true;
    }

    /// <summary>
    /// Dừng hiển thị sợi dây.
    /// </summary>
    public void StopVisualizing()
    {
        _bobModelTransform = null;
        this.enabled = false;
        _lineRenderer.enabled = false;
    }

    // Sử dụng LateUpdate để đảm bảo sợi dây được vẽ sau khi tất cả các
    // tính toán vật lý trong FixedUpdate và Update đã hoàn tất.
    void LateUpdate()
    {
        if (_bobModelTransform == null) return;

        // Cập nhật vị trí 2 đầu của sợi dây
        _lineRenderer.SetPosition(0, transform.position); // Điểm A: Pivot
        _lineRenderer.SetPosition(1, _bobModelTransform.position); // Điểm B: Bob
    }
}