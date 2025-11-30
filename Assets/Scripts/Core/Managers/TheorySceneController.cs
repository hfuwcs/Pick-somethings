using UnityEngine;
using UnityEngine.UI;

public class TheorySceneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button backButton;
    [SerializeField] private SceneController sceneController;

    private void Start()
    {
        if (LessonContentLoader.Instance != null)
        {
            Debug.Log($"[TheoryScene] Đang tải bài học ID: {GameSettings.SelectedLessonId}");
            LessonContentLoader.Instance.LoadLessonContent(GameSettings.SelectedLessonId);
        }
        else
        {
            Debug.LogError("[TheoryScene] Không tìm thấy LessonContentLoader!");
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private void OnBackButtonClicked()
    {
        if (sceneController != null)
        {
            sceneController.ReturnToMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}