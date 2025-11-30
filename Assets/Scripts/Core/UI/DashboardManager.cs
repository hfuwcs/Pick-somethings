using UnityEngine;
using System.Collections.Generic;
using Michsky.MUIP; // Namespace của Michsky

public class DashboardManager : MonoBehaviour
{
    public static DashboardManager Instance;

    [Header("UI References")]
    [SerializeField] private Transform contentContainer; // Kéo Dashboard_Panel vào đây
    [SerializeField] private GameObject rowPrefab;       // Kéo Prefab_DashboardRow vào đây
    [SerializeField] private SwitchManager modeSwitch;   // Kéo cái Switch Michsky vào đây

    private Dictionary<string, DashboardRow> _rows = new Dictionary<string, DashboardRow>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // Mặc định tắt Switch, ai cần dùng thì gọi Setup
        if (modeSwitch != null) modeSwitch.gameObject.SetActive(false);
    }

    public void ClearDashboard()
    {
        // Xóa hết các dòng cũ
        foreach (var row in _rows.Values)
        {
            Destroy(row.gameObject);
        }
        _rows.Clear();

        // Ẩn switch
        if (modeSwitch != null) modeSwitch.gameObject.SetActive(false);
    }

    // Hàm cập nhật số liệu (Nếu chưa có thì tạo mới, có rồi thì update text)
    public void UpdateStat(string id, string label, string value, Sprite icon = null)
    {
        if (_rows.ContainsKey(id))
        {
            _rows[id].SetData(label, value, icon);
        }
        else
        {
            GameObject newRow = Instantiate(rowPrefab, contentContainer);
            
            // Đảm bảo Switch luôn nằm cuối cùng (hoặc đầu tiên tùy ý)
            if (modeSwitch != null) 
                newRow.transform.SetSiblingIndex(modeSwitch.transform.GetSiblingIndex() - 1);

            DashboardRow rowScript = newRow.GetComponent<DashboardRow>();
            rowScript.SetData(label, value, icon);
            _rows.Add(id, rowScript);
        }
    }

    // Hàm cấu hình Switch cho chế độ Ideal/Realistic
    public void SetupModeSwitch(bool isOn, UnityEngine.Events.UnityAction<bool> onValueChanged)
    {
        if (modeSwitch == null) return;

        modeSwitch.gameObject.SetActive(true);
        modeSwitch.isOn = isOn;
        modeSwitch.UpdateUI(); // Cập nhật visual của Michsky

        // Xóa event cũ tránh trùng lặp
        modeSwitch.OnEvents.RemoveAllListeners();
        modeSwitch.OffEvents.RemoveAllListeners();

        // Gán event mới (Michsky dùng OnEvents cho TRUE và OffEvents cho FALSE)
        modeSwitch.OnEvents.AddListener(() => {
            if (AudioManager.Instance) AudioManager.Instance.PlaySound(AudioManager.Instance.clickSound);
            onValueChanged(true);
        });
        modeSwitch.OffEvents.AddListener(() => {
            if (AudioManager.Instance) AudioManager.Instance.PlaySound(AudioManager.Instance.clickSound);
            onValueChanged(false);
        });
    }
}