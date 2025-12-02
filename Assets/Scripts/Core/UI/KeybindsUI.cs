using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
//This file using for "Panel hướng dẫn" (đúng rồi đấy, mixing Viet and Eng)
public class KeybindsUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject contentContainer;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Button toggleButton;
    
    private bool isExpanded = true;

    private void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);
    }

    public void TogglePanel()
    {
        isExpanded = !isExpanded;

        if (contentContainer != null)
            contentContainer.SetActive(isExpanded);

        if (headerText != null)
            headerText.text = isExpanded ? "HƯỚNG DẪN [-]" : "HƯỚNG DẪN [+]";
    }
}