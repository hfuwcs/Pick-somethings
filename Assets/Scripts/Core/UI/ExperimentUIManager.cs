using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ExperimentUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject hudPanel;       // UI khi đang chơi (Crosshair, nút Menu, Tooltip)
    [SerializeField] private GameObject theoryPanel;    // Panel Lý thuyết
    [SerializeField] private GameObject quizPanel;      // Panel Kiểm tra
    [SerializeField] private GameObject menuPanel;      // Panel Menu con 
    [SerializeField] private MonoBehaviour mouseLookScript;

    [Header("Components")]
    [SerializeField] private InteractionController playerInteraction;

    private bool _isPaused = false;

    private void Update()
    {

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }


    private void Start()
    {
        ShowPracticeMode();
    }

    public void ToggleMenu()
    {
        if (theoryPanel.activeSelf || quizPanel.activeSelf)
        {
            ShowMenu(); 
        }
        else if (menuPanel.activeSelf)
        {
            ShowPracticeMode();
        }
        else
        {
            ShowMenu();
        }
    }

    public void ShowMenu()
    {
        SetPauseState(true);
        hudPanel.SetActive(false);
        theoryPanel.SetActive(false);
        quizPanel.SetActive(false);

        menuPanel.SetActive(true);
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