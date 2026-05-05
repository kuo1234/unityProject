using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int score;
    public int mistakes;
    public string lastFeedbackMessage = "Sort trash into the matching bins.";

    public event Action<int, int> ScoreChanged;
    public event Action<string, bool> FeedbackRaised;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (SceneManager.GetActiveScene().name == "GameScene" && GetComponent<GameSessionManager>() == null)
        {
            gameObject.AddComponent<GameSessionManager>();
        }
    }

    public void AddSortedItem()
    {
        score++;
        RaiseFeedback("Correct sort", true);
        ScoreChanged?.Invoke(score, mistakes);
    }

    public void AddSortingMistake(string reason = "Wrong bin")
    {
        mistakes++;
        RaiseFeedback(reason, false);
        ScoreChanged?.Invoke(score, mistakes);
    }

    public void ResetScore()
    {
        score = 0;
        mistakes = 0;
        lastFeedbackMessage = "Sort trash into the matching bins.";
        ScoreChanged?.Invoke(score, mistakes);
    }

    public void RaiseFeedback(string message, bool positive)
    {
        lastFeedbackMessage = message;
        FeedbackRaised?.Invoke(message, positive);
    }
}
