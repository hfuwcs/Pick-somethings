using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CircuitSchematicGenerator : MonoBehaviour
{
    public static CircuitSchematicGenerator Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Kéo cái Canvas (World Space) ở dưới lòng đất vào đây")]
    [SerializeField] private RectTransform schematicContainer; 
    [SerializeField] private GameObject iconPrefab; // Prefab chứa Image, layer SchematicUI
    [SerializeField] private GameObject linePrefab; // Prefab chứa Image (mảnh), layer SchematicUI

    [Header("Settings")]
    [SerializeField] private float scaleFactor = 100f; // Hệ số phóng đại từ 3D sang 2D

    // Lưu trữ tham chiếu để vẽ dây
    private Dictionary<Connector, Vector2> _connectorUIPositions = new Dictionary<Connector, Vector2>();
    private List<GameObject> _spawnedObjects = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RebuildSchematic()
    {
        ClearOld();
        
        // 1. Tìm tất cả linh kiện có gắn SchematicIcon
        var icons = FindObjectsByType<SchematicIcon>(FindObjectsSortMode.None);
        if (icons.Length == 0) return;

        // 2. Tính tâm của mạch 3D (để map vào tâm Canvas 2D)
        Vector3 center3D = CalculateCenter(icons);

        // 3. Vẽ Linh kiện
        foreach (var icon in icons)
        {
            // Map vị trí: (X, Z) trong 3D -> (X, Y) trong UI
            Vector3 worldPos = icon.transform.position;
            Vector3 relativePos = worldPos - center3D;
            Vector2 uiPos = new Vector2(relativePos.x, relativePos.z) * scaleFactor;

            // Spawn Icon
            GameObject uiObj = Instantiate(iconPrefab, schematicContainer);
            uiObj.transform.localPosition = uiPos; // World Space Canvas dùng localPos chuẩn
            uiObj.transform.localRotation = Quaternion.Euler(0, 0, -icon.GetRotation()); // Xoay ngược

            // Set Sprite
            Image img = uiObj.GetComponentInChildren<Image>(); // Tìm Image trong prefab
            if (img) img.sprite = icon.symbol;

            // LƯU VỊ TRÍ CONNECTOR (Quan trọng cho vẽ dây)
            // Ta cần biết Connector A và B của linh kiện này nằm đâu trên bản vẽ 2D
            // Logic đơn giản: Lấy vị trí Connector 3D -> Map y hệt như trên
            var component = icon.GetComponent<CircuitComponent>();
            if (component != null)
            {
                MapConnector(component.ConnectorA, center3D);
                MapConnector(component.ConnectorB, center3D);
            }
            
            _spawnedObjects.Add(uiObj);
        }

        // 4. Vẽ Dây (Sẽ dùng L-Shape ở đây sau)
        DrawWiresDirectly(); 
    }

    private void MapConnector(Connector c, Vector3 center3D)
    {
        if (c == null) return;
        Vector3 rel = c.transform.position - center3D;
        Vector2 pos = new Vector2(rel.x, rel.z) * scaleFactor;
        
        if (!_connectorUIPositions.ContainsKey(c))
            _connectorUIPositions.Add(c, pos);
        else
            _connectorUIPositions[c] = pos;
    }

    private void DrawWiresDirectly()
    {
        var wires = FindObjectsByType<Wire>(FindObjectsSortMode.None);

        foreach (var wire in wires)
        {
            if (wire.StartConnector == null || wire.EndConnector == null) continue;

            if (_connectorUIPositions.TryGetValue(wire.StartConnector, out Vector2 start) &&
                _connectorUIPositions.TryGetValue(wire.EndConnector, out Vector2 end))
            {
                // Thay vì vẽ 1 đường chéo, ta gọi hàm vẽ chữ L
                DrawLShapeConnection(start, end, Color.black);
            }
        }
    }

    // Hàm mới: Vẽ chữ L
    private void DrawLShapeConnection(Vector2 start, Vector2 end, Color color)
    {
        // Tính toán điểm gập (Corner)
        // Logic: Đi Ngang trước (theo X của End), rồi đi Dọc (theo Y của Start)
        // Bạn có thể đổi thành (start.x, end.y) nếu muốn đi Dọc trước
        Vector2 corner = new Vector2(end.x, start.y);

        // Vẽ đoạn 1: Start -> Corner (Ngang)
        CreateLineSegment(start, corner, color);

        // Vẽ đoạn 2: Corner -> End (Dọc)
        CreateLineSegment(corner, end, color);
    }

    private void CreateLineSegment(Vector2 p1, Vector2 p2, Color color)
    {
        // Nếu 2 điểm trùng nhau (hoặc quá gần) thì không vẽ
        if (Vector2.Distance(p1, p2) < 0.1f) return;

        GameObject lineObj = Instantiate(linePrefab, schematicContainer);
        _spawnedObjects.Add(lineObj);

        // Đẩy dây xuống dưới cùng để không đè lên Icon
        lineObj.transform.SetAsFirstSibling(); 

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        Image img = lineObj.GetComponent<Image>();
        img.color = color;

        // Toán học vẽ đoạn thẳng
        Vector2 dir = p2 - p1;
        float dist = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rect.sizeDelta = new Vector2(dist, 4f); // Độ dày nét vẽ (nên để 3-4 thôi)
        rect.anchoredPosition = p1 + dir / 2;
        rect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private void ClearOld()
    {
        foreach (var o in _spawnedObjects) Destroy(o);
        _spawnedObjects.Clear();
        _connectorUIPositions.Clear();
    }

    private Vector3 CalculateCenter(SchematicIcon[] icons)
    {
        if (icons.Length == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var i in icons) sum += i.transform.position;
        return sum / icons.Length;
    }
}