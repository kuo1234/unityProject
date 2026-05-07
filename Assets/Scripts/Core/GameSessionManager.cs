using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    private const float DashboardTextSize = 0.07f;
    private const float FeedbackTextSize = 0.075f;
    private const float BinLabelTextSize = 0.075f;

    [Header("Round")]
    public float roundDurationSeconds = 180f;
    public string completeSceneName = "CompleteScene";

    [Header("World UI")]
    public TextMesh dashboardText;
    public TextMesh feedbackText;
    public Transform uiAnchor;

    private float remainingSeconds;
    private bool roundActive;
    private ScoreManager scoreManager;
    private TrashSpawner[] spawners;
    private float feedbackClearTime;
    private AudioSource feedbackAudio;
    private AudioClip successClip;
    private AudioClip errorClip;
    private Material generalMaterial;
    private Material recyclableMaterial;
    private Material foodMaterial;
    private Material dirtyMaterial;
    private Material acceptedMaterial;
    private Material rejectedMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
        EnsureDemoContent();
        spawners = FindObjectsByType<TrashSpawner>(FindObjectsInactive.Exclude);
        EnsureWorldUi();
        EnsureAudioFeedback();

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
            scoreManager.FeedbackRaised += OnFeedbackRaised;
        }

        StartRound();
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.FeedbackRaised -= OnFeedbackRaised;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (roundActive)
        {
            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds <= 0f)
            {
                EndRound();
            }
        }

        if (feedbackText != null && feedbackText.gameObject.activeSelf && Time.time >= feedbackClearTime)
        {
            feedbackText.gameObject.SetActive(false);
        }

        UpdateDashboard();
    }

    public void ShowFeedback(string message, bool positive)
    {
        OnFeedbackRaised(message, positive);
    }

    private void StartRound()
    {
        remainingSeconds = roundDurationSeconds;
        roundActive = true;
        SetSpawnersEnabled(true);
    }

    private void EndRound()
    {
        remainingSeconds = 0f;
        roundActive = false;
        SetSpawnersEnabled(false);

        int score = scoreManager != null ? scoreManager.score : 0;
        int mistakes = scoreManager != null ? scoreManager.mistakes : 0;
        int attempts = Mathf.Max(1, score + mistakes);
        int accuracy = Mathf.RoundToInt((score / (float)attempts) * 100f);
        RoundResultStore.Save(score, mistakes, accuracy);

        SceneManager.LoadScene(completeSceneName);
    }

    private void SetSpawnersEnabled(bool enabled)
    {
        if (spawners == null)
        {
            return;
        }

        foreach (TrashSpawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.SetSpawningEnabled(enabled);
            }
        }
    }

    private void OnFeedbackRaised(string message, bool positive)
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = message;
        feedbackText.color = positive ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.35f, 0.25f);
        feedbackText.gameObject.SetActive(true);
        feedbackClearTime = Time.time + 1.8f;

        if (feedbackAudio != null)
        {
            feedbackAudio.PlayOneShot(positive ? successClip : errorClip, 0.35f);
        }
    }

    private void UpdateDashboard()
    {
        if (dashboardText == null)
        {
            return;
        }

        int score = scoreManager != null ? scoreManager.score : 0;
        int mistakes = scoreManager != null ? scoreManager.mistakes : 0;
        int seconds = Mathf.CeilToInt(remainingSeconds);
        int minutes = Mathf.Max(0, seconds / 60);
        int secondPart = Mathf.Max(0, seconds % 60);

        dashboardText.text =
            $"Time {minutes:00}:{secondPart:00}   Score {score}\n" +
            $"Mistakes {mistakes}";
    }

    private void EnsureWorldUi()
    {
        if (uiAnchor == null)
        {
            GameObject anchor = new GameObject("SessionUI");
            anchor.transform.position = new Vector3(0f, 2.85f, 1.8f);
            uiAnchor = anchor.transform;
        }

        uiAnchor.position = new Vector3(0f, 2.85f, 1.8f);
        uiAnchor.rotation = Quaternion.identity;

        dashboardText = dashboardText != null ? dashboardText : CreateText("DashboardText");
        feedbackText = feedbackText != null ? feedbackText : CreateText("FeedbackText");

        ConfigureText(dashboardText, new Vector3(0f, 0.7f, 0f), DashboardTextSize, TextAnchor.MiddleCenter);
        ConfigureText(feedbackText, new Vector3(0f, 0.08f, 0f), FeedbackTextSize, TextAnchor.MiddleCenter);

        feedbackText.gameObject.SetActive(false);
    }

    private TextMesh CreateText(string objectName)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(uiAnchor, false);
        return textObject.AddComponent<TextMesh>();
    }

    private void ConfigureText(TextMesh textMesh, Vector3 localPosition, float characterSize, TextAnchor anchor)
    {
        textMesh.transform.SetParent(uiAnchor, false);
        textMesh.transform.localPosition = localPosition;
        textMesh.transform.localRotation = Quaternion.identity;
        textMesh.transform.localScale = Vector3.one;
        textMesh.anchor = anchor;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 72;
        textMesh.characterSize = characterSize;
        textMesh.color = Color.white;
    }

    private void EnsureAudioFeedback()
    {
        feedbackAudio = GetComponent<AudioSource>();
        if (feedbackAudio == null)
        {
            feedbackAudio = gameObject.AddComponent<AudioSource>();
        }

        feedbackAudio.playOnAwake = false;
        feedbackAudio.spatialBlend = 0f;
        successClip = CreateToneClip("SortSuccessTone", 880f, 0.12f);
        errorClip = CreateToneClip("SortErrorTone", 220f, 0.18f);
    }

    private AudioClip CreateToneClip(string clipName, float frequency, float duration)
    {
        const int sampleRate = 24000;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float fade = 1f - (i / (float)sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * fade * 0.4f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void EnsureDemoContent()
    {
        CreateRuntimeMaterials();
        EnsureFoodBins();
        EnsureSpawnerPrefabs();
    }

    private void CreateRuntimeMaterials()
    {
        generalMaterial = CreateRuntimeMaterial("Runtime_General_Green", new Color(0.2f, 0.75f, 0.35f));
        recyclableMaterial = CreateRuntimeMaterial("Runtime_Recyclable_Blue", new Color(0.1f, 0.45f, 0.95f));
        foodMaterial = CreateRuntimeMaterial("Runtime_Food_Orange", new Color(0.95f, 0.48f, 0.12f));
        dirtyMaterial = CreateRuntimeMaterial("Runtime_Dirty_Brown", new Color(0.45f, 0.25f, 0.12f));
        acceptedMaterial = CreateRuntimeMaterial("Runtime_Accepted_Green", new Color(0.35f, 1f, 0.45f));
        rejectedMaterial = CreateRuntimeMaterial("Runtime_Rejected_Red", new Color(1f, 0.18f, 0.14f));
    }

    private Material CreateRuntimeMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        return material;
    }

    private void EnsureFoodBins()
    {
        DestroyStaleBin("Bin_Plastic");
        DestroyStaleBin("Bin_Paper");
        DestroyStaleBin("Bin_RawFood");
        DestroyStaleBin("Bin_CookedFood");
        DestroyStaleBin("BinLabel_Plastic");
        DestroyStaleBin("BinLabel_Paper");
        DestroyStaleBin("BinLabel_RawFood");
        DestroyStaleBin("BinLabel_CookedFood");

        EnsureBin("Bin_General", "General", TrashCategory.General, generalMaterial, new Vector3(-2.6f, 0.6f, 4f));
        EnsureBin("Bin_Recyclable", "Recyclable", TrashCategory.Recyclable, recyclableMaterial, new Vector3(0f, 0.6f, 4f));
        EnsureBin("Bin_Food", "Food", TrashCategory.Food, foodMaterial, new Vector3(2.6f, 0.6f, 4f));
    }

    private void DestroyStaleBin(string objectName)
    {
        GameObject staleObject = GameObject.Find(objectName);
        if (staleObject != null)
        {
            Destroy(staleObject);
        }
    }

    private void EnsureBin(string binName, string label, TrashCategory category, Material material, Vector3 position)
    {
        GameObject bin = GameObject.Find(binName);
        if (bin == null)
        {
            bin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bin.name = binName;
            bin.transform.position = position;
            bin.transform.localScale = new Vector3(1.45f, 1.2f, 1.45f);
        }

        if (bin.TryGetComponent(out Renderer renderer))
        {
            renderer.sharedMaterial = material;
        }

        Collider binCollider = bin.GetComponent<Collider>();
        if (binCollider != null)
        {
            binCollider.isTrigger = true;
        }

        BinValidator validator = bin.GetComponent<BinValidator>();
        if (validator == null)
        {
            validator = bin.AddComponent<BinValidator>();
        }

        validator.targetCategory = category;
        validator.acceptedFlashMaterial = acceptedMaterial;
        validator.rejectedFlashMaterial = rejectedMaterial;
        EnsureLabel("BinLabel_" + binName.Replace("Bin_", string.Empty), label, position + new Vector3(0f, 1.05f, -0.82f));
    }

    private void EnsureLabel(string objectName, string text, Vector3 position)
    {
        GameObject labelObject = GameObject.Find(objectName);
        if (labelObject == null)
        {
            labelObject = new GameObject(objectName);
            labelObject.transform.position = position;
        }

        labelObject.transform.position = position;
        labelObject.transform.rotation = Quaternion.identity;

        TextMesh textMesh = labelObject.GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = labelObject.AddComponent<TextMesh>();
        }

        textMesh.text = text;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.characterSize = BinLabelTextSize;
        textMesh.fontSize = 64;
        textMesh.color = Color.white;
    }

    private void EnsureSpawnerPrefabs()
    {
        TrashSpawner spawner = FindAnyObjectByType<TrashSpawner>();
        if (spawner == null)
        {
            return;
        }

        if (HasConfiguredSpawnerPrefabs(spawner.trashPrefabs))
        {
            return;
        }

        spawner.trashPrefabs = new[]
        {
            CreateRuntimeTrashTemplate("Trash_General_Template", PrimitiveType.Capsule, TrashCategory.General, false, generalMaterial),
            CreateRuntimeTrashTemplate("Trash_Plastic_Template", PrimitiveType.Sphere, TrashCategory.Recyclable, false, recyclableMaterial),
            CreateRuntimeTrashTemplate("Trash_Paper_Template", PrimitiveType.Cube, TrashCategory.Recyclable, false, recyclableMaterial),
            CreateRuntimeTrashTemplate("Trash_RawFood_Template", PrimitiveType.Capsule, TrashCategory.Food, false, foodMaterial),
            CreateRuntimeTrashTemplate("Trash_CookedFood_Template", PrimitiveType.Capsule, TrashCategory.Food, false, foodMaterial),
            CreateRuntimeTrashTemplate("Trash_DirtyPlastic_Template", PrimitiveType.Sphere, TrashCategory.Recyclable, true, dirtyMaterial),
            CreateRuntimeTrashTemplate("Trash_DirtyPaper_Template", PrimitiveType.Cube, TrashCategory.Recyclable, true, dirtyMaterial)
        };
    }

    private static bool HasConfiguredSpawnerPrefabs(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return false;
        }

        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null && prefab.GetComponent<TrashItem>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private GameObject CreateRuntimeTrashTemplate(string objectName, PrimitiveType primitiveType, TrashCategory category, bool isDirty, Material material)
    {
        GameObject template = GameObject.Find(objectName);
        if (template == null)
        {
            template = GameObject.CreatePrimitive(primitiveType);
            template.name = objectName;
            template.transform.position = new Vector3(0f, -100f, 0f);
            template.transform.localScale = Vector3.one * 0.45f;
            template.SetActive(false);
        }

        if (template.TryGetComponent(out Renderer renderer))
        {
            renderer.sharedMaterial = material;
        }

        TrashItem trashItem = template.GetComponent<TrashItem>();
        if (trashItem == null)
        {
            trashItem = template.AddComponent<TrashItem>();
        }

        trashItem.itemType = category;
        trashItem.isDirty = isDirty;

        Rigidbody body = template.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = template.AddComponent<Rigidbody>();
        }

        body.mass = 0.7f;
        body.linearDamping = 1f;
        body.angularDamping = 1f;
        return template;
    }
}
