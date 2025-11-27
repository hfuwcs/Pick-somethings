using UnityEngine;

public class Wire : MonoBehaviour, IInteractable
{
    [Header("Visual Settings")]
    [SerializeField] private Transform wireModel;
    [SerializeField] private float thickness = 0.02f;

    public Connector StartConnector { get; private set; }
    public Connector EndConnector { get; private set; }

    private void Awake()
    {
        if (wireModel == null)
        {
            if (transform.childCount > 0) wireModel = transform.GetChild(0);
        }
    }

    public void Initialize(Connector start)
    {
        StartConnector = start;
        transform.position = start.transform.position;
        StartConnector.AddWire(this);

        UpdateVisuals(start.transform.position);
    }

    public void UpdateEndPosition(Vector3 worldEndPos)
    {
        UpdateVisuals(worldEndPos);
    }

    public void Complete(Connector end)
    {
        EndConnector = end;
        UpdateVisuals(end.transform.position);

        gameObject.name = $"Wire_{StartConnector.ParentComponent.name}_to_{EndConnector.ParentComponent.name}";
        EndConnector.AddWire(this);
    }

    private void UpdateVisuals(Vector3 endPos)
    {
        if (wireModel == null) return;

        Vector3 startPos = StartConnector.transform.position;
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;

        if (distance < 0.001f) return;

        wireModel.position = (startPos + endPos) / 2f;

        wireModel.up = direction;


        wireModel.localScale = new Vector3(thickness, distance / 2f, thickness);
    }

    public void DisconnectAndDestroy()
    {
        if (StartConnector != null) StartConnector.RemoveWire(this);
        if (EndConnector != null) EndConnector.RemoveWire(this);

        if (CircuitManager.Instance != null) CircuitManager.Instance.RecalculateCircuit();

        Destroy(gameObject);
    }
    public void SetColor(Material mat)
    {
        if (wireModel != null)
        {
            var renderer = wireModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = mat;
            }
        }
    }
    public void OnHoverEnter() { }
    public void OnHoverExit() { }
    public void OnSelectStart() { }
    public void OnSelectEnd() { }
}