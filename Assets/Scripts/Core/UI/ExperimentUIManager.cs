// File: Assets/Scripts/UI/ExperimentUIManager.cs
using UnityEngine;
using UnityEngine.UI;

public class ExperimentUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject hudPanel;       // UI khi đang chơi (Crosshair, nút Menu, Tooltip)
    [SerializeField] private GameObject theoryPanel;    // Panel Lý thuyết
    [SerializeField] private GameObject quizPanel;      // Panel Kiểm tra
    [SerializeField] private GameObject menuPanel;      // Panel Menu con (để chọn Lý thuyết/Kiểm tra/Về MainMenu)
    [SerializeField] private MonoBehaviour mouseLookScript;

    [Header("Components")]
    [SerializeField] private InteractionController playerInteraction;

    private bool _isPaused = false;

    private void Start()
    {
        // Mặc định khi vào Scene: Hiện HUD, ẩn các cái khác
        ShowPracticeMode();
    }

    // --- CHỨC NĂNG CHUYỂN ĐỔI ---

    public void ToggleMenu()
    {
        if (_isPaused) ShowPracticeMode(); // Đang mở menu thì đóng lại
        else ShowMenu(); // Đang chơi thì mở menu
    }

    public void ShowMenu()
    {
        SetPauseState(true);
        hudPanel.SetActive(false);
        theoryPanel.SetActive(false);
        quizPanel.SetActive(false);

        menuPanel.SetActive(true); // Hiện bảng lựa chọn
    }

    public void ShowTheory()
    {
        SetPauseState(true);
        hudPanel.SetActive(false);
        menuPanel.SetActive(false);
        quizPanel.SetActive(false);

        theoryPanel.SetActive(true);
    }

    public void ShowQuiz()
    {
        SetPauseState(true);
        hudPanel.SetActive(false);
        menuPanel.SetActive(false);
        theoryPanel.SetActive(false);

        quizPanel.SetActive(true);
    }

    public void ShowPracticeMode()
    {
        SetPauseState(false);
        theoryPanel.SetActive(false);
        quizPanel.SetActive(false);
        menuPanel.SetActive(false);

        hudPanel.SetActive(true);
    }

    private void SetPauseState(bool pause)
    {
        _isPaused = pause;
        

        Time.timeScale = pause ? 0f : 1f;

        if (playerInteraction != null)
        {
            playerInteraction.SetUIMode(pause);
        }

        if (mouseLookScript != null)
        {
            mouseLookScript.enabled = !pause;
        }
    }
}