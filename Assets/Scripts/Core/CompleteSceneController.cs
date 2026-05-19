using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class CompleteSceneController : MonoBehaviour
{
    private const int PanelWidth = 480;
    private const int PanelHeight = 390;
    private const float WorldPanelDistance = 2.6f;
    private static readonly Vector2 WorldPanelSize = new Vector2(760f, 520f);

    private SceneLoader sceneLoader;
    private GUIStyle titleStyle;
    private GUIStyle statsStyle;
    private GUIStyle buttonStyle;
    private Canvas worldCanvas;
    private Text worldTitleText;
    private Text worldResultsText;
    private bool previousAgainPressed;
    private bool previousMenuPressed;
    private bool sceneLoadRequested;

    private void Awake()
    {
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null)
        {
            sceneLoader = gameObject.AddComponent<SceneLoader>();
        }
    }

    private void Start()
    {
        EnsureWorldPanel();
        UpdateWorldPanel();
        previousAgainPressed = IsAgainPressed();
        previousMenuPressed = IsMenuPressed();
    }

    private void LateUpdate()
    {
        HandleControllerShortcuts();
        UpdateWorldPanel();
        PositionWorldPanel();
    }

    private void OnGUI()
    {
        if (ShouldUseWorldPanel())
        {
            return;
        }

        EnsureStyles();

        Rect panelRect = new Rect(
            (Screen.width - PanelWidth) * 0.5f,
            (Screen.height - PanelHeight) * 0.5f,
            PanelWidth,
            PanelHeight);

        GUI.Box(panelRect, string.Empty);
        GUILayout.BeginArea(new Rect(panelRect.x + 28f, panelRect.y + 24f, panelRect.width - 56f, panelRect.height - 48f));
        GUILayout.Label(GetTitleText(), titleStyle);
        GUILayout.Space(18f);
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

    private bool ShouldUseWorldPanel()
    {
        return XRSettings.isDeviceActive;
    }

    private void HandleControllerShortcuts()
    {
        if (sceneLoadRequested)
        {
            return;
        }

        bool againPressed = IsAgainPressed();
        bool menuPressed = IsMenuPressed();

        if (againPressed && !previousAgainPressed)
        {
            sceneLoadRequested = true;
            sceneLoader.LoadGameScene();
            return;
        }

        if (menuPressed && !previousMenuPressed)
        {
            sceneLoadRequested = true;
            sceneLoader.LoadMenuScene();
            return;
        }

        previousAgainPressed = againPressed;
        previousMenuPressed = menuPressed;
    }

    private bool IsAgainPressed()
    {
        return IsXRButtonPressed(CommonUsages.primaryButton) ||
               IsXRButtonPressed(CommonUsages.triggerButton) ||
               IsInputSystemButtonPressed("primaryButton") ||
               IsInputSystemButtonPressed("triggerPressed");
    }

    private bool IsMenuPressed()
    {
        return IsXRButtonPressed(CommonUsages.secondaryButton) ||
               IsXRButtonPressed(CommonUsages.menuButton) ||
               IsXRButtonPressed(CommonUsages.primary2DAxisClick) ||
               IsInputSystemButtonPressed("secondaryButton") ||
               IsInputSystemButtonPressed("menuButton") ||
               IsInputSystemButtonPressed("primary2DAxisClick");
    }

    private bool IsXRButtonPressed(InputFeatureUsage<bool> button)
    {
        InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftController.isValid &&
            leftController.TryGetFeatureValue(button, out bool leftPressed) &&
            leftPressed)
        {
            return true;
        }

        InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        return rightController.isValid &&
               rightController.TryGetFeatureValue(button, out bool rightPressed) &&
               rightPressed;
    }

    private bool IsInputSystemButtonPressed(string controlName)
    {
#if ENABLE_INPUT_SYSTEM
        foreach (UnityEngine.InputSystem.InputDevice device in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (device is not UnityEngine.InputSystem.XR.XRController controller)
            {
                continue;
            }

            UnityEngine.InputSystem.Controls.ButtonControl button = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>(controlName);
            if (button != null && button.isPressed)
            {
                return true;
            }
        }
#endif

        return false;
    }

    private void EnsureWorldPanel()
    {
        if (worldCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("XR Result Canvas");
        worldCanvas = canvasObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 20;

        RectTransform canvasTransform = worldCanvas.GetComponent<RectTransform>();
        canvasTransform.sizeDelta = WorldPanelSize;
        canvasTransform.localScale = Vector3.one * 0.002f;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 12f;
        canvasObject.AddComponent<GraphicRaycaster>();

        Image panel = CreatePanel(canvasTransform);
        worldTitleText = CreateText(panel.rectTransform, "Title", 34, FontStyle.Bold, new Vector2(0f, 162f), new Vector2(680f, 72f));
        worldResultsText = CreateText(panel.rectTransform, "Results", 26, FontStyle.Normal, new Vector2(0f, 10f), new Vector2(680f, 235f));

        CreateButton(panel.rectTransform, "Again", new Vector2(-150f, -190f), sceneLoader.LoadGameScene);
        CreateButton(panel.rectTransform, "Menu", new Vector2(150f, -190f), sceneLoader.LoadMenuScene);

        PositionWorldPanel();
        worldCanvas.gameObject.SetActive(ShouldUseWorldPanel());
    }

    private Image CreatePanel(RectTransform parent)
    {
        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = WorldPanelSize;

        Image image = panelObject.AddComponent<Image>();
        image.color = new Color(0.07f, 0.08f, 0.1f, 0.92f);
        return image;
    }

    private Text CreateText(RectTransform parent, string objectName, int fontSize, FontStyle fontStyle, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void CreateButton(RectTransform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(220f, 62f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.15f, 0.42f, 0.82f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.22f, 0.55f, 1f, 1f);
        colors.pressedColor = new Color(0.08f, 0.28f, 0.62f, 1f);
        button.colors = colors;

        Text buttonText = CreateText(rectTransform, "Label", 26, FontStyle.Bold, Vector2.zero, rectTransform.sizeDelta);
        buttonText.text = label;
    }

    private void UpdateWorldPanel()
    {
        if (worldCanvas == null)
        {
            return;
        }

        worldCanvas.gameObject.SetActive(ShouldUseWorldPanel());
        worldTitleText.text = GetTitleText();
        worldResultsText.text = GetResultsText();
    }

    private void PositionWorldPanel()
    {
        if (worldCanvas == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Transform canvasTransform = worldCanvas.transform;
        Transform cameraTransform = camera.transform;
        canvasTransform.SetParent(cameraTransform, false);
        canvasTransform.localPosition = new Vector3(0f, 0f, WorldPanelDistance);
        canvasTransform.localRotation = Quaternion.identity;
    }

    private string GetResultsText()
    {
        if (!RoundResultStore.HasResult)
        {
            return "Stars: 0/3\nItems rescued: 0\nBest streak: 0\nAccuracy: 0%";
        }

        return
            $"Stars: {RoundResultStore.Stars}/3\n" +
            $"Items rescued: {RoundResultStore.Score}\n" +
            $"Best streak: {RoundResultStore.BestStreak}\n" +
            $"Mistakes: {RoundResultStore.Mistakes}\n" +
            $"Accuracy: {RoundResultStore.Accuracy}%\n" +
            GetPraiseText();
    }

    private string GetTitleText()
    {
        if (!RoundResultStore.HasResult)
        {
            return "Round Complete";
        }

        return RoundResultStore.Stars >= 3 ? "Super Sorter!" : "Great Sorting!";
    }

    private string GetPraiseText()
    {
        if (RoundResultStore.Stars >= 3)
        {
            return "You made the planet shine!";
        }

        if (RoundResultStore.Stars >= 2)
        {
            return "Great job helping the Earth!";
        }

        if (RoundResultStore.Score > 0)
        {
            return "Nice work. Try for more stars!";
        }

        return "Give it another try!";
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
