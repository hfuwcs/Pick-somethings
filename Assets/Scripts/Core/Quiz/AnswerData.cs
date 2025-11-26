using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerData : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI infoTextObject;
    [SerializeField] Image toggle;

    [Header("Textures")]
    [SerializeField] Sprite uncheckToggle;
    [SerializeField] Sprite checkToggle;

    [Header("References")]
    [SerializeField]  GameEvents events;

    private RectTransform _rect;
    public RectTransform Rect
    {
        get
        {
            if (_rect == null)
            {
                _rect = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            }
            return _rect;
        }
    }
    private int _anserIndex = -1;
    public int AnswerIndex { get { return _anserIndex; } }

    private bool Checked = false;
    public void UpdateData(string info, int index)
    {
        infoTextObject.text = info;
        _anserIndex = index;
        
    }
    public void Reset()
    {
        Checked = false;
        UpdateUI();
    }
    public void SwitchState()
    {
        Checked = !Checked;
        UpdateUI();

        if (events.UpdateQuestionAnswer != null)
        {
            events.UpdateQuestionAnswer(this);
        }
       
    }
    void UpdateUI()
    {
        toggle.sprite = Checked ? checkToggle : uncheckToggle;
    }
}
