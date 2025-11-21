using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;

    private IEnumerator Start()
    {
        if (AppConfig.IsConfigLoaded) yield break;

        if (statusText) statusText.text = "Đang kết nối máy chủ...";

        string url = AppConfig.CONFIG_GIST_URL + "?t=" + System.DateTime.Now.Ticks;

        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                string serverUrl = uwr.downloadHandler.text.Trim();
                
                if (serverUrl.StartsWith("http"))
                {
                    AppConfig.BaseUrl = serverUrl;
                    AppConfig.IsConfigLoaded = true;
                    Debug.Log($"[Config] Server URL set to: {AppConfig.BaseUrl}");
                    if (statusText) statusText.text = "Sẵn sàng";
                }
                else
                {
                    Debug.LogError($"[Config] URL từ Gist không hợp lệ: {serverUrl}");
                    if (statusText) statusText.text = "Lỗi Config";
                }
            }
            else
            {
                Debug.LogError($"[Config] Không lấy được Gist: {uwr.error}");
                if (statusText) statusText.text = "Lỗi Mạng";
            }
        }
    }
}