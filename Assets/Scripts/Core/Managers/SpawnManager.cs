using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Header("Settings")]
    [Tooltip("Khoảng cách xuất hiện trước mặt Camera")]
    [SerializeField] private float spawnDistance = 1.5f;
    [SerializeField] private Transform spawnOrigin;
    [SerializeField] private int maxItemsTotal = 20;

    private Dictionary<string, int> _itemCounters = new Dictionary<string, int>();
    private List<GameObject> _spawnedObjects = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (spawnOrigin == null) spawnOrigin = Camera.main.transform;
    }

    public void SpawnItem(SpawnableItem itemData)
    {
        if (_spawnedObjects.Count >= maxItemsTotal)
        {
            if (ExperimentNotification.Instance != null)
                ExperimentNotification.Instance.Show("Kho đầy rồi!", "Không thể tạo thêm vật phẩm, hãy xóa bớt.", ExperimentNotification.Type.Warning);
            return;
        }

        if (itemData == null || itemData.prefab == null) return;

        Vector3 randomOffset = new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
        Vector3 spawnPos = spawnOrigin.position + (spawnOrigin.forward * spawnDistance) + randomOffset;

        GameObject newItem = Instantiate(itemData.prefab, spawnPos, Quaternion.identity);

        string key = itemData.itemName;
        if (!_itemCounters.ContainsKey(key)) _itemCounters[key] = 0;
        
        _itemCounters[key]++;
        int count = _itemCounters[key];
        
        newItem.name = $"{key} #{count}";

        _spawnedObjects.Add(newItem);

        Rigidbody rb = newItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (ExperimentNotification.Instance != null)
        {
            ExperimentNotification.Instance.Show("Đã lấy", $"Đã thêm {newItem.name}", ExperimentNotification.Type.Success);
        }
    }

    public void RemoveItem(GameObject item)
    {
        if (_spawnedObjects.Contains(item))
        {
            _spawnedObjects.Remove(item);
            Destroy(item);
        }
    }
}