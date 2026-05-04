using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle hintStyle;

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnGUI()
    {
        EnsureStyles();

        float panelWidth = Mathf.Min(520f, Screen.width - 48f);
        float panelHeight = 260f;
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);

        GUI.Box(panelRect, GUIContent.none);

        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 28f, panelRect.width - 48f, 54f), "Sorting Game", titleStyle);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 88f, panelRect.width - 48f, 42f), "Press start when you are ready.", hintStyle);

        Rect startButtonRect = new Rect(panelRect.x + 96f, panelRect.y + 154f, panelRect.width - 192f, 64f);
        if (GUI.Button(startButtonRect, "Start", buttonStyle))
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
            fontStyle = FontStyle.Bold
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            normal = { textColor = new Color(0.9f, 0.94f, 1f) }
        };
    }
}
