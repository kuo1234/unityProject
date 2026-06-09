using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle hintStyle;

    private bool started;
    private bool previousXrPressed;

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
    {
        CreateWorldPrompt();
    }

    private void Update()
    {
        if (!started && WasStartPressed())
        {
            StartGame();
        }
    }

    // VR / Simulator 中看得到的 3D 開始提示(OnGUI 在 VR 視角不顯示,故另建世界空間文字)
    private void CreateWorldPrompt()
    {
        Camera cam = Camera.main;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 origin = cam != null ? cam.transform.position : Vector3.zero;
        Vector3 pos = origin + forward * 4f + Vector3.up * 0.2f;

        GameObject prompt = new GameObject("StartPrompt_World");
        prompt.transform.position = pos;
        // 面向玩家:沿視線方向 +180°,並以 x=-1 翻正鏡像(同記分板做法)
        prompt.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
        prompt.transform.localScale = new Vector3(-1f, 1f, 1f);

        TextMesh tm = prompt.AddComponent<TextMesh>();
        tm.text = "SORTING GAME\n\nPress SPACE / Trigger\nto Start";
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.1f;
        tm.fontSize = 64;
        tm.color = new Color(0.9f, 0.95f, 1f);
    }

    private bool WasStartPressed()
    {
        // 鍵盤 / 滑鼠(桌面與 Simulator)
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            return true;
        }
#endif

        // OVR 控制器(若 Meta XR runtime 啟用)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.All) ||
            OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.All) ||
            OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.All))
        {
            return true;
        }

        // Unity XR 控制器(邊緣偵測)
        bool xrPressed = IsXrTriggerPressed();
        bool xrDown = xrPressed && !previousXrPressed;
        previousXrPressed = xrPressed;
        return xrDown;
    }

    private bool IsXrTriggerPressed()
    {
        foreach (XRNode node in new[] { XRNode.RightHand, XRNode.LeftHand })
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
            {
                continue;
            }

            if ((device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool trigger) && trigger) ||
                (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool primary) && primary))
            {
                return true;
            }
        }

        return false;
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
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 88f, panelRect.width - 48f, 42f), "Press SPACE / Trigger or click Start.", hintStyle);

        Rect startButtonRect = new Rect(panelRect.x + 96f, panelRect.y + 154f, panelRect.width - 192f, 64f);
        if (GUI.Button(startButtonRect, "Start", buttonStyle))
        {
            StartGame();
        }
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
