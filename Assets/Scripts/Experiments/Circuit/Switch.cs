using System;
using UnityEngine;

public class Switch : CircuitComponent, IClickable, IInteractable
{

    [Header("Trạng thái Công tắc")]
    [SerializeField]
    private bool startsOpen = true;

    [Header("Phản hồi Trực quan")]
    [SerializeField]
    private Transform switchVisual; // Đối tượng 3D sẽ xoay để minh họa
    [SerializeField]
    private Vector3 openRotation = new Vector3(0, 0, 45);
    [SerializeField]
    private Vector3 closedRotation = new Vector3(0, 0, -45);

    private bool _isOpen;
    private Renderer _renderer;
    private Color _originalColor;
    #region IClickable Implementation
    public Grabbable AssociatedGrabbable { get; private set; }

    public void OnClick()
    {
        _isOpen = !_isOpen;
        Debug.Log($"Công tắc được click. Trạng thái mới: {(_isOpen ? "Mở" : "Đóng")}");
        UpdateSwitchStateAndNotify();
    }
    #endregion
    protected override void Awake()
    {
        base.Awake();
        _isOpen = startsOpen;
        AssociatedGrabbable = GetComponent<Grabbable>();
        _renderer = switchVisual.GetComponent<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;

        UpdateSwitchStateAndNotify(false);
    }

    private void UpdateSwitchStateAndNotify(bool notifyCircuitManager = true)
    {
        if (_isOpen)
        {
            // Mạch hở, trở kháng vô cùng lớn
            Impedance = new System.Numerics.Complex(double.PositiveInfinity, 0);
            if (switchVisual != null)
                switchVisual.localEulerAngles = openRotation;
        }
        else
        {
            // Mạch kín, trở kháng bằng 0 (lý tưởng)
            Impedance = System.Numerics.Complex.Zero;
            if (switchVisual != null)
                switchVisual.localEulerAngles = closedRotation;
        }

        // Thông báo cho manager rằng mạch đã thay đổi
        if (notifyCircuitManager && CircuitManager.Instance != null)
        {
            CircuitManager.Instance.RecalculateCircuit();
        }
    }

    /// <summary>
    /// Trạng thái của công tắc không phụ thuộc vào dòng điện chạy qua nó.
    /// </summary>
    public override void UpdateState(System.Numerics.Complex current)
    {
    }

    // --- Triển khai IInteractable ---

    public void OnHoverEnter()
    {
        if (_renderer != null) _renderer.material.color = Color.yellow;
    }

    public void OnHoverExit()
    {
        if (_renderer != null) _renderer.material.color = _originalColor;
    }

    public void OnSelectStart()
    {
    }

    public void OnSelectEnd()
    {
    }
}