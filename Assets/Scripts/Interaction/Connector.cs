using UnityEngine;
public class Connector : MonoBehaviour
{
    [Tooltip("ID định danh loại kết nối. SnapZone sẽ chỉ chấp nhận Connector có cùng ID.")]
    [SerializeField] private string connectionID = "Default";

    private Grabbable _parentGrabbable;
    public CircuitComponent ParentComponent { get; private set; }

    public string ConnectionID => connectionID;
    public Grabbable ParentGrabbable => _parentGrabbable;
    public SnapZone ConnectedZone { get; private set; }
    private void Awake()
    {
        _parentGrabbable = GetComponentInParent<Grabbable>();
        ParentComponent = GetComponentInParent<CircuitComponent>();

        if (_parentGrabbable == null)
        {
            Debug.LogError($"Connector '{name}' không tìm thấy Grabbable cha.", this);
        }
    }
    public void SetConnectedZone(SnapZone zone)
    {
        ConnectedZone = zone;
    }

    public void ClearConnectedZone()
    {
        ConnectedZone = null;
    }
}