using UnityEngine;

public class DebugPinState : MonoBehaviour
{
    public Grabbable grabbable;
    public CircuitComponent component;
    public Connector connectorA;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();
        component = GetComponent<CircuitComponent>();
        if (component != null) connectorA = component.ConnectorA;
    }

    void OnGUI()
    {
        if (grabbable == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z < 0) return;

        string status = $"State: {grabbable.CurrentState}\n";
        
        if (connectorA != null)
        {
            var col = connectorA.GetComponent<Collider>();
            status += $"Conn A Collider: {(col != null ? col.enabled : "NULL")}\n";
            status += $"Conn A Wiring: {connectorA.IsInteractableForWiring}";
        }

        GUI.Box(new Rect(screenPos.x, Screen.height - screenPos.y, 150, 60), status);
    }
}