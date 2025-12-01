using TMPro; // Cần package TextMeshPro
using UnityEngine;
using System.Numerics;

public class Ammeter : CircuitComponent
{
    [SerializeField] private TextMeshProUGUI displayValText;

    protected override void Awake()
    {
        base.Awake();
        Impedance = new Complex(0.0001, 0); 
    }

    public override void UpdateState(Complex voltageDrop, Complex current)
    {
        base.UpdateState(voltageDrop, current);
        
        if (displayValText != null)
        {
            double val = current.Magnitude;
            displayValText.text = $"{val:F2} A";
        }
    }

    public override IInfoDisplayable.TooltipInfo GetTooltipInfo()
    {
        string content =
            $"<color=#00FFFF>I: {_lastCurrent.Magnitude:F2} A</color>";

        return new IInfoDisplayable.TooltipInfo("AMPE KẾ", content);
    }
}