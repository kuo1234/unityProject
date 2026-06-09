using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    // 看板放在世界座標固定位置,面向 +Z(玩家 rig 朝 +Z 看);文字用 x=-1 翻正鏡像
    [SerializeField] private Vector3 boardPosition = new Vector3(0f, 1.6f, 0f);

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle hintStyle;

    private bool started;

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
    {
        // 優先使用場景中已存在的看板按鈕(真實物件);找不到才執行時建立(備援)
        GameObject buttonObject = GameObject.Find("StartButton");
        VrClickRelay relay = buttonObject != null ? buttonObject.GetComponent<VrClickRelay>() : null;

        if (relay == null)
        {
            BuildWorldBoard();
            buttonObject = GameObject.Find("StartButton");
            relay = buttonObject != null ? buttonObject.GetComponent<VrClickRelay>() : null;
        }

        if (relay != null)
        {
            relay.onClick = StartGame;
        }
    }

    private void Update()
    {
        // 桌面鍵盤備援(VR 主要用指標點擊 START)
        if (started)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            StartGame();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            StartGame();
        }
#endif
    }

    private void BuildWorldBoard()
    {
        GameObject board = new GameObject("StartBoard_World");
        board.transform.position = boardPosition;
        board.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 面向 -Z(玩家)

        // 背板
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "Background";
        Destroy(bg.GetComponent<Collider>());
        bg.transform.SetParent(board.transform, false);
        bg.transform.localPosition = new Vector3(0f, 0f, -0.04f); // 稍微遠離玩家,當底
        bg.transform.localScale = new Vector3(3.2f, 2f, 1f);
        Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        bgMat.color = new Color(0.06f, 0.08f, 0.12f, 1f);
        bg.GetComponent<Renderer>().sharedMaterial = bgMat;

        CreateText(board.transform, "Title", "SORTING GAME", new Vector3(0f, 0.62f, 0.01f), 0.22f, new Color(0.95f, 0.97f, 1f));
        CreateText(board.transform, "Hint", "Aim at START and pull trigger", new Vector3(0f, 0.2f, 0.01f), 0.1f, new Color(0.7f, 0.78f, 0.9f));

        // START 按鈕(綠色面板 + 碰撞體 + VrClickRelay)
        GameObject button = GameObject.CreatePrimitive(PrimitiveType.Quad);
        button.name = "StartButton";
        button.transform.SetParent(board.transform, false);
        button.transform.localPosition = new Vector3(0f, -0.5f, 0.01f);
        button.transform.localScale = new Vector3(1.5f, 0.6f, 1f);
        Material btnMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        button.GetComponent<Renderer>().sharedMaterial = btnMat;

        // 把 Quad 的對撞體換成稍厚的 BoxCollider 方便雷射命中
        Destroy(button.GetComponent<Collider>());
        BoxCollider box = button.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 1f, 0.2f);

        VrClickRelay relay = button.AddComponent<VrClickRelay>();
        relay.highlightRenderer = button.GetComponent<Renderer>();
        relay.normalColor = new Color(0.20f, 0.75f, 0.35f);
        relay.hoverColor = new Color(0.40f, 1f, 0.55f);
        relay.onClick = StartGame;

        CreateText(button.transform, "Label", "START", new Vector3(0f, 0f, -0.06f), 0.16f, Color.white);
    }

    private void CreateText(Transform parent, string name, string text, Vector3 localPos, float charSize, Color color)
    {
        GameObject t = new GameObject(name);
        t.transform.SetParent(parent, false);
        t.transform.localPosition = localPos;
        t.transform.localRotation = Quaternion.identity;
        t.transform.localScale = new Vector3(-1f, 1f, 1f); // 抵銷父物件 180° 鏡像

        TextMesh tm = t.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = charSize;
        tm.fontSize = 64;
        tm.color = color;
    }

    public void StartGame()
    {
        if (started)
        {
            return;
        }

        started = true;

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
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
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 88f, panelRect.width - 48f, 42f), "Aim at START and pull trigger (or press SPACE).", hintStyle);

        Rect startButtonRect = new Rect(panelRect.x + 96f, panelRect.y + 154f, panelRect.width - 192f, 64f);
        if (GUI.Button(startButtonRect, "Start", buttonStyle))
        {
            StartGame();
        }
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
            fontSize = 20,
            normal = { textColor = new Color(0.9f, 0.94f, 1f) }
        };
    }
}
