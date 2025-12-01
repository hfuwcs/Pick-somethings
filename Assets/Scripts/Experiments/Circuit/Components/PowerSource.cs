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
        VoltageSource = new Complex(voltage, 0);
    }

    /// <summary>
    /// Nguồn điện không thay đổi trạng thái dựa trên dòng điện.
    /// </summary>
    public override void UpdateState(Complex voltageDrop, Complex current)
    {
        base.UpdateState(voltageDrop, current);
    }

    public override IInfoDisplayable.TooltipInfo GetTooltipInfo()
    {
        string content =
            $"<color=#FFD700>E: {voltage:F1} V</color>\n" +
            $"<color=#00FFFF>I: {_lastCurrent.Magnitude:F2} A</color>";

        return new IInfoDisplayable.TooltipInfo("NGUỒN ĐIỆN", content);
    }
}