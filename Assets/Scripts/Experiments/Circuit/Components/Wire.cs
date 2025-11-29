using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class Wire : MonoBehaviour, IInteractable
{
    [Header("Wire Settings")]
    [SerializeField] private float wireThickness = 0.02f;
    [SerializeField] private int segmentCount = 20; // Số lượng đốt dây (càng nhiều càng mượt nhưng nặng hơn)
    [SerializeField] private float totalLengthMultiplier = 1.1f; // Dây dài hơn khoảng cách thực 10% (để có độ chùng)

    [Header("Physics Settings")]
    [SerializeField] private int solverIterations = 10; // Càng cao dây càng cứng (ít co giãn), chuẩn là 3-10
    [SerializeField] private Vector3 gravity = new Vector3(0, -9.81f, 0);
    [SerializeField] private float drag = 0.95f; // Ma sát không khí (để dây không đung đưa mãi)
    [SerializeField] private LayerMask collisionLayer; // Layer của các vật thể dây sẽ va chạm

    // Cấu trúc dữ liệu cho mỗi đốt dây
    private class Node
    {
        public Vector3 position;
        public Vector3 prevPosition;

        public Node(Vector3 pos)
        {
            position = pos;
            prevPosition = pos;
        }
    }

    private List<Node> nodes = new List<Node>();
    private float segmentLength;
    private LineRenderer lineRenderer;
    private bool isSimulating = false;

    public Connector StartConnector { get; private set; }
    public Connector EndConnector { get; private set; }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = wireThickness;
        lineRenderer.endWidth = wireThickness;
        lineRenderer.useWorldSpace = true;
    }

    // --- SETUP PHASE ---

    public void Initialize(Connector start)
    {
        StartConnector = start;
        lineRenderer.positionCount = 2;
        StartConnector.AddWire(this); 
        lineRenderer.SetPosition(0, start.transform.position);
        lineRenderer.SetPosition(1, start.transform.position);
    }

    public void UpdateEndPosition(Vector3 worldEndPos)
    {
        if (isSimulating) return;
        lineRenderer.SetPosition(0, StartConnector.transform.position);
        lineRenderer.SetPosition(1, worldEndPos);
    }

    public void Complete(Connector end)
    {
        EndConnector = end;
        StartSimulation();
        
        gameObject.name = $"Wire_{StartConnector.ParentComponent.name}_to_{EndConnector.ParentComponent.name}";
        EndConnector.AddWire(this);
    }

    // --- PHYSICS ENGINE (VERLET INTEGRATION) ---

    private void StartSimulation()
    {
        Vector3 startPos = StartConnector.transform.position;
        Vector3 endPos = EndConnector.transform.position;
        float dist = Vector3.Distance(startPos, endPos);
        
        float totalLength = dist * totalLengthMultiplier;
        segmentLength = totalLength / segmentCount;

        nodes.Clear();
        Vector3 direction = (endPos - startPos).normalized;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 p = startPos + direction * (dist * ((float)i / (segmentCount - 1)));
            nodes.Add(new Node(p));
        }

        lineRenderer.positionCount = nodes.Count;
        isSimulating = true;
    }

    private void FixedUpdate()
    {
        if (!isSimulating) return;

        SimulateVerlet();
        ApplyConstraints();
        SolveCollisions(); 
        UpdateVisuals();
    }

    private void SimulateVerlet()
    {
        float dt = Time.fixedDeltaTime;
        
        for (int i = 1; i < nodes.Count - 1; i++)
        {
            Node node = nodes[i];
            Vector3 velocity = (node.position - node.prevPosition) * drag;
            node.prevPosition = node.position;
            
            node.position += velocity + gravity * (dt * dt);
        }
    }

    private void ApplyConstraints()
    {
        if (StartConnector != null) nodes[0].position = StartConnector.transform.position;
        if (EndConnector != null) nodes[nodes.Count - 1].position = EndConnector.transform.position;

        for (int k = 0; k < solverIterations; k++)
        {
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Node nodeA = nodes[i];
                Node nodeB = nodes[i + 1];

                Vector3 delta = nodeB.position - nodeA.position;
                float currentDist = delta.magnitude;
                
                if (currentDist < 0.0001f) continue; 

                float difference = (currentDist - segmentLength) / currentDist;
                Vector3 correction = delta * 0.5f * difference;

                if (i == 0)
                {
                    nodeB.position -= correction * 2f;
                }
                else if (i + 1 == nodes.Count - 1)
                {
                    nodeA.position += correction * 2f;
                }
                else
                {
                    nodeA.position += correction;
                    nodeB.position -= correction;
                }
            }
        }
    }

    // --- COLLISION LOGIC ---
    private void SolveCollisions()
    {

        float collisionRadius = wireThickness * 0.5f; 

        for (int i = 1; i < nodes.Count - 1; i++)
        {
            Collider[] colliders = Physics.OverlapSphere(nodes[i].position, collisionRadius, collisionLayer);
            
            foreach (var col in colliders)
            {
                Vector3 closestPoint = col.ClosestPoint(nodes[i].position);

                nodes[i].position = closestPoint + (nodes[i].position - closestPoint).normalized * 0.005f;

                nodes[i].prevPosition = nodes[i].position; 
            }
        }
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            lineRenderer.SetPosition(i, nodes[i].position);
        }
    }

    // --- UTILITIES ---

    public void DisconnectAndDestroy()
    {
        if (StartConnector != null) StartConnector.RemoveWire(this);
        if (EndConnector != null) EndConnector.RemoveWire(this);
        if (CircuitManager.Instance != null) CircuitManager.Instance.RecalculateCircuit();
        Destroy(gameObject);
    }

    public void SetColor(Material mat)
    {
        if (lineRenderer != null) lineRenderer.material = mat;
    }

    public void OnHoverEnter() { }
    public void OnHoverExit() { }
    public void OnSelectStart() { }
    public void OnSelectEnd() { }
}