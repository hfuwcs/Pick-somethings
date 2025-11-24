using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDebugger : MonoBehaviour
{
    // Bấm F12 khi đang Play để kiểm tra
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            Diagnose();
        }
    }

    [ContextMenu("Diagnose UI")] // Cho phép kích hoạt từ Inspector ngay cả khi không Play
    public void Diagnose()
    {
        RectTransform rect = GetComponent<RectTransform>();
        CanvasGroup cg = GetComponent<CanvasGroup>();
        Canvas rootCanvas = GetComponentInParent<Canvas>();

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<color=yellow>=== BÁO CÁO DIAGNOSTIC UI: {gameObject.name} ===</color>");

        // 1. KIỂM TRA ACTIVE
        sb.AppendLine($"<b>1. GameObject Active:</b> Self={gameObject.activeSelf}, Hierarchy={gameObject.activeInHierarchy}");
        if (!gameObject.activeInHierarchy) sb.AppendLine("<color=red>-> LỖI: Object đang bị tắt (Inactive)!</color>");

        // 2. KIỂM TRA TRANSFORM (Vị trí & Kích thước)
        sb.AppendLine($"<b>2. RectTransform:</b>");
        sb.AppendLine($"   - Position (World): {rect.position}");
        sb.AppendLine($"   - Anchored Position (Local): {rect.anchoredPosition}");
        sb.AppendLine($"   - Size Delta: {rect.sizeDelta} (Width x Height)");
        sb.AppendLine($"   - Scale: {rect.localScale}");
        
        if (rect.localScale == Vector3.zero) sb.AppendLine("<color=red>-> LỖI: Scale đang là (0,0,0). Tooltip bị thu nhỏ vô hình!</color>");
        if (rect.rect.width == 0 || rect.rect.height == 0) sb.AppendLine("<color=red>-> LỖI: Width hoặc Height bằng 0!</color>");

        // 3. KIỂM TRA CANVAS GROUP (Độ mờ)
        if (cg != null)
        {
            sb.AppendLine($"<b>3. Canvas Group:</b> Alpha={cg.alpha}, Interactable={cg.interactable}, BlocksRaycasts={cg.blocksRaycasts}");
            if (cg.alpha == 0) sb.AppendLine("<color=red>-> LỖI: Alpha = 0 (Hoàn toàn trong suốt)!</color>");
        }
        else
        {
            sb.AppendLine("<b>3. Canvas Group:</b> <color=grey>Không tìm thấy component này.</color>");
        }

        // 4. KIỂM TRA MÀN HÌNH
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, rect.position);
        sb.AppendLine($"<b>4. Screen Position:</b> {screenPos}");
        if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
            sb.AppendLine("<color=orange>-> CẢNH BÁO: Tooltip đang nằm ngoài màn hình!</color>");

        // 5. KIỂM TRA CONTENT (Text)
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        sb.AppendLine($"<b>5. Texts found ({texts.Length}):</b>");
        foreach (var t in texts)
        {
            sb.AppendLine($"   - [{t.gameObject.name}]: '{t.text}' | Color Alpha: {t.color.a} | Visible: {t.gameObject.activeInHierarchy}");
        }

        // 6. KIỂM TRA CANVAS CHA
        if (rootCanvas != null)
        {
            sb.AppendLine($"<b>6. Root Canvas:</b> {rootCanvas.name} | RenderMode: {rootCanvas.renderMode} | Scale: {rootCanvas.transform.localScale.x}");
        }
        else
        {
            sb.AppendLine("<color=red>-> LỖI NGHIÊM TRỌNG: Không tìm thấy Canvas cha!</color>");
        }

        Debug.Log(sb.ToString());
    }
}