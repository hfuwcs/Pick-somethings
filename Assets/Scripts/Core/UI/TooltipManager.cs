using UnityEngine;
using TMPro;
using System.Collections;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("Michsky UI References")]
    [SerializeField] private GameObject tooltipObject;     // Object cha (Michsky_Tooltip)
    [SerializeField] private RectTransform tooltipRect;    // RectTransform của nó
    [SerializeField] private TextMeshProUGUI contentText;  // Text nội dung chính
    [SerializeField] private TextMeshProUGUI titleText;    // Text tiêu đề (nếu có)
    [SerializeField] private CanvasGroup canvasGroup;      // Để làm hiệu ứng Fade

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(15, -15); // Cách chuột bao xa
    [SerializeField] private float fadeSpeed = 10f; // Tốc độ hiện ra

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (canvasGroup != null) canvasGroup.alpha = 0;
    }

    private void Update()
    {
        if (canvasGroup.alpha > 0)
        {
            FollowMouse();
        }
    }

    private void FollowMouse()
    {
        Vector2 mousePos = UnityEngine.Input.mousePosition;
        
        float pivotX = mousePos.x / Screen.width;
        float pivotY = mousePos.y / Screen.height;

        tooltipRect.pivot = new Vector2(pivotX > 0.8f ? 1 : 0, pivotY < 0.2f ? 0 : 1);
        
        tooltipRect.position = mousePos + offset;
    }

    public void ShowTooltip(string content, string title = "")
    {
        if (contentText != null) contentText.text = content;
        
        if (titleText != null)
        {
            if (string.IsNullOrEmpty(title)) titleText.gameObject.SetActive(false);
            else 
            {
                titleText.gameObject.SetActive(true);
                titleText.text = title;
            }
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 1f));
    }

    public void HideTooltip()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0f));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha)
    {
        while (Mathf.Abs(cg.alpha - targetAlpha) > 0.01f)
        {
            cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        cg.alpha = targetAlpha;
    }
}