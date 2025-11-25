using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DashboardRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Image iconImage;

    public void SetData(string label, string value, Sprite icon = null)
    {
        if (labelText != null) labelText.text = label;
        if (valueText != null) valueText.text = value;
        
        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }
    }
}