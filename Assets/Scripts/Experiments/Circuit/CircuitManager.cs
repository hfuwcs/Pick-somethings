using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

/// <summary>
/// Quản lý trạng thái và tính toán vật lý cho một mạch điện.
/// </summary>
public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance { get; private set; }

    // Danh sách các linh kiện hiện đang được kết nối trong mạch.
    private readonly List<CircuitComponent> _components = new List<CircuitComponent>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        SnapZone.OnComponentSnapped += RegisterComponent;
        SnapZone.OnComponentUnsnapped += UnregisterComponent;
        WiringManager.OnWireConnected += HandleWireConnection;
    }

    private void OnDisable()
    {
        SnapZone.OnComponentSnapped -= RegisterComponent;
        SnapZone.OnComponentUnsnapped -= UnregisterComponent;
        WiringManager.OnWireConnected -= HandleWireConnection;
    }
    private void HandleWireConnection(Connector start, Connector end)
    {
        Debug.Log("[CircuitManager] Nhận được sự kiện kết nối dây. Tính toán lại mạch.");
        RecalculateCircuit();
    }
    private void RegisterComponent(CircuitComponent component)
    {
        if (!_components.Contains(component))
        {
            _components.Add(component);
            Debug.Log($"[CircuitManager] Đã đăng ký linh kiện: {component.name}. Tổng số linh kiện: {_components.Count}");
            RecalculateCircuit();
        }
    }

    private void UnregisterComponent(CircuitComponent component)
    {
        if (_components.Contains(component))
        {
            _components.Remove(component);
            Debug.Log($"[CircuitManager] Đã hủy đăng ký linh kiện: {component.name}. Tổng số linh kiện: {_components.Count}");
            RecalculateCircuit();
        }
    }

    /// <summary>
    /// Hàm tính toán cốt lõi. Được gọi mỗi khi có sự thay đổi trong mạch.
    /// Phiên bản này giả định một mạch nối tiếp đơn giản.
    /// </summary>
    public void RecalculateCircuit()
    {
        if (_components.Count == 0)
        {
            Debug.Log("[CircuitManager] Mạch không có linh kiện nào.");
            return;
        }

        // Tính tổng trở kháng và tổng hiệu điện thế của toàn mạch
        Complex totalImpedance = Complex.Zero;
        Complex totalVoltage = Complex.Zero;
        foreach (var component in _components)
        {
            totalImpedance += component.Impedance;
            totalVoltage += component.VoltageSource;
        }

        Debug.Log($"[CircuitManager] Tính toán: Total Voltage = {totalVoltage.Magnitude:F2}V, Total Impedance = {totalImpedance.Magnitude:F2} Ohm");

        //  Tính toán dòng điện trong mạch (Ohm: I = V / Z)
        Complex current = Complex.Zero;
        // Kiểm tra điều kiện để mạch hoạt động (phải có nguồn và không bị ngắn mạch)
        bool hasPowerSource = totalVoltage.Magnitude > 1e-6;
        bool isShortCircuit = totalImpedance.Magnitude < 1e-6;

        if (hasPowerSource && !isShortCircuit)
        {
            current = totalVoltage / totalImpedance;
        }
        else
        {
            if (!hasPowerSource) Debug.LogWarning("[CircuitManager] Mạch hở hoặc không có nguồn điện.");
            if (isShortCircuit) Debug.LogWarning("[CircuitManager] Cảnh báo: Ngắn mạch! Tổng trở kháng gần bằng 0.");
        }

        Debug.Log($"[CircuitManager] Dòng điện tính được: {current.Magnitude:F3}A, Phase: {current.Phase * Mathf.Rad2Deg:F2} deg");

        // Thông báo kết quả cho từng linh kiện để chúng cập nhật trạng thái
        foreach (var component in _components)
        {
            component.UpdateState(current);
        }
    }
}