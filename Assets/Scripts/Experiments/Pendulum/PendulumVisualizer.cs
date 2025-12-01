using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PendulumVisualizer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float wireThickness = 0.01f;
    
    [Tooltip("Số lần lặp lại của Texture trên mỗi mét dây (Tiling)")]
    [SerializeField] private float textureTilingFactor = 10f; 

    private Transform _pivotPoint;
    private Transform _bobConnectorPoint;
    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        
        _lineRenderer.startWidth = wireThickness;
        _lineRenderer.endWidth = wireThickness;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.textureMode = LineTextureMode.Tile;
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }

    public void StartVisualizing(Transform pivot, Transform bobConnector)
    {
        _pivotPoint = pivot;
        _bobConnectorPoint = bobConnector;
        
        _lineRenderer.enabled = true;
        this.enabled = true;
    }

    public void StopVisualizing()
    {
        _lineRenderer.enabled = false;
        this.enabled = false;
        _pivotPoint = null;
        _bobConnectorPoint = null;
    }

    void LateUpdate()
    {
        if (_pivotPoint == null || _bobConnectorPoint == null) return;

        Vector3 start = _pivotPoint.position;
        Vector3 end = _bobConnectorPoint.position;
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);
        
        float distance = Vector3.Distance(start, end);
        Material mat = _lineRenderer.material;
        if (mat != null)
        {
            mat.mainTextureScale = new Vector2(distance * textureTilingFactor, 1);
        }
    }
}