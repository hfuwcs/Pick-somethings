using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;


[Serializable()]
public struct UIManagerParameters
{
    [Header("Answer Options")]
    [SerializeField] float margins;
    public float Margins { get { return margins; } }

    [Header("Resolution Screen Option")]
    [SerializeField] Color correctBGColor;
    public Color CorrectBGColor { get { return correctBGColor; } }

    [SerializeField] Color incorrectBGColor;
    public Color IncorrectBGColor { get { return incorrectBGColor; } }

    [SerializeField] Color finalBGColor;
    public Color FinalBGColor { get { return finalBGColor; } }
}
[Serializable()]
public struct UIElements
{
    [SerializeField] RectTransform answersContentArea;
    public RectTransform AnswersContentArea { get { return answersContentArea; } }

    [SerializeField] TextMeshProUGUI questionInfoTextObject;
    public TextMeshProUGUI QuestionInfoTextObject { get { return questionInfoTextObject; } }

    [SerializeField] TextMeshProUGUI scoreText;
    public TextMeshProUGUI ScoreText { get { return scoreText; } }
    
    [Space]
    [SerializeField] Animator resolutionScreenAnimator;
    public Animator ResolutionScreenAnimator { get { return resolutionScreenAnimator; } }
    [SerializeField] Image resolutionBG;
    public Image ResolutionBG { get { return resolutionBG; } }
    [SerializeField] TextMeshProUGUI resolutionStateInfoText;
    public TextMeshProUGUI ResolutionStateInfoText { get { return resolutionStateInfoText; } }
    [SerializeField] TextMeshProUGUI resolutionScoreText;
    public TextMeshProUGUI ResolutionScoreText { get { return resolutionScoreText; } }
    [Space]

    [SerializeField] TextMeshProUGUI highScoreText;
    public TextMeshProUGUI HighScoreText { get { return highScoreText; } }
    [SerializeField] CanvasGroup mainCanvasGroup;
    public CanvasGroup MainCanvasGroup { get { return mainCanvasGroup; } }
    [SerializeField] RectTransform finishUIElements;
    public RectTransform FinishUIElements { get { return finishUIElements; } }
}
public class QuizUIManager : MonoBehaviour
{
    public enum ResolutionScreenType { Correct, Incorrect, Finish}

    [Header("References")]
    [SerializeField] GameEvents events;

    [Header("UI Elemens (Prefabs)")]
    [SerializeField] AnswerData answerPrefab;

    [SerializeField] UIElements uiElements;

    [Space]
    [SerializeField] UIManagerParameters parameters;

    List<AnswerData> currenAnswer = new List<AnswerData>();
    private int resStateParaHash = 0;

    private IEnumerator IE_DisplayTimeResolution;
    void OnEnable()
    {
        events.UpdateQuestionUI += UpdateQuestionUI;
        events.DisplayResolutionScreen += DisplayResolution;
        events.ScoreUpdated += UpdateSoreUI;
    }

    void OnDisable()
    {
        events.UpdateQuestionUI -= UpdateQuestionUI;
        events.DisplayResolutionScreen -= DisplayResolution;
        events.ScoreUpdated -= UpdateSoreUI;
    }
    void Start()
    {
        UpdateSoreUI();
        resStateParaHash = Animator.StringToHash("ScreenState");
    }
    void UpdateQuestionUI (Question question)
    {
        uiElements.QuestionInfoTextObject.text = question.Info;
        CreateAnswer(question);
    }

    void DisplayResolution(ResolutionScreenType type, int score)
    {
       UpdateResUI(type, score);
        uiElements.ResolutionScreenAnimator.SetInteger(resStateParaHash, 2);
        uiElements.MainCanvasGroup.blocksRaycasts = false;


        if (type != ResolutionScreenType.Finish)
        {
            if (IE_DisplayTimeResolution != null)
            {
                StopCoroutine(IE_DisplayTimeResolution);
            }
            IE_DisplayTimeResolution = DisplayTimeResolution();
            StartCoroutine(IE_DisplayTimeResolution);
        }
        
    }

    IEnumerator DisplayTimeResolution ()
    {
        yield return new WaitForSeconds(GameUtility.ResolutionDelayTime);
        uiElements.ResolutionScreenAnimator.SetInteger(resStateParaHash, 1);
        uiElements.MainCanvasGroup.blocksRaycasts = true;
    }
    void UpdateResUI(ResolutionScreenType type, int score)
    {
        var highScore = PlayerPrefs.GetInt(GameUtility.SavePrefKey);
        switch (type)
        {
            case ResolutionScreenType.Correct:
                uiElements.ResolutionBG.color = parameters.CorrectBGColor;
                uiElements.ResolutionStateInfoText.text = "Chính xác!";
                uiElements.ResolutionScoreText.text = "+" + score;
                break;
            case ResolutionScreenType.Incorrect:
                uiElements.ResolutionBG.color = parameters.IncorrectBGColor;
                uiElements.ResolutionStateInfoText.text = "Không chính xác!";
                uiElements.ResolutionScoreText.text = "0";
                break;
            case ResolutionScreenType.Finish:
                uiElements.ResolutionBG.color = parameters.FinalBGColor;
                uiElements.ResolutionStateInfoText.text = "Tổng điểm!";
                
                StartCoroutine(CalculateScore());
                uiElements.FinishUIElements.gameObject.SetActive(true);
                uiElements.HighScoreText.gameObject.SetActive(true);
                uiElements.HighScoreText.text = ((highScore > events.StartupHighscore) ? "<color=yellow>new</color>" : string.Empty) + "Điểm cao: " + highScore;
                
                break;
        }
    }

    IEnumerator CalculateScore()
    {
        if (events.CurrentFinalScore == 0)
        {
            uiElements.ResolutionScoreText.text = 0.ToString();
        }

        var scoreValue = 0;
        var scoreMoreThanZero = events.CurrentFinalScore > 0;
        while ((scoreMoreThanZero)? scoreValue < events.CurrentFinalScore: scoreValue > events.CurrentFinalScore)
        {
            scoreValue += scoreMoreThanZero ? 1 : -1;
            uiElements.ResolutionScoreText.text = scoreValue.ToString();

            yield return null;
        }
    }
    void CreateAnswer(Question question)
    {
        EraseAnswer();

        float offset = 0 - parameters.Margins;
        for (int i = 0; i < question.Answers.Length; i++)
        {
            AnswerData newAnswer = (AnswerData)Instantiate(answerPrefab, uiElements.AnswersContentArea);
            newAnswer.UpdateData(question.Answers[i].Info, i);

            newAnswer.Rect.anchoredPosition = new Vector2(0, offset);

            offset -= newAnswer.Rect.sizeDelta.y + parameters.Margins;
            uiElements.AnswersContentArea.sizeDelta = new Vector2(uiElements.AnswersContentArea.sizeDelta.x, -offset);

            currenAnswer.Add(newAnswer);
        }
    }
    void EraseAnswer()
    {
        foreach (var answer in currenAnswer)
        {
            Destroy(answer.gameObject);
        }
        currenAnswer.Clear();
    }

    void UpdateSoreUI()
    {
        uiElements.ScoreText.text = "Score: " + events.CurrentFinalScore;
    }
}
