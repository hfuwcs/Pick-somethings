using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ExperimentUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject theoryPanel;
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private GameObject menuPanel;
    [Header("Camera Control")]
    [SerializeField] private CinemachineInputAxisController cameraInputController; 
    
    [Header("Components")]
    [SerializeField] private InteractionController playerInteraction;

    private bool _isPaused = false;
    private bool _isCursorMode = false;

    private void Start()
    {
        ShowPracticeMode();
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isCursorMode)
            {
                ToggleCursorOnly(); 
            }
            else
            {
                ToggleMenu();
            }
        }
    }

     public void ToggleCursorOnly()
    {
        if (_isPaused) return;

        _isCursorMode = !_isCursorMode;

        if (_isCursorMode)
        {
            if (cameraInputController != null) 
            {
                cameraInputController.enabled = false; 
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (cameraInputController != null) 
            {
                cameraInputController.enabled = true;
            }
        }

        if (playerInteraction != null)
        {
            playerInteraction.SetUIMode(_isCursorMode);
        }
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
        
        if (LessonContentLoader.Instance != null)
        {
            LessonContentLoader.Instance.LoadLessonContent(12);
        }
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
        _isCursorMode = false;

        Time.timeScale = pause ? 0f : 1f;

        Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = pause;

        if (playerInteraction != null) playerInteraction.SetUIMode(pause);

        if (cameraInputController != null) cameraInputController.enabled = !pause;
    }
}