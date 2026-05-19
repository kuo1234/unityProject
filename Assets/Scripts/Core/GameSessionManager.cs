using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    private const float DashboardTextSize = 0.07f;
    private const float FeedbackTextSize = 0.075f;
    private const float MistakeAlertDistance = 0.42f;
    private const float MistakeAlertTitleSize = 0.018f;
    private const float MistakeAlertActionSize = 0.014f;
    private const float MistakeAlertSeconds = 1.8f;
    private static readonly Vector3 MistakeAlertPanelScale = new Vector3(0.82f, 0.24f, 1f);
    private const float BinLabelTextSize = 0.075f;
    private const float FeedbackPulseSeconds = 0.7f;
    private const float MistakeAlertPulseSeconds = 0.8f;
    private const float StarCelebrationDistance = 0.58f;
    private const float StarCelebrationSeconds = 2.25f;
    private const float StarCelebrationPulseSeconds = 1.2f;
    private const float StarCelebrationTextSize = 0.022f;

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
    private float feedbackPulseEndTime;
    private Transform mistakeAlertAnchor;
    private TextMesh mistakeAlertTitleText;
    private TextMesh mistakeAlertActionText;
    private Transform mistakeAlertPanelTransform;
    private float mistakeAlertClearTime;
    private float mistakeAlertPulseEndTime;
    private Transform starCelebrationAnchor;
    private TextMesh starCelebrationText;
    private ParticleSystem starConfetti;
    private AudioClip starClip;
    private float starCelebrationClearTime;
    private float starCelebrationPulseEndTime;
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
            scoreManager.StarEarned += OnStarEarned;
        }

        StartRound();
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.FeedbackRaised -= OnFeedbackRaised;
            scoreManager.StarEarned -= OnStarEarned;
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
            feedbackText.transform.localScale = Vector3.one;
        }

        UpdateFeedbackPulse();
        UpdateMistakeAlert();
        UpdateStarCelebration();
        UpdateDashboard();
    }

    public void ShowFeedback(string message, bool positive)
    {
        OnFeedbackRaised(message, positive);
    }

    public void PlaySortCelebration(Vector3 position)
    {
        ParticleSystem celebration = CreateCelebrationParticles(position);
        celebration.Play();
        Destroy(celebration.gameObject, 1.5f);
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
        int stars = scoreManager != null ? scoreManager.stars : 0;
        int bestStreak = scoreManager != null ? scoreManager.bestStreak : 0;
        int attempts = Mathf.Max(1, score + mistakes);
        int accuracy = Mathf.RoundToInt((score / (float)attempts) * 100f);
        RoundResultStore.Save(score, mistakes, accuracy, stars, bestStreak);

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
        if (!positive)
        {
            ShowMistakeAlert(message);

            if (feedbackAudio != null)
            {
                feedbackAudio.PlayOneShot(errorClip, 0.42f);
            }

            return;
        }

        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = message;
        feedbackText.color = new Color(0.35f, 1f, 0.45f);
        feedbackText.gameObject.SetActive(true);
        feedbackText.transform.localScale = Vector3.one;
        feedbackClearTime = Time.time + 2.1f;
        feedbackPulseEndTime = Time.time + FeedbackPulseSeconds;

        if (feedbackAudio != null)
        {
            feedbackAudio.PlayOneShot(successClip, 0.35f);
        }
    }

    private void OnStarEarned(int starCount)
    {
        EnsureStarCelebration();
        if (starCelebrationAnchor == null || starCelebrationText == null || starConfetti == null)
        {
            return;
        }

        starCelebrationText.text = $"Star earned!\n{starCount}/3 stars";
        starCelebrationAnchor.gameObject.SetActive(true);
        PositionStarCelebration();
        starCelebrationAnchor.localScale = Vector3.one;
        starCelebrationClearTime = Time.time + StarCelebrationSeconds;
        starCelebrationPulseEndTime = Time.time + StarCelebrationPulseSeconds;

        starConfetti.Clear(true);
        starConfetti.Play(true);

        if (feedbackAudio != null)
        {
            feedbackAudio.PlayOneShot(starClip, 0.52f);
        }
    }

    private void EnsureStarCelebration()
    {
        Camera camera = Camera.main;
        if (camera == null || starCelebrationText != null)
        {
            return;
        }

        GameObject anchor = new GameObject("StarCelebration");
        starCelebrationAnchor = anchor.transform;
        starCelebrationAnchor.SetParent(camera.transform, false);

        GameObject textObject = new GameObject("StarCelebrationText");
        textObject.transform.SetParent(starCelebrationAnchor, false);
        textObject.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        textObject.transform.localRotation = Quaternion.identity;

        starCelebrationText = textObject.AddComponent<TextMesh>();
        starCelebrationText.alignment = TextAlignment.Center;
        starCelebrationText.anchor = TextAnchor.MiddleCenter;
        starCelebrationText.characterSize = StarCelebrationTextSize;
        starCelebrationText.fontSize = 72;
        starCelebrationText.fontStyle = FontStyle.Bold;
        starCelebrationText.color = new Color(1f, 0.95f, 0.2f);
        starCelebrationText.richText = false;

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.sharedMaterial = CreateOverlayTextMaterial(starCelebrationText.font, 5010);
        textRenderer.sortingOrder = 5010;

        starConfetti = CreateStarConfetti(starCelebrationAnchor);
        starCelebrationAnchor.gameObject.SetActive(false);
    }

    private ParticleSystem CreateStarConfetti(Transform parent)
    {
        GameObject confettiObject = new GameObject("StarConfetti");
        confettiObject.transform.SetParent(parent, false);
        confettiObject.transform.localPosition = new Vector3(0f, 0f, 0.05f);
        confettiObject.transform.localRotation = Quaternion.identity;

        ParticleSystem particles = confettiObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 1.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.05f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.2f),
            new Color(0.25f, 1f, 0.65f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 95) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(0.55f, 0.18f, 0.01f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.45f, 0.7f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-360f, 360f);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 5005;
        renderer.material = CreateParticleOverlayMaterial();
        return particles;
    }

    private void UpdateStarCelebration()
    {
        if (starCelebrationAnchor == null || !starCelebrationAnchor.gameObject.activeSelf)
        {
            return;
        }

        PositionStarCelebration();

        if (Time.time >= starCelebrationClearTime)
        {
            starCelebrationAnchor.gameObject.SetActive(false);
            starCelebrationAnchor.localScale = Vector3.one;
            return;
        }

        float remaining = Mathf.Clamp01((starCelebrationPulseEndTime - Time.time) / StarCelebrationPulseSeconds);
        float pulse = Mathf.Sin(remaining * Mathf.PI * 6f) * 0.12f * remaining;
        starCelebrationAnchor.localScale = Vector3.one * (1f + pulse);
    }

    private void PositionStarCelebration()
    {
        Camera camera = Camera.main;
        if (camera == null || starCelebrationAnchor == null)
        {
            return;
        }

        if (starCelebrationAnchor.parent != camera.transform)
        {
            starCelebrationAnchor.SetParent(camera.transform, false);
        }

        starCelebrationAnchor.localPosition = new Vector3(0f, 0.08f, Mathf.Max(camera.nearClipPlane + 0.14f, StarCelebrationDistance));
        starCelebrationAnchor.localRotation = Quaternion.identity;
    }

    private void ShowMistakeAlert(string message)
    {
        EnsureMistakeAlert();
        if (mistakeAlertTitleText == null || mistakeAlertActionText == null || mistakeAlertAnchor == null)
        {
            return;
        }

        SetMistakeAlertText(message);
        mistakeAlertAnchor.gameObject.SetActive(true);
        PositionMistakeAlert();
        mistakeAlertAnchor.localScale = Vector3.one;
        mistakeAlertClearTime = Time.time + MistakeAlertSeconds;
        mistakeAlertPulseEndTime = Time.time + MistakeAlertPulseSeconds;
    }

    private void SetMistakeAlertText(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            mistakeAlertTitleText.text = "Try again";
            mistakeAlertActionText.text = "Match the bin";
            return;
        }

        if (message.Contains("Wash"))
        {
            mistakeAlertTitleText.text = "Needs a wash";
            mistakeAlertActionText.text = "Use sink first";
            return;
        }

        const string tryPrefix = "Try the ";
        const string binSuffix = " bin";
        if (message.StartsWith(tryPrefix) && message.EndsWith(binSuffix))
        {
            string binName = message.Substring(tryPrefix.Length, message.Length - tryPrefix.Length - binSuffix.Length);
            mistakeAlertTitleText.text = "Wrong bin";
            mistakeAlertActionText.text = $"Use {binName} bin";
            return;
        }

        mistakeAlertTitleText.text = "Oops";
        mistakeAlertActionText.text = message;
    }

    private void EnsureMistakeAlert()
    {
        Camera camera = Camera.main;
        if (camera == null || mistakeAlertTitleText != null)
        {
            return;
        }

        GameObject anchor = new GameObject("MistakeAlert");
        mistakeAlertAnchor = anchor.transform;
        mistakeAlertAnchor.SetParent(camera.transform, false);

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name = "MistakeAlertPanel";
        panel.transform.SetParent(mistakeAlertAnchor, false);
        panel.transform.localPosition = new Vector3(0f, 0f, 0.015f);
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = MistakeAlertPanelScale;
        mistakeAlertPanelTransform = panel.transform;

        Collider panelCollider = panel.GetComponent<Collider>();
        if (panelCollider != null)
        {
            Destroy(panelCollider);
        }

        Renderer panelRenderer = panel.GetComponent<Renderer>();
        panelRenderer.sharedMaterial = CreateOverlayMaterial("Runtime_Mistake_Alert", new Color(1f, 0.22f, 0.08f, 0.96f), 4990);
        panelRenderer.sortingOrder = 4990;

        mistakeAlertTitleText = CreateMistakeAlertText("MistakeAlertTitle", new Vector3(0f, 0.045f, 0f), MistakeAlertTitleSize, 62, FontStyle.Bold);
        mistakeAlertActionText = CreateMistakeAlertText("MistakeAlertAction", new Vector3(0f, -0.045f, 0f), MistakeAlertActionSize, 56, FontStyle.Normal);

        mistakeAlertAnchor.gameObject.SetActive(false);
    }

    private TextMesh CreateMistakeAlertText(string objectName, Vector3 localPosition, float characterSize, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(mistakeAlertAnchor, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.alignment = TextAlignment.Center;
        text.anchor = TextAnchor.MiddleCenter;
        text.characterSize = characterSize;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.richText = false;

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.sharedMaterial = CreateOverlayTextMaterial(text.font, 5000);
        textRenderer.sortingOrder = 5000;
        return text;
    }

    private void UpdateMistakeAlert()
    {
        if (mistakeAlertAnchor == null || !mistakeAlertAnchor.gameObject.activeSelf)
        {
            return;
        }

        PositionMistakeAlert();

        if (Time.time >= mistakeAlertClearTime)
        {
            mistakeAlertAnchor.gameObject.SetActive(false);
            mistakeAlertAnchor.localScale = Vector3.one;
            return;
        }

        float remaining = Mathf.Clamp01((mistakeAlertPulseEndTime - Time.time) / MistakeAlertPulseSeconds);
        float pulse = Mathf.Sin(remaining * Mathf.PI * 5f) * 0.1f * remaining;
        mistakeAlertAnchor.localScale = Vector3.one * (1f + pulse);
    }

    private void PositionMistakeAlert()
    {
        Camera camera = Camera.main;
        if (camera == null || mistakeAlertAnchor == null)
        {
            return;
        }

        if (mistakeAlertAnchor.parent != camera.transform)
        {
            mistakeAlertAnchor.SetParent(camera.transform, false);
        }

        mistakeAlertAnchor.localPosition = new Vector3(0f, -0.12f, Mathf.Max(camera.nearClipPlane + 0.08f, MistakeAlertDistance));
        mistakeAlertAnchor.localRotation = Quaternion.identity;
    }

    private Material CreateOverlayMaterial(string materialName, Color color, int renderQueue)
    {
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        material.renderQueue = renderQueue;
        material.SetInt("_ZWrite", 0);
        material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        return material;
    }

    private Material CreateOverlayTextMaterial(Font font, int renderQueue)
    {
        Material material = new Material(font.material);
        material.name = "Runtime_Mistake_Alert_Text";
        material.renderQueue = renderQueue;
        material.SetInt("_ZWrite", 0);
        material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        return material;
    }

    private Material CreateParticleOverlayMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.name = "Runtime_Star_Confetti";
        material.renderQueue = 5005;
        material.SetInt("_ZWrite", 0);
        material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        return material;
    }

    private void UpdateFeedbackPulse()
    {
        if (feedbackText == null || !feedbackText.gameObject.activeSelf || Time.time >= feedbackPulseEndTime)
        {
            if (feedbackText != null)
            {
                feedbackText.transform.localScale = Vector3.one;
            }

            return;
        }

        float remaining = Mathf.Clamp01((feedbackPulseEndTime - Time.time) / FeedbackPulseSeconds);
        float pulse = Mathf.Sin(remaining * Mathf.PI * 4f) * 0.08f * remaining;
        feedbackText.transform.localScale = Vector3.one * (1f + pulse);
    }

    private void UpdateDashboard()
    {
        if (dashboardText == null)
        {
            return;
        }

        int score = scoreManager != null ? scoreManager.score : 0;
        int mistakes = scoreManager != null ? scoreManager.mistakes : 0;
        int streak = scoreManager != null ? scoreManager.streak : 0;
        int stars = scoreManager != null ? scoreManager.stars : 0;
        int seconds = Mathf.CeilToInt(remainingSeconds);
        int minutes = Mathf.Max(0, seconds / 60);
        int secondPart = Mathf.Max(0, seconds % 60);

        dashboardText.text =
            $"Time {minutes:00}:{secondPart:00}   Items rescued {score}\n" +
            $"Stars {stars}/3   Streak {streak}   Mistakes {mistakes}";
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
        starClip = CreateStarClip();
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

    private AudioClip CreateStarClip()
    {
        const int sampleRate = 24000;
        const float duration = 0.36f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float[] notes = { 660f, 880f, 1320f };

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            int noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt((time / duration) * notes.Length));
            float localTime = time - ((duration / notes.Length) * noteIndex);
            float fade = 1f - (time / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * localTime) * fade * 0.42f;
        }

        AudioClip clip = AudioClip.Create("StarEarnedTone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private ParticleSystem CreateCelebrationParticles(Vector3 position)
    {
        GameObject particleObject = new GameObject("SortCelebration");
        particleObject.transform.position = position + Vector3.up * 0.85f;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.45f;
        main.loop = false;
        main.startLifetime = 0.75f;
        main.startSpeed = 1.8f;
        main.startSize = 0.08f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.25f),
            new Color(0.35f, 1f, 0.55f));

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);

        return particles;
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
