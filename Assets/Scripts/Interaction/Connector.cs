// File: Assets/_Project/Scripts/Interaction/Connector.cs
using UnityEngine;
public class Connector : MonoBehaviour
{
    [Tooltip("ID định danh loại kết nối. SnapZone sẽ chỉ chấp nhận Connector có cùng ID.")]
    [SerializeField] private string connectionID = "Default";

    private Grabbable _parentGrabbable;

    public string ConnectionID => connectionID;
    public Grabbable ParentGrabbable => _parentGrabbable;

    private void Awake()
    {
        _parentGrabbable = GetComponentInParent<Grabbable>();

        if (_parentGrabbable == null)
        {
            Debug.LogError($"Connector trên '{gameObject.name}' không thể tìm thấy component Grabbable ở đối tượng cha hoặc chính nó.", this);
        }
    }
}