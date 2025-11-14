using UnityEngine;
using System.Numerics;
using System.Collections.Generic;

[RequireComponent(typeof(Grabbable))] //Lưu ý: Nhớ để ý Grabbable
public abstract class CircuitComponent : MonoBehaviour
{
    [Header("Cấu hình Kết nối")]
    [Tooltip("Connector đại diện cho điểm kết nối đầu tiên.")]
    [SerializeField] private Connector connectorA;

    [Tooltip("Connector đại diện cho điểm kết nối thứ hai.")]
    [SerializeField] private Connector connectorB;

    // Public accessors để các hệ thống khác có thể đọc
    public Connector ConnectorA => connectorA;
    public Connector ConnectorB => connectorB;

    /// <summary>
    /// Trở kháng phức (Z) của linh kiện.
    /// Đối với điện trở thuần, phần ảo sẽ bằng 0.
    /// Z = R + j(X_L - X_C)
    /// </summary>
    public Complex Impedance { get; protected set; } = Complex.Zero;

    /// <summary>
    /// Nguồn hiệu điện thế phức (V) mà linh kiện này cung cấp.
    /// Hầu hết các linh kiện sẽ có giá trị này bằng 0, trừ nguồn điện.
    /// </summary>
    public Complex VoltageSource { get; protected set; } = Complex.Zero;

    /// <summary>
    /// Tham chiếu đến Grabbable component để quản lý trạng thái vật lý.
    /// </summary>
    protected Grabbable GrabbableComponent { get; private set; }
    private readonly Dictionary<Connector, Joint> _connectorJoints = new Dictionary<Connector, Joint>();
    protected virtual void Awake()
    {
        GrabbableComponent = GetComponent<Grabbable>();

        if (connectorA == null || connectorB == null)
        {
            Debug.LogError($"Linh kiện '{gameObject.name}' chưa được gán đủ 2 Connector trong Inspector.", this);
        }
    }

    /// <summary>
    /// Được gọi bởi CircuitManager để cập nhật trạng thái của linh kiện
    /// dựa trên dòng điện chạy qua nó.
    /// </summary>
    /// <param name="current">Dòng điện phức chạy qua linh kiện.</param>
    public abstract void UpdateState(Complex current);
}