using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridGenerator : MonoBehaviour
{
    public GameObject slotPrefab;
    public int rows = 3;
    public int cols = 4;
    public float spacing = 0.25f;

    [ContextMenu("Generate Grid")]
    public void Generate()
    {
        // 1. Dọn dẹp con cũ
        // Lưu ý: Dùng vòng lặp ngược để destroy an toàn trong Editor
        var tempArray = new GameObject[transform.childCount];
        for(int i = 0; i < tempArray.Length; i++)
        {
           tempArray[i] = transform.GetChild(i).gameObject;
        }
        foreach(var child in tempArray)
        {
            DestroyImmediate(child);
        }

        // 2. Tính toán vị trí bắt đầu (để grid nằm giữa tâm Container)
        float startX = -(cols - 1) * spacing / 2;
        float startZ = -(rows - 1) * spacing / 2;

        for (int x = 0; x < cols; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                // Tính vị trí cục bộ
                Vector3 localPos = new Vector3(startX + x * spacing, 0, startZ + z * spacing);
                
                // Tạo Prefab
                #if UNITY_EDITOR
                GameObject slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, transform);
                #else
                GameObject slot = Instantiate(slotPrefab, transform);
                #endif

                // 3. QUAN TRỌNG: Reset Scale sau khi làm con
                // Đảm bảo slot luôn có scale (1,1,1) so với cha (Container)
                slot.transform.localScale = Vector3.one; 
                
                slot.transform.localPosition = localPos;
                slot.name = $"Slot_{x}_{z}";
            }
        }
    }
}