using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TestScrollLoad : MonoBehaviour
{
    public Transform contentContainer;
    public GameObject pagePrefab;
    public ScrollRect scrollRect;

    void Start()
    {
        StartCoroutine(RunTest());
    }

    IEnumerator RunTest()
    {
        // 1. Tạo giả 2 tấm ảnh (1 tấm dọc, 1 tấm ngang)
        Texture2D tex1 = CreateTexture(Color.red, 500, 1000); // Ảnh dọc (Cao gấp đôi rộng)
        Texture2D tex2 = CreateTexture(Color.green, 1000, 500); // Ảnh ngang (Rộng gấp đôi cao)
        
        Texture2D[] testTextures = new Texture2D[] { tex1, tex2 };

        // 2. Đợi 1 frame để UI ổn định
        yield return null;

        // 3. Lấy chiều rộng thực tế của vùng hiển thị
        float containerWidth = contentContainer.GetComponent<RectTransform>().rect.width;
        // Fallback nếu chưa lấy được (thường do Layout chưa cập nhật kịp)
        if (containerWidth == 0) containerWidth = scrollRect.GetComponent<RectTransform>().rect.width;

        Debug.Log("Chiều rộng Container: " + containerWidth);

        foreach (Texture2D tex in testTextures)
        {
            GameObject obj = Instantiate(pagePrefab, contentContainer);
            RawImage img = obj.GetComponent<RawImage>();
            img.texture = tex;

            // --- CÁCH TỰ ĐỘNG (ASPECT RATIO FITTER) ---
            
            // 1. Tìm component
            AspectRatioFitter fitter = obj.GetComponent<AspectRatioFitter>();
            
            // 2. Tính tỷ lệ (Rộng / Cao)
            float ratio = (float)tex.width / (float)tex.height;
            
            // 3. Gán tỷ lệ cho component tự xử
            fitter.aspectRatio = ratio;

            // KHÔNG set LayoutElement minHeight/preferredHeight nữa!
            // KHÔNG tính toán targetHeight nữa!

            Debug.Log($"Ảnh {tex.width}x{tex.height} -> Ratio {ratio} -> Fitter tự lo Height");
        }

        // 4. Rebuild (Vẫn cần thiết để Content cập nhật ngay)
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer.GetComponent<RectTransform>());
    }

    // Hàm tạo ảnh màu giả lập
    Texture2D CreateTexture(Color col, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h);
        // Chỉ tô 1 màu cho nhanh
        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}