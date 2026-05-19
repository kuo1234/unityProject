using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int score;
    public int mistakes;
    public int streak;
    public int bestStreak;
    public int stars;
    public string lastFeedbackMessage = "Sort trash into the matching bins.";

    public event Action<int, int> ScoreChanged;
    public event Action<string, bool> FeedbackRaised;
    public event Action<int> StarEarned;

    private static readonly string[] PositiveMessages =
    {
        "Nice sort!",
        "Great job!",
        "Clean planet point!",
        "Super recycling!",
        "You helped the Earth!"
    };

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
        streak++;
        bestStreak = Mathf.Max(bestStreak, streak);
        UpdateStars();
        RaiseFeedback(GetPositiveMessage(), true);
        ScoreChanged?.Invoke(score, mistakes);
    }

    public void AddSortingMistake(string reason = "Wrong bin")
    {
        mistakes++;
        streak = 0;
        UpdateStars();
        RaiseFeedback(GetMistakeMessage(reason), false);
        ScoreChanged?.Invoke(score, mistakes);
    }

    public void ResetScore()
    {
        score = 0;
        mistakes = 0;
        streak = 0;
        bestStreak = 0;
        stars = 0;
        lastFeedbackMessage = "Sort trash into the matching bins.";
        ScoreChanged?.Invoke(score, mistakes);
    }

    public void RaiseFeedback(string message, bool positive)
    {
        lastFeedbackMessage = message;
        FeedbackRaised?.Invoke(message, positive);
    }

    private void UpdateStars()
    {
        int previousStars = stars;
        int earnedStars = 0;

        if (score >= 3)
        {
            earnedStars++;
        }

        if (bestStreak >= 3)
        {
            earnedStars++;
        }

        if (score >= 6 && mistakes <= 2)
        {
            earnedStars++;
        }

        stars = Mathf.Clamp(earnedStars, 0, 3);
        if (stars > previousStars)
        {
            StarEarned?.Invoke(stars);
        }
    }

    private string GetPositiveMessage()
    {
        if (streak > 0 && streak % 5 == 0)
        {
            return "Super sorter!";
        }

        if (streak > 0 && streak % 3 == 0)
        {
            return $"{streak} in a row!";
        }

        return PositiveMessages[score % PositiveMessages.Length];
    }

    private static string GetMistakeMessage(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "Try again!";
        }

        if (reason.Contains("Wash"))
        {
            return "Wash it first";
        }

        if (reason.StartsWith("Wrong bin: try ", StringComparison.OrdinalIgnoreCase))
        {
            return "Try the " + reason.Substring("Wrong bin: try ".Length) + " bin";
        }

        return reason;
    }
}
