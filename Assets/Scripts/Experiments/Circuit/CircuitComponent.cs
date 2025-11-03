using UnityEngine;
using System.Numerics;

/// <summary>
/// Lớp cơ sở trừu tượng cho tất cả các thành phần trong một mạch điện.
/// Định nghĩa các thuộc tính vật lý cốt lõi sử dụng số phức cho cả mạch DC và AC.
/// </summary>
[RequireComponent(typeof(Grabbable))]
public abstract class CircuitComponent : MonoBehaviour
{
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

    protected virtual void Awake()
    {
        GrabbableComponent = GetComponent<Grabbable>();
    }

    /// <summary>
    /// Được gọi bởi CircuitManager để cập nhật trạng thái của linh kiện
    /// dựa trên dòng điện chạy qua nó.
    /// </summary>
    /// <param name="current">Dòng điện phức chạy qua linh kiện.</param>
    public abstract void UpdateState(Complex current);
}