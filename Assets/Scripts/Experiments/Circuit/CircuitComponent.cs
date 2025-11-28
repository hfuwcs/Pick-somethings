using UnityEngine;
using System.Numerics;
using System.Collections.Generic;

[RequireComponent(typeof(Grabbable))] //Lưu ý: Nhớ để ý Grabbable
public abstract class CircuitComponent : MonoBehaviour, IMultiPointSnappable, IInfoDisplayable
{
    [Header("Cấu hình Kết nối")]
    [Tooltip("Connector đại diện cho điểm kết nối đầu tiên.")]
    [SerializeField] private Connector connectorA;

    [Tooltip("Connector đại diện cho điểm kết nối thứ hai.")]
    [SerializeField] private Connector connectorB;

    public Connector ConnectorA => connectorA;
    public Connector ConnectorB => connectorB;
    protected Complex _lastVoltageDrop;
    protected Complex _lastCurrent;

    /// <summary>
    /// Trở kháng phức (Z) của linh kiện.
    /// Đối với điện trở thuần, phần ảo sẽ bằng 0.
    /// Z = R + j(X_L - X_C)
    /// </summary>
    public Complex Impedance { get; protected set; } = Complex.Zero;

    /// <summary>
    /// Nguồn hiệu điện thế phức (V) mà linh kiện này cung cấp.
    /// </summary>
    public Complex VoltageSource { get; protected set; } = Complex.Zero;

    protected Grabbable GrabbableComponent { get; private set; }
    private readonly Dictionary<Connector, Joint> _connectorJoints = new Dictionary<Connector, Joint>();
    protected virtual void Awake()
    {
        GrabbableComponent = GetComponent<Grabbable>();

        if (connectorA == null || connectorB == null)
        {
            Debug.LogError($"Linh kiện '{gameObject.name}' chưa được gán đủ 2 Connector trong Inspector.", this);
        }
    }

    protected virtual void OnEnable()
    {
        if (GrabbableComponent != null)
        {
            GrabbableComponent.OnStateChanged += HandleGrabbableStateChanged;
        }

        HandleGrabbableStateChanged(GrabbableComponent != null ? GrabbableComponent.CurrentState : GrabbableState.Idle);
    }

    protected virtual void OnDisable()
    {
        if (GrabbableComponent != null)
        {
            GrabbableComponent.OnStateChanged -= HandleGrabbableStateChanged;
        }
    }

    private void HandleGrabbableStateChanged(GrabbableState newState)
    {
        bool allowWiring = (newState == GrabbableState.Snapped || newState == GrabbableState.Anchored);

        if (connectorA != null) connectorA.SetInteractableState(allowWiring);
        if (connectorB != null) connectorB.SetInteractableState(allowWiring);
    }
    public virtual void UpdateState(Complex voltageDrop, Complex current)
    {
        _lastVoltageDrop = voltageDrop;
        _lastCurrent = current;
    }
    public virtual IInfoDisplayable.TooltipInfo GetTooltipInfo()
    {
        return new IInfoDisplayable.TooltipInfo(
            gameObject.name,
            $"U: {_lastVoltageDrop.Magnitude:F2} V\n" +
            $"I: {_lastCurrent.Magnitude:F2} A"
        );
    }
    #region IMultiPointSnappable Implementation

    public void SnapPoint(Connector connector, SnapZone snapZone)
    {
        if (_connectorJoints.ContainsKey(connector)) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Rigidbody connectedBody = snapZone.GetComponent<Rigidbody>();
        if (connectedBody == null)
        {
            Debug.LogError($"SnapZone '{snapZone.name}' không có Rigidbody để tạo Joint.", snapZone);
            return;
        }

        ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = connectedBody;
        joint.anchor = transform.InverseTransformPoint(connector.transform.position);
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        _connectorJoints.Add(connector, joint);
        HandleGrabbableStateChanged(GrabbableState.Anchored); 
    }

    public void UnsnapPoint(Connector connector)
    {
        if (_connectorJoints.TryGetValue(connector, out Joint joint))
        {
            Destroy(joint);
            _connectorJoints.Remove(connector);
            Debug.Log($"[MultiPoint] Đã hủy Joint cho {connector.name}.");
        }

        if (_connectorJoints.Count == 0)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            HandleGrabbableStateChanged(GrabbableState.Idle);
        }
    }

    #endregion
}