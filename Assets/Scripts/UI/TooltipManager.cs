using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [SerializeField] private RectTransform tooltipPanel; 
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Vector2 offset = new Vector2(15, -15);

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        
        HideTooltip();
    }

    private void Update()
    {
        if (tooltipPanel.gameObject.activeSelf)
        {
            MoveTooltipToMouse();
        }
    }

    public void ShowTooltip(string content)
    {
        tooltipText.text = content;
        tooltipPanel.gameObject.SetActive(true);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
        MoveTooltipToMouse();
    }

    public void HideTooltip()
    {
        tooltipPanel.gameObject.SetActive(false);
    }

    private void MoveTooltipToMouse()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        float pivotX = mousePos.x / Screen.width;
        float pivotY = mousePos.y / Screen.height;

        tooltipPanel.pivot = new Vector2(pivotX, pivotY);
        tooltipPanel.position = mousePos + offset;
    }
}