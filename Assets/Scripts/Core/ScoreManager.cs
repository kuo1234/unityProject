using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public float roundDurationSeconds = 90f;
    public int score;
    public int mistakes;
    public int correctSorts;
    public int totalSortAttempts;
    public PlayerTrashInteractor interactor;

    public bool IsRoundActive { get; private set; } = true;

    private GUIStyle labelStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle panelStyle;
    private GUIStyle feedbackStyle;
    private float roundTimeRemaining;
    private string feedbackMessage = string.Empty;
    private float feedbackUntilTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        roundTimeRemaining = roundDurationSeconds;
    }

    private void Update()
    {
        if (!IsRoundActive)
        {
            return;
        }

        roundTimeRemaining = Mathf.Max(0f, roundTimeRemaining - Time.deltaTime);
        if (roundTimeRemaining <= 0f)
        {
            EndRound();
        }
    }

    public void AddSortedItem()
    {
        if (!IsRoundActive)
        {
            return;
        }

        score++;
        correctSorts++;
        totalSortAttempts++;
        ShowFeedback("Correct", Color.green);
    }

    public void AddSortingMistake(string reason = "Wrong bin")
    {
        if (!IsRoundActive)
        {
            return;
        }

        mistakes++;
        totalSortAttempts++;
        ShowFeedback(reason, Color.red);
    }

    public void EndRound()
    {
        IsRoundActive = false;
        roundTimeRemaining = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ShowFeedback(string message, Color color)
    {
        feedbackMessage = message;
        feedbackUntilTime = Time.time + 1.25f;

        EnsureStyles();
        if (feedbackStyle != null)
        {
            feedbackStyle.normal.textColor = color;
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUI.Label(new Rect(20f, 20f, 320f, 40f), $"Score: {score}", labelStyle);
        GUI.Label(new Rect(20f, 60f, 320f, 40f), $"Mistakes: {mistakes}", labelStyle);
        GUI.Label(new Rect(20f, 100f, 320f, 40f), $"Time: {Mathf.CeilToInt(roundTimeRemaining)}", labelStyle);
        GUI.Label(new Rect(20f, 140f, 920f, 40f), "Meta XR keys: T=index trigger, U=grip, B=A/X pick/drop. N=B/Y or I=thumbstick throws.", labelStyle);

        if (Time.time < feedbackUntilTime && !string.IsNullOrEmpty(feedbackMessage))
        {
            GUI.Label(new Rect((Screen.width * 0.5f) - 180f, 86f, 360f, 46f), feedbackMessage, feedbackStyle);
        }

        if (interactor == null)
        {
            interactor = FindFirstObjectByType<PlayerTrashInteractor>();
        }

        if (interactor != null)
        {
            GUI.Label(new Rect(20f, 180f, 1100f, 40f), interactor.GetDebugStatus(), labelStyle);
        }

        GUI.Label(new Rect((Screen.width * 0.5f) - 10f, (Screen.height * 0.5f) - 18f, 40f, 40f), "+", labelStyle);

        if (!IsRoundActive)
        {
            DrawGameOverPanel();
        }
    }

    private void DrawGameOverPanel()
    {
        float panelWidth = Mathf.Min(560f, Screen.width - 48f);
        float panelHeight = 320f;
        Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none, panelStyle);

        int accuracy = totalSortAttempts > 0 ? Mathf.RoundToInt((correctSorts / (float)totalSortAttempts) * 100f) : 0;
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 28f, panelRect.width - 48f, 54f), "Round Over", titleStyle);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 94f, panelRect.width - 48f, 116f),
            $"Score: {score}\nCorrect: {correctSorts}\nMistakes: {mistakes}\nAccuracy: {accuracy}%",
            labelStyle);

        Rect restartRect = new Rect(panelRect.x + 64f, panelRect.y + 232f, 190f, 58f);
        Rect menuRect = new Rect(panelRect.x + panelRect.width - 254f, panelRect.y + 232f, 190f, 58f);

        if (GUI.Button(restartRect, "Restart", buttonStyle))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (GUI.Button(menuRect, "Main Menu", buttonStyle))
        {
            SceneManager.LoadScene("PreGame");
        }
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            normal = { textColor = Color.white }
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold
        };

        panelStyle = new GUIStyle(GUI.skin.box);

        feedbackStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }
}
