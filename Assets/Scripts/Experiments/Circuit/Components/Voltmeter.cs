using TMPro;
using UnityEngine;
using System.Numerics;

public class Voltmeter : CircuitComponent
{
    [SerializeField] private TextMeshProUGUI displayValText;

    protected override void Awake()
    {
        base.Awake();
        Impedance = new Complex(1e10, 0); // 100 MegaOhm
    }

    public override void UpdateState(Complex voltageDrop, Complex current)
    {
        base.UpdateState(voltageDrop, current);
        
        if (displayValText != null)
        {
            double val = voltageDrop.Magnitude;
            displayValText.text = $"{val:F2} V";
        }
    }

    public override IInfoDisplayable.TooltipInfo GetTooltipInfo()
    {
        string content =
            $"<color=#FFA500>U: {_lastVoltageDrop.Magnitude:F2} V</color>";

        return new IInfoDisplayable.TooltipInfo("VÔN KẾ", content);
    }
}