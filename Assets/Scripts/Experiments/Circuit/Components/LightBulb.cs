using UnityEngine;
using System.Numerics;

//Rõ ràng là cái bóng đèn chứ gì nữa?
public class LightBulb : CircuitComponent
{
    [Header("Thông số Bóng đèn")]
    [SerializeField]
    [Tooltip("Điện trở của bóng đèn khi hoạt động (Ohm).")]
    private double resistance = 5.0;

    [Header("Phản hồi Trực quan")]
    [SerializeField]
    private Light pointLight;

    [SerializeField]
    private Renderer bulbRenderer;

    [SerializeField]
    [ColorUsage(true, true)]
    private Color emissionColor = Color.yellow;

    [SerializeField]
    [Tooltip("Cường độ dòng điện tối thiểu để đèn bắt đầu phát sáng (A).")]
    private double minCurrentToGlow = 0.1;

    [SerializeField]
    [Tooltip("Cường độ dòng điện để đèn đạt độ sáng tối đa (A).")]
    private double maxCurrentForMaxBrightness = 2.0;
    private bool _hasNotified = false;
    private Material _bulbMaterialInstance;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    protected override void Awake()
    {
        base.Awake();
        Impedance = new Complex(resistance, 0);
        VoltageSource = Complex.Zero;

        if (bulbRenderer != null)
        {
            _bulbMaterialInstance = bulbRenderer.material;
        }
        else
        {
            Debug.LogError("Chưa gán Renderer cho bóng đèn.", this);
        }

        // Tắt đèn khi bắt đầu
        UpdateState(Complex.Zero, Complex.Zero);
    }

    public override void UpdateState(Complex voltageDrop, Complex current)
    {
        if (pointLight == null || _bulbMaterialInstance == null) return;

        double currentMagnitude = current.Magnitude;
        float intensity = Mathf.InverseLerp(0, 2.0f, (float)current.Magnitude);

        if (currentMagnitude >= minCurrentToGlow)
        {
            // Tính toán cường độ sáng dựa trên
            // tỷ lệ tuyến tính giữa min và max current.
            intensity = Mathf.InverseLerp(
                (float)minCurrentToGlow,
                (float)maxCurrentForMaxBrightness,
                (float)currentMagnitude
            );
        }

        pointLight.intensity = intensity * 100.0f;

        float hdrIntensity = intensity * 45.0f;
        Color finalColor = emissionColor * Mathf.LinearToGammaSpace(hdrIntensity);
        _bulbMaterialInstance.SetColor(EmissionColorID, finalColor);
        if (intensity > 0.01f)
            _bulbMaterialInstance.EnableKeyword("_EMISSION");
        else
            _bulbMaterialInstance.DisableKeyword("_EMISSION");

        if (current.Magnitude > minCurrentToGlow && !_hasNotified)
        {
            _hasNotified = true;
            if (ExperimentNotification.Instance != null)
            {
                ExperimentNotification.Instance.Show(
                    "Thành công!",
                    "Mạch điện đã hoạt động. Đèn đang sáng.",
                    ExperimentNotification.Type.Success
                );
            }
        }
        else if (current.Magnitude <= minCurrentToGlow)
        {
            _hasNotified = false;
        }
    }
}