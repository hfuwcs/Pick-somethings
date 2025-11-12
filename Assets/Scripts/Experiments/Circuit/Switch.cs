using System;
using UnityEngine;

public class Switch : CircuitComponent, IInteractable
{
    public event Action OnSwitchClicked;

    [Header("Trạng thái Công tắc")]
    [SerializeField]
    private bool startsOpen = true;

    [Header("Phản hồi Trực quan")]
    [SerializeField]
    private Transform switchObjectVisual; // Đối tượng 3D sẽ xoay để minh họa
    [SerializeField]
    private Vector3 openRotation = new Vector3(0, 0, 45);
    [SerializeField]
    private Vector3 closedRotation = new Vector3(0, 0, -45);

    private bool _isOpen;
    private Renderer _renderer;
    private Color _originalColor;

    protected override void Awake()
    {
        base.Awake();
        _isOpen = startsOpen;
        _renderer = switchObjectVisual.GetComponent<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;

        UpdateSwitchState();
    }

    private void UpdateSwitchState()
    {
        if (_isOpen)
        {
            Impedance = new System.Numerics.Complex(double.PositiveInfinity, 0);
            if (switchObjectVisual != null)
                switchObjectVisual.localEulerAngles = openRotation;
        }
        else
        {
            Impedance = System.Numerics.Complex.Zero;
            if (switchObjectVisual != null)
                switchObjectVisual.localEulerAngles = closedRotation;
        }

        if (CircuitManager.Instance != null)
        {
            CircuitManager.Instance.RecalculateCircuit();
        }
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
        _isOpen = !_isOpen;
        Debug.Log($"Công tắc được bật. Trạng thái mới: {(_isOpen ? "Mở" : "Đóng")}");
        UpdateSwitchState();
        OnSwitchClicked?.Invoke();
    }

    public void OnSelectEnd()
    {
    }

    public override void UpdateState(System.Numerics.Complex current)
    {
    }
}