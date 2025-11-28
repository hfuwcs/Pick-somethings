using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        WiringManager.OnWireConnected += HandleCircuitChanged;
    }

    private void OnDisable()
    {
        WiringManager.OnWireConnected -= HandleCircuitChanged;
    }

    private void HandleCircuitChanged(Connector c1, Connector c2)
    {
        RecalculateCircuit();
    }

    public void RecalculateCircuit()
    {
        var allComponents = FindObjectsByType<CircuitComponent>(FindObjectsSortMode.None);
        if (allComponents.Length == 0) return;

        var schematicGen = FindFirstObjectByType<CircuitSchematicGenerator>();
        if (schematicGen != null)
        {
            schematicGen.RebuildSchematic();
        }
        CircuitGraph graph = BuildCircuitGraph(allComponents);
        Dictionary<int, Complex> nodeVoltages = SolveCircuit(graph);
        UpdateAllComponents(graph, nodeVoltages);
    }

    private CircuitGraph BuildCircuitGraph(IEnumerable<CircuitComponent> components)
    {
        var graph = new CircuitGraph();
        int nodeIdCounter = 0;

        var nodeMap = new Dictionary<Connector, ElectricalNode>();

        var activeConnectors = FindObjectsByType<Connector>(FindObjectsSortMode.None)
                               .Where(c => c.IsInteractableForWiring && c.HasActiveConnection)
                               .ToList();

        var visitedConnectors = new HashSet<Connector>();

        foreach (var startConnector in activeConnectors)
        {
            if (visitedConnectors.Contains(startConnector)) continue;

            var newNode = new ElectricalNode(nodeIdCounter++);
            graph.Nodes.Add(newNode);

            // BFS to find all connected connectors
            var queue = new Queue<Connector>();
            queue.Enqueue(startConnector);
            visitedConnectors.Add(startConnector);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                newNode.Connectors.Add(current);
                nodeMap[current] = newNode;

                // Find connected connectors via wires
                foreach (var wire in current.ConnectedWires)
                {
                    if (wire == null) continue;

                    Connector neighbor = (wire.StartConnector == current) ? wire.EndConnector : wire.StartConnector;

                    if (neighbor != null && !visitedConnectors.Contains(neighbor))
                    {
                        if (neighbor.HasActiveConnection)
                        {
                            visitedConnectors.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            Debug.Log($"[BuildGraph-Nodes] Đã tạo Node ID: {newNode.Id} với {newNode.Connectors.Count} connectors: {string.Join(", ", newNode.Connectors.Select(c => c.name))}");
        }

        // Build branches from components
        foreach (var component in components)
        {
            // Only create branch if both connectors are in the node map
            if (nodeMap.TryGetValue(component.ConnectorA, out var nodeA) &&
                nodeMap.TryGetValue(component.ConnectorB, out var nodeB))
            {
                // Skip short circuit case
                if (nodeA == nodeB)
                {
                    Debug.LogWarning($"[BuildGraph-Branches] Linh kiện {component.name} bị ngắn mạch (2 đầu cùng Node {nodeA.Id}), bỏ qua.");
                    continue;
                }

                var branch = new ElectricalBranch(component, nodeA, nodeB);
                graph.Branches.Add(branch);
                Debug.Log($"[BuildGraph-Branches] Đã tạo Branch: {component.name} nối giữa Node {nodeA.Id} và Node {nodeB.Id}");
            }
            else
            {

                component.UpdateState(Complex.Zero, Complex.Zero);
            }
        }

        Debug.Log($"[BuildGraph-Summary] Hoàn thành xây dựng Graph: {graph.Nodes.Count} Nodes, {graph.Branches.Count} Branches.");
        return graph;
    }

    private Dictionary<int, Complex> SolveCircuit(CircuitGraph graph)
    {
        Debug.Log($"[SolveCircuit] Bắt đầu giải mạch với {graph.Nodes.Count} Nodes và {graph.Branches.Count} Branches.");

        var powerSourceBranch = graph.Branches.FirstOrDefault(b => b.Component is PowerSource);
        if (powerSourceBranch == null || graph.Nodes.Count < 2)
        {
            Debug.LogWarning("Không tìm thấy nguồn điện hoặc không đủ nút.");
            return new Dictionary<int, Complex>();
        }

        PowerSource powerSource = powerSourceBranch.Component as PowerSource;

        ElectricalNode groundNode = powerSourceBranch.NodeB;
        ElectricalNode sourceNode = powerSourceBranch.NodeA;
        Debug.Log($"[SolveCircuit] Nút đất (V=0): Node {groundNode.Id}. Nút nguồn (V={powerSource.VoltageSource.Magnitude}): Node {sourceNode.Id}.");

        // Collect unknown nodes for solving
        List<ElectricalNode> unknownNodes = graph.Nodes.Where(n => n != groundNode && n != sourceNode).ToList();
        int n = unknownNodes.Count;

        if (n == 0)
        {
            var results = new Dictionary<int, Complex>();
            results[groundNode.Id] = 0;
            results[sourceNode.Id] = powerSource.VoltageSource;
            return results;
        }

        Complex[,] A = new Complex[n, n];
        Complex[] B = new Complex[n];

        for (int i = 0; i < n; i++)
        {
            ElectricalNode currentNode = unknownNodes[i];

            foreach (var branch in graph.Branches.Where(b => b.NodeA == currentNode || b.NodeB == currentNode))
            {
                if (branch.Component.Impedance.Magnitude > 1e-9)
                {
                    A[i, i] += 1.0 / branch.Component.Impedance;
                }
            }

            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                ElectricalNode otherNode = unknownNodes[j];
                foreach (var branch in graph.Branches.Where(b => (b.NodeA == currentNode && b.NodeB == otherNode) || (b.NodeA == otherNode && b.NodeB == currentNode)))
                {
                    if (branch.Component.Impedance.Magnitude > 1e-9)
                    {
                        A[i, j] -= 1.0 / branch.Component.Impedance;
                    }
                }
            }

            foreach (var branch in graph.Branches.Where(b => (b.NodeA == currentNode && b.NodeB == sourceNode) || (b.NodeA == sourceNode && b.NodeB == currentNode)))
            {
                if (branch.Component.Impedance.Magnitude > 1e-9)
                {
                    B[i] += powerSource.VoltageSource / branch.Component.Impedance;
                }
            }
        }

        Complex[] unknownVoltages = SolveLinearSystem(A, B);
        Debug.Log($"[SolveCircuit] Hệ phương trình đã được giải. Điện thế các nút chưa biết: [{string.Join(", ", unknownVoltages.Select(v => v.Magnitude.ToString("F2")))}]");

        var finalVoltages = new Dictionary<int, Complex>();
        finalVoltages[groundNode.Id] = 0;
        finalVoltages[sourceNode.Id] = powerSource.VoltageSource;
        for (int i = 0; i < n; i++)
        {
            finalVoltages[unknownNodes[i].Id] = unknownVoltages[i];
        }
        return finalVoltages;
    }

    private void UpdateAllComponents(CircuitGraph graph, Dictionary<int, Complex> nodeVoltages)
    {
        if (nodeVoltages == null || nodeVoltages.Count == 0)
        {
            foreach (var component in FindObjectsByType<CircuitComponent>(FindObjectsSortMode.None))
            {
                component.UpdateState(Complex.Zero, Complex.Zero);
            }
            return;
        }


        var activeComponents = new HashSet<CircuitComponent>();
        foreach (var branch in graph.Branches)
        {
            activeComponents.Add(branch.Component);
        }

        foreach (var component in FindObjectsByType<CircuitComponent>(FindObjectsSortMode.None))
        {
            if (!activeComponents.Contains(component))
            {
                component.UpdateState(Complex.Zero, Complex.Zero);
            }
        }

        foreach (var branch in graph.Branches.Where(b => !(b.Component is PowerSource)))
        {
            Complex vA = nodeVoltages.GetValueOrDefault(branch.NodeA.Id, 0);
            Complex vB = nodeVoltages.GetValueOrDefault(branch.NodeB.Id, 0);

            // Hiệu điện thế (Voltage drop)
            Complex voltageDiff = vA - vB;

            // Dòng điện (Current) - Định luật Ohm: I = U / Z
            Complex current = Complex.Zero;
            if (branch.Component.Impedance.Magnitude > 1e-9)
            {
                current = voltageDiff / branch.Component.Impedance;
            }

            Debug.Log($"[Update] Linh kiện: {branch.Component.name}, V_A(N{branch.NodeA.Id})={vA.Magnitude:F2}V, V_B(N{branch.NodeB.Id})={vB.Magnitude:F2}V, I = {current.Magnitude:F3}A");

            branch.Component.UpdateState(voltageDiff, current);
        }

        var powerSourceBranch = graph.Branches.FirstOrDefault(b => b.Component is PowerSource);
        if (powerSourceBranch != null)
        {
            Complex sourceCurrent = 0;


            foreach (var branch in graph.Branches)
            {
                if (branch.Component is PowerSource) continue;

                if (branch.NodeA == powerSourceBranch.NodeA || branch.NodeB == powerSourceBranch.NodeA)
                {
                    Complex vA = nodeVoltages.GetValueOrDefault(branch.NodeA.Id, 0);
                    Complex vB = nodeVoltages.GetValueOrDefault(branch.NodeB.Id, 0);

                    Complex branchCurrent = Complex.Zero;
                    if (branch.Component.Impedance.Magnitude > 1e-9)
                        branchCurrent = (vA - vB) / branch.Component.Impedance;

                    if (branch.NodeA == powerSourceBranch.NodeA)
                        sourceCurrent += branchCurrent;
                    else
                        sourceCurrent -= branchCurrent;
                }
            }

            Debug.Log($"[Update] Nguồn: {powerSourceBranch.Component.name}, I = {sourceCurrent.Magnitude:F3}A");

            double maxSafeCurrent = 20.0; // Giới hạn an toàn

            if (sourceCurrent.Magnitude > maxSafeCurrent)
            {
                Debug.LogWarning("QUÁ TẢI! Ngắt điện toàn mạch để bảo vệ.");
                if (ExperimentNotification.Instance != null)
                {
                    ExperimentNotification.Instance.Show(
                        "NGẮN MẠCH!",
                        "Dòng điện quá lớn. Hệ thống đã tự ngắt để bảo vệ mạch.",
                        ExperimentNotification.Type.Error
                    );

                    // if (AudioManager.Instance) AudioManager.Instance.PlayError();
                }
                powerSourceBranch.Component.UpdateState(Complex.Zero, sourceCurrent);

                foreach (var branch in graph.Branches)
                {
                    if (branch.Component != powerSourceBranch.Component)
                    {
                        branch.Component.UpdateState(Complex.Zero, Complex.Zero);
                    }
                }
            }
            else
            {
                powerSourceBranch.Component.UpdateState(powerSourceBranch.Component.VoltageSource, sourceCurrent);
            }
        }
    }


    private Complex[] SolveLinearSystem(Complex[,] A, Complex[] B)
    {
        int n = B.Length;
        for (int p = 0; p < n; p++)
        {
            int max = p;
            for (int i = p + 1; i < n; i++)
            {
                if (A[i, p].Magnitude > A[max, p].Magnitude)
                {
                    max = i;
                }
            }
            Complex[] tempA = new Complex[n];
            for (int i = 0; i < n; i++) tempA[i] = A[p, i];
            for (int i = 0; i < n; i++) A[p, i] = A[max, i];
            for (int i = 0; i < n; i++) A[max, i] = tempA[i];

            Complex tempB = B[p];
            B[p] = B[max];
            B[max] = tempB;

            if (A[p, p].Magnitude <= 1e-9) continue;

            for (int i = p + 1; i < n; i++)
            {
                Complex alpha = A[i, p] / A[p, p];
                B[i] -= alpha * B[p];
                for (int j = p; j < n; j++)
                {
                    A[i, j] -= alpha * A[p, j];
                }
            }
        }

        Complex[] x = new Complex[n];
        for (int i = n - 1; i >= 0; i--)
        {
            Complex sum = 0.0;
            for (int j = i + 1; j < n; j++)
            {
                sum += A[i, j] * x[j];
            }
            if (A[i, i].Magnitude > 1e-9)
            {
                x[i] = (B[i] - sum) / A[i, i];
            }
        }
        return x;
    }
}