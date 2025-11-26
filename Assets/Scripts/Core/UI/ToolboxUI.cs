using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Michsky.MUIP; // Namespace Button của Michsky

public class ToolboxUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<SpawnableItem> itemsToDisplay; 
    [Header("UI References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject buttonPrefab;

    private void Start()
    {
        GenerateButtons();
    }

    private void GenerateButtons()
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        foreach (var item in itemsToDisplay)
        {
            GameObject btnObj = Instantiate(buttonPrefab, contentContainer);
            
            ButtonManager btnManager = btnObj.GetComponent<ButtonManager>();
            if (btnManager != null)
            {
                btnManager.buttonText = item.itemName;
                if (item.icon != null) btnManager.buttonIcon = item.icon;
                
                btnManager.onClick.AddListener(() => OnItemClicked(item));
                
                btnManager.UpdateUI();
            }
            

        }
    }

    private void OnItemClicked(SpawnableItem item)
    {
        SpawnManager.Instance.SpawnItem(item);
    }
}