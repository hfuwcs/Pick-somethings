using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LessonContentLoader : MonoBehaviour
{
    public static LessonContentLoader Instance { get; private set; }

    [SerializeField] private TheoryDisplayManager uiManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <param name="lessonId">ID bài học trên database</param>
    public void LoadLessonContent(int lessonId)
    {
        StartCoroutine(ProcessLessonRoutine(lessonId));
    }

    private IEnumerator ProcessLessonRoutine(int lessonId)
    {
        string localPath = Path.Combine(Application.persistentDataPath, $"Lessons/{lessonId}");
        
        string url = $"{AppConfig.BaseUrl.TrimEnd('/')}/api/lesson/{lessonId}";
        Debug.Log($"[API] Fetching metadata: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("ngrok-skip-browser-warning", "true");
            webRequest.SetRequestHeader("User-Agent", "UnityGameClient");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[API Error] {webRequest.error}");
                TryLoadOffline(lessonId, localPath);
                yield break;
            }

            // 2. Parse JSON
            string json = webRequest.downloadHandler.text;
            LessonDetail data = JsonUtility.FromJson<LessonDetail>(json);
            
            Debug.Log($"[API] Lesson '{data.title}' - Server Ver: {data.version}");

            string versionKey = $"Lesson_{lessonId}_Version";
            int currentVersion = PlayerPrefs.GetInt(versionKey, -1);

            if (data.version > currentVersion || !Directory.Exists(localPath))
            {
                Debug.Log("[Data] Phát hiện phiên bản mới. Đang tải...");
                yield return StartCoroutine(DownloadAndCacheImages(data, localPath));
                
                PlayerPrefs.SetInt(versionKey, data.version);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.Log("[Data] Phiên bản hiện tại đã mới nhất. Load từ Disk.");
            }

            // 4. Load ảnh từ Disk lên UI
            yield return StartCoroutine(LoadImagesFromDiskToUI(localPath, data.pages.Length));
        }
    }

    private IEnumerator DownloadAndCacheImages(LessonDetail data, string savePath)
    {
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        for (int i = 0; i < data.pages.Length; i++)
        {
            string imgUrl = data.pages[i];
            string fileName = $"page_{i}.jpg";
            string filePath = Path.Combine(savePath, fileName);

            Debug.Log($"[Download] Downloading page {i}: {imgUrl}");

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imgUrl))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Download Error] {uwr.error}");
                    continue;
                }

                // Lấy Texture và lưu xuống đĩa
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                byte[] bytes = texture.EncodeToJPG();
                File.WriteAllBytes(filePath, bytes);
            }
        }
        Debug.Log("[Download] Hoàn tất tải xuống.");
    }

    private IEnumerator LoadImagesFromDiskToUI(string localPath, int pageCount)
    {
        List<Texture2D> loadedTextures = new List<Texture2D>();

        for (int i = 0; i < pageCount; i++)
        {
            string filePath = Path.Combine(localPath, $"page_{i}.jpg");
            string url = "file://" + filePath;

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                    loadedTextures.Add(tex);
                }
                else
                {
                    Debug.LogWarning($"[Disk Load] Không tìm thấy file: {filePath}");
                }
            }
        }

        uiManager.DisplayDownloadedLesson(loadedTextures);
    }

    private void TryLoadOffline(int lessonId, string localPath)
    {
        if (Directory.Exists(localPath))
        {
            int fileCount = Directory.GetFiles(localPath, "page_*.jpg").Length;
            if (fileCount > 0)
            {
                Debug.LogWarning("[Offline Mode] Đang load dữ liệu cũ...");
                StartCoroutine(LoadImagesFromDiskToUI(localPath, fileCount));
            }
        }
    }
}