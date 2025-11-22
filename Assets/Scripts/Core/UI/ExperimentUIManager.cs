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
        int currentLessonId = 12;
        
        if (LessonContentLoader.Instance != null)
        {
            Debug.Log("[UI] Requesting lesson content load...");
            LessonContentLoader.Instance.LoadLessonContent(currentLessonId);
        }
        else
        {
            Debug.LogError("[UI] LessonContentLoader not found!");
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