using UnityEngine;

public class PendulumVisualizer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform wireModel;
    [SerializeField] private float wireThickness = 0.02f;
    
    private Transform _pivotPoint;
    private Transform _bobConnectorPoint;

    private void Awake()
    {
        if (wireModel != null)
        {
            var col = wireModel.GetComponent<Collider>();
            if (col != null) Destroy(col); 
            
            var renderer = wireModel.GetComponent<Renderer>();
            if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            wireModel.gameObject.SetActive(false);
        }
    }

    public void StartVisualizing(Transform pivot, Transform bobConnector)
    {
        _pivotPoint = pivot;
        _bobConnectorPoint = bobConnector;
        
        if (wireModel != null) wireModel.gameObject.SetActive(true);
        this.enabled = true;
    }

    public void StopVisualizing()
    {
        if (wireModel != null) wireModel.gameObject.SetActive(false);
        this.enabled = false;
        _pivotPoint = null;
        _bobConnectorPoint = null;
    }

    void LateUpdate()
    {
        if (_pivotPoint == null || _bobConnectorPoint == null || wireModel == null) return;

        Vector3 start = _pivotPoint.position;
        Vector3 end = _bobConnectorPoint.position;
        float distance = Vector3.Distance(start, end);

        wireModel.position = (start + end) / 2f;

        wireModel.up = start - end;

        wireModel.localScale = new Vector3(wireThickness, distance / 2f, wireThickness);
    }
}