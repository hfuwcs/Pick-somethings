using UnityEngine;
using System.Numerics;

// Linh kiện: Nguồn điện.
public class PowerSource : CircuitComponent, IMultiPointSnappable
{
    [Header("Thông số Nguồn điện")]
    [SerializeField]
    [Tooltip("Hiệu điện thế của nguồn (V).")]
    private double voltage = 9.0;

    protected override void Awake()
    {
        base.Awake();
        // Nguồn điện DC lý tưởng có trở kháng trong bằng 0.
        Impedance = Complex.Zero;
        // Hiệu điện thế là một số thực (phần ảo bằng 0).
        VoltageSource = new Complex(voltage, 0);
    }

    /// <summary>
    /// Nguồn điện không thay đổi trạng thái dựa trên dòng điện.
    /// </summary>
    public override void UpdateState(Complex current)
    {
        // No-op: Trạng thái của nguồn điện không phụ thuộc vào dòng điện trong mạch.
    }
}