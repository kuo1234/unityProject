using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int score;
    public int mistakes;
    public PlayerTrashInteractor interactor;

    private GUIStyle labelStyle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddSortedItem()
    {
        score++;
    }

    public void AddSortingMistake()
    {
        mistakes++;
    }

    private void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                normal = { textColor = Color.white }
            };
        }

        GUI.Label(new Rect(20f, 20f, 320f, 40f), $"Score: {score}", labelStyle);
        GUI.Label(new Rect(20f, 60f, 320f, 40f), $"Mistakes: {mistakes}", labelStyle);
        GUI.Label(new Rect(20f, 100f, 920f, 40f), "Meta XR keys: T=index trigger, U=grip, B=A/X pick/drop. N=B/Y or I=thumbstick throws.", labelStyle);
        if (interactor == null)
        {
            interactor = FindFirstObjectByType<PlayerTrashInteractor>();
        }

        if (interactor != null)
        {
            GUI.Label(new Rect(20f, 140f, 1100f, 40f), interactor.GetDebugStatus(), labelStyle);
        }

        GUI.Label(new Rect((Screen.width * 0.5f) - 10f, (Screen.height * 0.5f) - 18f, 40f, 40f), "+", labelStyle);
    }
}
