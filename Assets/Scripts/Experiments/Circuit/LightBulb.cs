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

    private Material _bulbMaterialInstance;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    protected override void Awake()
    {
        base.Awake();
        Impedance = new Complex(resistance, 0);
        VoltageSource = Complex.Zero;

        if (bulbRenderer != null)
        {
            // Tạo một instance của material để tránh thay đổi asset gốc.
            _bulbMaterialInstance = bulbRenderer.material;
        }
        else
        {
            Debug.LogError("Chưa gán Renderer cho bóng đèn.", this);
        }

        // Tắt đèn khi bắt đầu
        UpdateState(Complex.Zero);
    }

    public override void UpdateState(Complex current)
    {
        if (pointLight == null || _bulbMaterialInstance == null) return;

        double currentMagnitude = current.Magnitude;
        float intensity = 0f;

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

        // Cập nhật ánh sáng và vật liệu
        pointLight.intensity = intensity * 2.0f; // Khuếch đại để nhìn rõ hơn
        _bulbMaterialInstance.SetColor(EmissionColorID, emissionColor * intensity);
    }
}