using System;
using UnityEngine;

public class Switch : CircuitComponent, IClickable
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
    public bool IsClickable 
    { 
        get 
        {
            bool result = GrabbableComponent.CurrentState == GrabbableState.Anchored;
            Debug.Log($"[SWITCH DEBUG] IsClickable: CurrentState = {GrabbableComponent.CurrentState}, Result = {result}");
            return result;
        }
    }

    public void OnClick()
    {
        Debug.Log($"[SWITCH DEBUG] OnClick - IsClickable: {IsClickable}, CurrentState: {GrabbableComponent.CurrentState}");
        
        if (!IsClickable)
        {
            Debug.LogWarning($"[SWITCH DEBUG] Click bị từ chối. CurrentState: {GrabbableComponent.CurrentState}");
            return;
        }

        _isOpen = !_isOpen;
        Debug.Log($"[SWITCH DEBUG] Switch toggled: {(_isOpen ? "Mở" : "Đóng")}");
        UpdateSwitchStateAndNotify(true);
    }
    #endregion
    protected override void Awake()
    {
        base.Awake();
        _isOpen = startsOpen;
        _renderer = switchVisual.GetComponent<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;

        UpdateSwitchStateAndNotify(false);
    }

    private void UpdateSwitchStateAndNotify(bool notifyCircuitManager = true)
    {
        if (_isOpen)
        {
            Impedance = new System.Numerics.Complex(1e9, 0);
            if (switchVisual != null)
                switchVisual.localEulerAngles = openRotation;
        }
        else
        {
            Impedance = new System.Numerics.Complex(0.001, 0); 
            if (switchVisual != null)
                switchVisual.localEulerAngles = closedRotation;
        }

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
}