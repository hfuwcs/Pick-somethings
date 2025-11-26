using UnityEngine;
using Michsky.MUIP;

public class ExperimentNotification : MonoBehaviour
{
    public static ExperimentNotification Instance;

    [Header("Michsky Reference")]
    [SerializeField] private NotificationManager notificationManager; 

    [Header("Icons")]
    [SerializeField] private Sprite iconSuccess;
    [SerializeField] private Sprite iconWarning;
    [SerializeField] private Sprite iconError;

    public enum Type { Success, Warning, Error, Info }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Show(string title, string content, Type type = Type.Info)
    {
        if (notificationManager == null) return;

        notificationManager.title = title;
        notificationManager.description = content;

        switch (type)
        {
            case Type.Success:
                notificationManager.icon = iconSuccess;
                break;
            case Type.Warning:
                notificationManager.icon = iconWarning;
                break;
            case Type.Error:
                notificationManager.icon = iconError;
                break;
            default:
                notificationManager.icon = iconSuccess;
                break;
        }

        notificationManager.UpdateUI();
        notificationManager.Open();
    }
}