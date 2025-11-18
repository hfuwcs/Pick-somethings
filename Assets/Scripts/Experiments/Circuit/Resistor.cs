using UnityEngine;
using System.Numerics;

//Linh kiện: Điện trở.
public class Resistor : CircuitComponent
{
    [Header("Thông số Điện trở")]
    [SerializeField]
    [Tooltip("Giá trị điện trở (Ohm).")]
    private double resistance = 10.0;

    protected override void Awake()
    {
        base.Awake();
        // Điện trở thuần có trở kháng là một số thực.
        Impedance = new Complex(resistance, 0);
        VoltageSource = Complex.Zero;
    }

    /// <summary>
    /// Điện trở có thể nóng lên, nhưng trong mô phỏng này ta bỏ qua.
    /// </summary>
    public override void UpdateState(Complex voltageDrop,Complex current)
    {
        // No-op: Có thể mở rộng để mô phỏng nhiệt độ tỏa ra.
        // double power = Complex.Abs(current * current) * Impedance.Real;
        // Debug.Log($"Công suất tỏa nhiệt trên điện trở {gameObject.name}: {power} W");
    }
}