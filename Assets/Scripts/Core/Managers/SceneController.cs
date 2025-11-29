using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private static string previousSceneName = "";
    
    public static string PreviousSceneName => previousSceneName;
    
    public static bool HasPreviousScene => !string.IsNullOrEmpty(previousSceneName);

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
    }


    private static void SaveCurrentSceneAsPrevious()
    {
        previousSceneName = SceneManager.GetActiveScene().name;
    }

    public void ReturnToPreviousScene()
    {
        if (HasPreviousScene)
        {
            Time.timeScale = 1f;
            string sceneToLoad = previousSceneName;
            previousSceneName = "";
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            ReturnToMainMenu();
        }
    }

    public void LoadSceneWithHistory(string sceneName)
    {
        Time.timeScale = 1f;
        SaveCurrentSceneAsPrevious();
        SceneManager.LoadScene(sceneName);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void PickCategory(string categoryName)
    {
        Time.timeScale = 1f;
        SaveCurrentSceneAsPrevious();
        GameSettings.SelectedCategory = categoryName;
        SceneManager.LoadScene("QuizScene"); 
    }

    public static void ClearSceneHistory()
    {
        previousSceneName = "";
    }
}