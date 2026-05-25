using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct LearnCardInfo
{
    public string title;
    public string body;
    public Color accentColor;

    public LearnCardInfo(string title, string body, Color accentColor)
    {
        this.title = title;
        this.body = body;
        this.accentColor = accentColor;
    }
}

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
        "You helped the Earth!",
        "Good matching!",
        "Keep going!"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }

            return;
        }

        Instance = this;

        if (SceneManager.GetActiveScene().name == "GameScene" && GetComponent<GameSessionManager>() == null)
        {
            gameObject.AddComponent<GameSessionManager>();
        }
    }

    public void AddSortedItem(TrashItem sortedItem = null)
    {
        score++;
        streak++;
        bestStreak = Mathf.Max(bestStreak, streak);
        UpdateStars();
        RaiseFeedback(GetPositiveMessage(sortedItem), true);
        ScoreChanged?.Invoke(score, mistakes);
    }

    public void AddSortingMistake(string reason = "Wrong bin")
    {
        mistakes++;
        streak = 0;
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

    public static string GetChildCategoryName(TrashCategory category)
    {
        return category switch
        {
            TrashCategory.General => "general",
            TrashCategory.Recyclable => "recycle",
            TrashCategory.Food => "food",
            _ => "matching"
        };
    }

    public static string GetChildBinName(TrashCategory category)
    {
        return category switch
        {
            TrashCategory.General => "green general",
            TrashCategory.Recyclable => "blue recycle",
            TrashCategory.Food => "orange food",
            _ => "matching"
        };
    }

    private string GetPositiveMessage(TrashItem sortedItem)
    {
        if (streak > 0 && streak % 5 == 0)
        {
            return "Super sorter! You are helping Earth.";
        }

        if (streak > 0 && streak % 3 == 0)
        {
            return $"{streak} in a row! Keep cleaning.";
        }

        if (sortedItem != null)
        {
            return GetEducationalSuccessMessage(sortedItem);
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
            return "Wash first, then sort it.";
        }

        if (reason.StartsWith("Wrong bin: try ", StringComparison.OrdinalIgnoreCase))
        {
            return "Try the " + reason.Substring("Wrong bin: try ".Length) + " bin.";
        }

        return reason;
    }

    private static string GetEducationalSuccessMessage(TrashItem sortedItem)
    {
        if (TryGetLearnCardInfo(sortedItem, out LearnCardInfo info))
        {
            return info.title.Replace(" -> ", " goes in ") + ". " + info.body;
        }

        return "Nice sort! You helped the Earth.";
    }

    public static bool TryGetLearnCardInfo(TrashItem sortedItem, out LearnCardInfo info)
    {
        info = default;
        if (sortedItem == null)
        {
            return false;
        }

        string itemName = GetFriendlyItemName(sortedItem);
        switch (sortedItem.itemType)
        {
            case TrashCategory.Recyclable:
                info = new LearnCardInfo(
                    itemName + " -> Recycle",
                    "It can be used again.",
                    new Color(0.1f, 0.45f, 0.95f));
                return true;
            case TrashCategory.Food:
                info = new LearnCardInfo(
                    itemName + " -> Food Waste",
                    "It can become compost.",
                    new Color(0.95f, 0.48f, 0.12f));
                return true;
            case TrashCategory.General:
                info = new LearnCardInfo(
                    itemName + " -> General Trash",
                    "It cannot be recycled.",
                    new Color(0.2f, 0.75f, 0.35f));
                return true;
            default:
                return false;
        }
    }

    private static string GetFriendlyItemName(TrashItem item)
    {
        if (item == null)
        {
            return "That";
        }

        string name = item.gameObject.name.ToLowerInvariant();
        if (name.Contains("bento"))
        {
            return "Bento box";
        }

        if (name.Contains("crumpled") || name.Contains("paper"))
        {
            return "Crumpled paper";
        }

        if (name.Contains("fish"))
        {
            return "Fish bone";
        }

        if (name.Contains("chicken") || name.Contains("humerus"))
        {
            return "Chicken bone";
        }

        if (name.Contains("water") && name.Contains("bottle"))
        {
            return "Water bottle";
        }

        if (name.Contains("plastic") && name.Contains("bottle"))
        {
            return "Plastic bottle";
        }

        if (name.Contains("banana"))
        {
            return "Banana peel";
        }

        if (name.Contains("broccoli"))
        {
            return "Broccoli";
        }

        if (name.Contains("donut"))
        {
            return "Food scrap";
        }

        if (name.Contains("carton") || name.Contains("tetrapak"))
        {
            return "Carton";
        }

        if (name.Contains("can"))
        {
            return "Can";
        }

        if (name.Contains("cardboard"))
        {
            return "Cardboard";
        }

        if (name.Contains("paperbag") || name.Contains("paper bag"))
        {
            return "Paper bag";
        }

        if (name.Contains("toiletpaper"))
        {
            return "Toilet paper";
        }

        if (name.Contains("snack"))
        {
            return "Snack pack";
        }

        return "That";
    }
}
