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
    }
    public override string GetTooltipInfo()
    {
        return $"<b>Điện trở</b>\n" +
               $"R: {resistance:F1} Ω\n" +
               $"U: {_lastVoltageDrop.Magnitude:F2} V\n" +
               $"I: {_lastCurrent.Magnitude:F2} A";
    }
}