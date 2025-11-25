using UnityEngine;
using System.Collections.Generic;
using Michsky.MUIP; // Namespace của Switch Michsky

public class DashboardManager : MonoBehaviour
{
    public static DashboardManager Instance;

    [Header("UI References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private SwitchManager modeSwitch;
    private Dictionary<string, DashboardRow> _rows = new Dictionary<string, DashboardRow>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ClearDashboard()
    {
        foreach (var row in _rows.Values)
        {
            Destroy(row.gameObject);
        }
        _rows.Clear();
        
        if (modeSwitch != null) modeSwitch.gameObject.SetActive(false);
    }

    public void UpdateStat(string id, string label, string value, Sprite icon = null)
    {
        if (_rows.ContainsKey(id))
        {
            _rows[id].SetData(label, value, icon);
        }
        else
        {
            GameObject newRow = Instantiate(rowPrefab, contentContainer);
            if (modeSwitch != null) newRow.transform.SetSiblingIndex(modeSwitch.transform.GetSiblingIndex());
            
            DashboardRow rowScript = newRow.GetComponent<DashboardRow>();
            rowScript.SetData(label, value, icon);
            _rows.Add(id, rowScript);
        }
    }

    public void SetupModeSwitch(bool isOn, UnityEngine.Events.UnityAction<bool> onValueChanged)
    {
        if (modeSwitch == null) return;

        modeSwitch.gameObject.SetActive(true);
        modeSwitch.isOn = isOn;
        modeSwitch.UpdateUI();
        modeSwitch.OnEvents.RemoveAllListeners();
        modeSwitch.OffEvents.RemoveAllListeners();
        
        modeSwitch.OnEvents.AddListener(() => onValueChanged(true));
        modeSwitch.OffEvents.AddListener(() => onValueChanged(false));
    }
}