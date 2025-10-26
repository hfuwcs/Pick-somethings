using System;
using UnityEngine;

public abstract class ExperimentManagerBase : MonoBehaviour
{
    /// <summary>
    /// Định nghĩa các trạng thái có thể có của một thí nghiệm.
    /// </summary>
    public enum ExperimentState
    {
        PreExperiment,
        Running,
        Paused,
        PostExperiment  // Thí nghiệm đã kết thúc, chờ hiển thị kết quả hoặc reset.
    }

    #region Events
    public static event Action<ExperimentManagerBase> OnExperimentStarted;
    public static event Action<ExperimentManagerBase> OnExperimentEnded;
    public static event Action<ExperimentManagerBase> OnExperimentReset;
    #endregion

    /// <summary>
    /// Trạng thái hiện tại của thí nghiệm. Chỉ có thể được thay đổi bởi class này hoặc các class con.
    /// </summary>
    public ExperimentState CurrentState { get; protected set; } = ExperimentState.PreExperiment;

    #region Public Control Methods
    /// <summary>
    /// Bắt đầu thí nghiệm. Chỉ hoạt động khi đang ở trạng thái PreExperiment.
    /// </summary>
    public void BeginExperiment()
    {
        if (CurrentState != ExperimentState.PreExperiment) return;

        CurrentState = ExperimentState.Running;
        StartExperimentLogic();
        OnExperimentStarted?.Invoke(this);
        Debug.Log($"Thí nghiệm '{this.GetType().Name}' đã bắt đầu.");
    }

    public void EndExperiment()
    {
        if (CurrentState != ExperimentState.Running && CurrentState != ExperimentState.Paused) return;

        CurrentState = ExperimentState.PostExperiment;
        EndExperimentLogic();
        OnExperimentEnded?.Invoke(this);
        Debug.Log($"Thí nghiệm '{this.GetType().Name}' đã kết thúc.");
    }

    public void ResetExperiment()
    {
        CurrentState = ExperimentState.PreExperiment;
        ResetExperimentLogic();
        if (Time.timeScale == 0) Time.timeScale = 1f;
        OnExperimentReset?.Invoke(this);
        Debug.Log($"Thí nghiệm '{this.GetType().Name}' đã được reset.");
    }

    public void TogglePause()
    {
        if (CurrentState == ExperimentState.Running)
        {
            CurrentState = ExperimentState.Paused;
            PauseExperimentLogic();
            Debug.Log("Thí nghiệm đã tạm dừng.");
        }
        else if (CurrentState == ExperimentState.Paused)
        {
            CurrentState = ExperimentState.Running;
            ResumeExperimentLogic();
            Debug.Log("Thí nghiệm đã tiếp tục.");
        }
    }
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        InitializeExperiment();
    }
    #endregion

    #region Abstract & Virtual Methods (Contract for child classes)
    protected abstract void InitializeExperiment();

    /// <summary>
    /// Logic cụ thể khi thí nghiệm bắt đầu.
    /// </summary>
    protected abstract void StartExperimentLogic();

    /// <summary>
    /// Logic cụ thể khi thí nghiệm kết thúc (ví dụ: dọn dẹp).
    /// </summary>
    protected abstract void EndExperimentLogic();

    protected abstract void ResetExperimentLogic();

    /// <summary>
    /// Logic khi tạm dừng thí nghiệm. Mặc định là đóng băng thời gian.
    /// Có thể được override để thêm hành vi khác (hiển thị menu pause...).
    /// </summary>
    protected virtual void PauseExperimentLogic()
    {
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Logic khi tiếp tục thí nghiệm. Mặc định là khôi phục thời gian.
    /// </summary>
    protected virtual void ResumeExperimentLogic()
    {
        Time.timeScale = 1f;
    }
    #endregion
}