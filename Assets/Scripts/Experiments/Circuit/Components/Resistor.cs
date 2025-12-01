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
        Impedance = new Complex(resistance, 0);
        VoltageSource = Complex.Zero;
    }
    public override void UpdateState(Complex voltageDrop, Complex current)
    {
        base.UpdateState(voltageDrop, current);
    }
    public override IInfoDisplayable.TooltipInfo GetTooltipInfo()
    {
        string content =
            $"<color=#FFD700>R: {resistance:F1} Ω</color>\n" +
            $"<color=#FFA500>U: {_lastVoltageDrop.Magnitude:F2} V</color>\n" +
            $"<color=#00FFFF>I: {_lastCurrent.Magnitude:F2} A</color>";

        return new IInfoDisplayable.TooltipInfo("ĐIỆN TRỞ", content);
    }
}