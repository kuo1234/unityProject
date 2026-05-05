using UnityEngine;

public class CompleteSceneController : MonoBehaviour
{
    private const int PanelWidth = 420;
    private const int PanelHeight = 320;

    private SceneLoader sceneLoader;
    private GUIStyle titleStyle;
    private GUIStyle statsStyle;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null)
        {
            sceneLoader = gameObject.AddComponent<SceneLoader>();
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        Rect panelRect = new Rect(
            (Screen.width - PanelWidth) * 0.5f,
            (Screen.height - PanelHeight) * 0.5f,
            PanelWidth,
            PanelHeight);

        GUI.Box(panelRect, string.Empty);
        GUILayout.BeginArea(new Rect(panelRect.x + 28f, panelRect.y + 24f, panelRect.width - 56f, panelRect.height - 48f));
        GUILayout.Label("Round Complete", titleStyle);
        GUILayout.Space(24f);
        GUILayout.Label(GetResultsText(), statsStyle);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Again", buttonStyle, GUILayout.Height(48f)))
        {
            sceneLoader.LoadGameScene();
        }

        if (GUILayout.Button("Menu", buttonStyle, GUILayout.Height(48f)))
        {
            sceneLoader.LoadMenuScene();
        }

        GUILayout.EndArea();
    }

    private string GetResultsText()
    {
        if (!RoundResultStore.HasResult)
        {
            return "Score: 0\nMistakes: 0\nAccuracy: 0%";
        }

        return
            $"Score: {RoundResultStore.Score}\n" +
            $"Mistakes: {RoundResultStore.Mistakes}\n" +
            $"Accuracy: {RoundResultStore.Accuracy}%";
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
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        statsStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            normal = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22
        };
    }
}
