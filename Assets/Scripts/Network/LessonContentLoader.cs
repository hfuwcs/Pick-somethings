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
                if (Directory.Exists(localPath))
                {
                    try
                    {
                        Directory.Delete(localPath, true);
                        Debug.Log("[Cache] Đã xóa cache cũ.");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[Cache] Lỗi khi xóa cache cũ: {ex.Message}");
                    }
                }

                Directory.CreateDirectory(localPath);
                yield return StartCoroutine(DownloadAndCacheImages(data, localPath));

                PlayerPrefs.SetInt(versionKey, data.version);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.Log("[Data] Phiên bản hiện tại đã mới nhất. Load từ Disk.");
            }

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

            using (UnityWebRequest uwr = UnityWebRequest.Get(imgUrl))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Download Error] {uwr.error}");
                    continue;
                }

                File.WriteAllBytes(filePath, uwr.downloadHandler.data);
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
            //string url = "file://" + filePath;

            if (File.Exists(filePath))
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                
                Texture2D tex = new Texture2D(2, 2);
                
                if (tex.LoadImage(fileData))
                {

                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    
                    tex.name = $"Page_{i}"; 
                    
                    loadedTextures.Add(tex);
                }
            }
            else
            {
                Debug.LogWarning($"[Disk Load] Không tìm thấy file: {filePath}");
            }
            
            yield return null; 
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