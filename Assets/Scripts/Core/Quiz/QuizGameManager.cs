using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

public class QuizGameManager : MonoBehaviour
{
    private Data data = null;

    [SerializeField] GameEvents events = null;

    [SerializeField] Animator timerAnimator = null;
    [SerializeField] TextMeshProUGUI timerText = null;
    [SerializeField] Color timerHalfWayOutColor = Color.yellow;
    [SerializeField] Color timerAlmostOutColor = Color.red;

    private List<AnswerData> PickedAnswer = new List<AnswerData>();
    private List<int> FinishedQuestions = new List<int>();
    private int currentQuestion = 0;

    private int timerStateParaHash = 0;

    private IEnumerator IE_WaitTillNextRound = null;
    private IEnumerator IE_StartTimer = null;
    private Color timerDefaultColor = Color.white;
    private bool IsFinished
    {
        get
        {
            return (FinishedQuestions.Count < data.Questions.Length) ? false : true;     
        }
    }

    void OnEnable()
    {
        events.UpdateQuestionAnswer += UpdateAnswer;
    }
    void OnDisable()
    {
        events.UpdateQuestionAnswer -= UpdateAnswer;
    }

    private void Awake()
    {
        events.CurrentFinalScore = 0;
    }
    void Start()
    {
        Time.timeScale = 1f;
        events.StartupHighscore = PlayerPrefs.GetInt(GameUtility.SavePrefKey);

        timerDefaultColor = timerText.color;
        LoadQuestions();


        timerStateParaHash = Animator.StringToHash("TimerState");

        var seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);

        Display();
    }

    public void UpdateAnswer(AnswerData newAnswer)
    {
        if (data.Questions[currentQuestion].Type == AnswerType.Single)
        {
            foreach (var answer in PickedAnswer)
            {
                if (answer != newAnswer)
                {
                    answer.Reset();
                }
            }
            PickedAnswer.Clear();
            PickedAnswer.Add(newAnswer);
        }
        else
        {
            bool alreadyPicked = PickedAnswer.Exists(x => x == newAnswer);
            if (!alreadyPicked)
            {
                PickedAnswer.Add(newAnswer);
            }
            else
            {
                PickedAnswer.Remove(newAnswer);
            }
        }
    }

    public void EraseAnswer()
    {
        PickedAnswer = new List<AnswerData>();
    }

    public void Accept()
    {
        UpdateTimer(false);
        bool isCorrect = CheckAnswer();
        FinishedQuestions.Add(currentQuestion);

        UpdateScore((isCorrect) ? data.Questions[currentQuestion].AddScore : 0);

        if (IsFinished)
        {
            SetHighscore();
        }

        var type = (IsFinished) ? QuizUIManager.ResolutionScreenType.Finish : (isCorrect) ? QuizUIManager.ResolutionScreenType.Correct : QuizUIManager.ResolutionScreenType.Incorrect;

        if (events.DisplayResolutionScreen != null)
        {
            events.DisplayResolutionScreen(type, data.Questions[currentQuestion].AddScore);
        }

        QuizAudioManager.Instance.PlaySound((isCorrect) ? "CorrectSFX":"IncorrectSFX");

        if (type != QuizUIManager.ResolutionScreenType.Finish)
        {
            if (IE_WaitTillNextRound != null)
            {
                StopCoroutine(IE_WaitTillNextRound);
            }
            IE_WaitTillNextRound = WaitTillNextRound();
            StartCoroutine(IE_WaitTillNextRound);
        }

    }

    void UpdateTimer(bool state)
    {
        switch (state)
        {
            case true:
                IE_StartTimer = StartTimer();
                StartCoroutine(IE_StartTimer);

                timerAnimator.SetInteger(timerStateParaHash, 2);
                break;
            case false:
                if (IE_StartTimer != null)
                {
                    StopCoroutine(IE_StartTimer);
                }
                QuizAudioManager.Instance.StopSound("CountdownSFX");
                break;
        }
    }
    IEnumerator StartTimer()
    {
        var totalTime = data.Questions[currentQuestion].Timer;
        var timeLeft = totalTime;

        timerText.color = timerDefaultColor;
        while (timeLeft > 0)
        {
            timeLeft--;

            if (timeLeft < totalTime / 2 && timeLeft >= totalTime / 4)
            {
                QuizAudioManager.Instance.PlaySound("CountdownSFX");
                timerText.color = timerHalfWayOutColor;
            }
            if (timeLeft <= totalTime / 4)
            {
                QuizAudioManager.Instance.PlaySound("CountdownSFX");
                timerText.color = timerAlmostOutColor;

            }
            timerText.text = timeLeft.ToString();
            yield return new WaitForSeconds(1f);
        }
        Accept();
    }
    IEnumerator WaitTillNextRound()
    {
        yield return new WaitForSeconds(GameUtility.ResolutionDelayTime);
        Display();
    }

    void Display()
    {
        EraseAnswer();
        var question = GetRandomQuestion();

        if (events.UpdateQuestionUI != null)
        {
            events.UpdateQuestionUI(question);
        }
        else
        {
            Debug.LogWarning("Ups! Something went wrong while trying to display new Question UI Data.");
        }
        if (question.UseTimer)
        {
            UpdateTimer(question.UseTimer);
        }

    }

    Question GetRandomQuestion()
    {
        var randomIndex = GetRandomQuestionIndex();
        currentQuestion = randomIndex;

        return data.Questions[currentQuestion];
    }
    int GetRandomQuestionIndex()
    {
        var random = -1;
        if (FinishedQuestions.Count < data.Questions.Length)
        {
            do
            {
                random = Random.Range(0, data.Questions.Length);
            } while (FinishedQuestions.Contains(random) || random == currentQuestion);
        }
        return random;
    }
    bool CheckAnswer()
    {
        if (!CompareAnswers())
        {
            return false;
        }
        return true;
    }
    bool CompareAnswers()
    {
        if (PickedAnswer.Count > 0)
        {
            List<int> c = data.Questions[currentQuestion].GetCorrectAnswer();
            List<int> p = PickedAnswer.Select(x => x.AnswerIndex).ToList();

            var f = c.Except(p).ToList();
            var s = p.Except(c).ToList();

            return !f.Any() && !s.Any();
        }
        return false;
    }
    void LoadQuestions()
    {
        // 1. Lấy toàn bộ dữ liệu từ XML
        var fullData = Data.Fetch();

        // 2. Kiểm tra xem người chơi có chọn chủ đề nào không
        // Nếu SelectedCategory rỗng hoặc là "All" -> Lấy hết câu hỏi
        if (string.IsNullOrEmpty(GameSettings.SelectedCategory) || GameSettings.SelectedCategory == "All")
        {
            data = fullData;
        }
        else
        {
            // 3. LỌC DỮ LIỆU: Chỉ lấy câu nào có Category trùng với lựa chọn
            data = new Data(); // Tạo data mới để chứa kết quả lọc
            
            data.Questions = fullData.Questions
                .Where(q => q.Category == GameSettings.SelectedCategory)
                .ToArray();
        }

        // 4. Kiểm tra lỗi nếu không tìm thấy câu nào (ví dụ nhập sai tên)
        if (data.Questions == null || data.Questions.Length == 0)
        {
            Debug.LogError("Không tìm thấy câu hỏi nào thuộc chủ đề: " + GameSettings.SelectedCategory);
            // Fallback: Nếu lỗi thì load tất cả để game không bị đơ
            data = fullData;
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void LoadData()
    {
        data = Data.Fetch();
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu");
        
    }
    private void SetHighscore()
    {
        var highscore = PlayerPrefs.GetInt(GameUtility.SavePrefKey);
        if(highscore < events.CurrentFinalScore)
        {
           PlayerPrefs.SetInt(GameUtility.SavePrefKey, events.CurrentFinalScore);
        }
    }

    private void UpdateScore(int add)
    {
        events.CurrentFinalScore += add;
        if(events.CurrentFinalScore < 0)
        {
            events.CurrentFinalScore = 0;
        }
        if (events.ScoreUpdated != null)
        {
            events.ScoreUpdated();
        }
    }
}