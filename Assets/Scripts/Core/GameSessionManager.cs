using System.Collections;
using System.Collections.Generic;
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
    private const float BinLabelTextSize = 0.055f;
    private const float FeedbackPulseSeconds = 0.7f;
    private const float MistakeAlertPulseSeconds = 0.8f;
    private const float StarCelebrationDistance = 0.58f;
    private const float StarCelebrationSeconds = 2.25f;
    private const float StarCelebrationPulseSeconds = 1.2f;
    private const float StarCelebrationTextSize = 0.022f;
    private const float LearnCardDistance = 0.66f;
    private const float LearnCardSeconds = 2.1f;
    private const float LearnCardPulseSeconds = 0.7f;
    private const float LearnCardTitleSize = 0.017f;
    private const float LearnCardBodySize = 0.014f;
    private static readonly Vector3 LearnCardPanelScale = new Vector3(0.88f, 0.28f, 1f);
    private const float IdleHintSeconds = 8f;
    private const string OrganicFoodWastePrefabName = "Trash_FoodWaste_Organic_Template";
    private const string BananaPeelPrefabName = "Trash_FoodWaste_BananaPeel_Template";
    private const string BroccoliPrefabName = "Trash_FoodWaste_Broccoli_Template";
    private const string DirtyPlasticPrefabName = "Trash_DirtyPlastic_Template";
    private const string DirtyPaperPrefabName = "Trash_DirtyPaper_Template";
    private const float LevelTransitionSeconds = 2.5f;
    private const float RoundCompleteDelaySeconds = 1.6f;
    private static readonly Color DarkTextColor = new Color(0.08f, 0.11f, 0.14f);
    private static readonly Color PositiveDarkTextColor = new Color(0.03f, 0.28f, 0.12f);
    private static readonly LevelConfig[] LevelConfigs =
    {
        new LevelConfig(1, 5, false, true, 0, true, false, "Level 1: sort {0} items."),
        new LevelConfig(2, 7, false, true, 0, false, false, "Level 2: sort {0} items without the spotlight."),
        new LevelConfig(3, 8, true, true, 2, true, true, "Level 3: wash dirty trash first."),
        new LevelConfig(4, 10, true, true, 2, false, false, "Level 4: final mixed challenge.")
    };
    private static int FinalLevel => LevelConfigs.Length;

    [Header("Round")]
    public float roundDurationSeconds = 240f;
    public string completeSceneName = "CompleteScene";

    [Header("World UI")]
    public TextMesh dashboardText;
    public TextMesh feedbackText;
    public Transform uiAnchor;

    [Header("Voice Prompts")]
    public AudioClip generalEducationVoiceClip;
    public AudioClip recycleEducationVoiceClip;
    public AudioClip foodEducationVoiceClip;
    public AudioClip washFirstVoiceClip;
    public AudioClip tryAgainVoiceClip;

    private float remainingSeconds;
    private bool roundActive;
    private ScoreManager scoreManager;
    private TrashSpawner[] spawners;
    private float feedbackClearTime;
    private AudioSource feedbackAudio;
    private AudioClip successClip;
    private AudioClip errorClip;
    private AudioClip successCelebrationClip;
    private AudioClip bounceChimeClip;
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
    private Transform learnCardAnchor;
    private Renderer learnCardPanelRenderer;
    private TextMesh learnCardTitleText;
    private TextMesh learnCardBodyText;
    private float learnCardClearTime;
    private float learnCardPulseEndTime;
    private Material generalMaterial;
    private Material recyclableMaterial;
    private Material foodMaterial;
    private Material dirtyMaterial;
    private Material acceptedMaterial;
    private Material rejectedMaterial;
    private ChildGuidanceController childGuidance;
    private float nextIdleHintTime;
    private int successfulSortCount;
    private int currentLevel = 1;
    private int itemsRequiredThisLevel = LevelConfigs[0].itemCount;
    private int itemsSortedThisLevel;
    private bool levelTransitionActive;
    private Coroutine levelTransitionRoutine;
    private Coroutine roundCompleteRoutine;
    private Vector2 originalPlayerSpawnPosition;
    private bool hasOriginalPlayerSpawnPosition;
    private string currentGuidanceMessage = "Pick up a trash item.";

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
    }

    private void Start()
    {
        scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
        EnsureDemoContent();
        spawners = FindObjectsByType<TrashSpawner>(FindObjectsInactive.Exclude);
        ConfigureSpawnersForLevelMode();
        EnsureWorldUi();
        EnsureAudioFeedback();
        EnsureChildGuidance();
        EnsurePlayerSpeedTuner();
        EnsureChildFriendlyEnvironment();
        CaptureOriginalPlayerSpawnPosition();

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
        UpdateLearnCard();
        UpdateIdleGuidance();
        UpdateDashboard();
    }

    public void ShowFeedback(string message, bool positive)
    {
        OnFeedbackRaised(message, positive);
    }

    public void ShowGuidance(string message, Transform target = null, bool positive = false)
    {
        currentGuidanceMessage = string.IsNullOrWhiteSpace(message) ? "Pick up a trash item." : message;
        if (childGuidance == null)
        {
            EnsureChildGuidance();
        }

        if (childGuidance != null)
        {
            childGuidance.Show(currentGuidanceMessage, target, positive);
        }
    }

    public void NotifyPlayerAction(PlayerGuidanceAction action, TrashItem item = null)
    {
        if (action != PlayerGuidanceAction.Idle)
        {
            nextIdleHintTime = Time.time + IdleHintSeconds;
        }

        switch (action)
        {
            case PlayerGuidanceAction.TargetedTrash:
                if (item != null)
                {
                    ShowGuidance("Pick up this trash.");
                }
                break;
            case PlayerGuidanceAction.PickedUpTrash:
                ShowGuidance(GetDestinationHint(item), GetGuidanceTargetForItem(item));
                break;
            case PlayerGuidanceAction.WashedTrash:
                ShowGuidance("Clean! Now find its bin.", GetGuidanceTargetAfterWashing(item), true);
                break;
            case PlayerGuidanceAction.SortedCorrectly:
                ShowLearnCard(item);
                HandleSortedCorrectly();
                break;
            case PlayerGuidanceAction.SortedIncorrectly:
                ShowGuidance(GetDestinationHint(item), GetGuidanceTargetForItem(item));
                break;
            case PlayerGuidanceAction.Idle:
                ShowIdleHint();
                break;
        }
    }

    public void PlaySortCelebration(Vector3 position)
    {
        ParticleSystem celebration = CreateCelebrationParticles(position);
        celebration.Play();
        CreateCelebrationStars(position);
        CreateCelebrationFlash(position);
        if (feedbackAudio != null)
        {
            feedbackAudio.PlayOneShot(successCelebrationClip, 0.46f);
            StartCoroutine(PlayDelayedOneShot(bounceChimeClip, 0.34f, 0.32f));
        }

        Destroy(celebration.gameObject, 5.8f);
    }

    private void HandleSortedCorrectly()
    {
        successfulSortCount++;
        itemsSortedThisLevel = Mathf.Min(itemsSortedThisLevel + 1, itemsRequiredThisLevel);

        if (itemsSortedThisLevel >= itemsRequiredThisLevel)
        {
            if (currentLevel < FinalLevel)
            {
                BeginLevelTransition();
                return;
            }

            BeginRoundComplete();
            return;
        }

        ShowGuidance($"Great sort! {itemsRequiredThisLevel - itemsSortedThisLevel} left.", null, true);
    }

    private void BeginLevelTransition()
    {
        if (levelTransitionRoutine != null)
        {
            StopCoroutine(levelTransitionRoutine);
        }

        levelTransitionRoutine = StartCoroutine(LevelTransitionRoutine());
    }

    private IEnumerator LevelTransitionRoutine()
    {
        levelTransitionActive = true;
        SetSpawnersEnabled(false);
        if (childGuidance != null)
        {
            childGuidance.ClearTarget();
        }

        ShowGuidance($"Level {currentLevel} complete!\nGet ready for Level {currentLevel + 1}.", null, true);
        yield return new WaitForSeconds(LevelTransitionSeconds);
        if (!roundActive)
        {
            levelTransitionRoutine = null;
            yield break;
        }

        currentLevel++;
        RespawnPlayerAtOriginalSpawn();
        StartLevel(currentLevel);
        levelTransitionRoutine = null;
    }

    private void BeginRoundComplete()
    {
        if (roundCompleteRoutine != null)
        {
            return;
        }

        roundCompleteRoutine = StartCoroutine(RoundCompleteRoutine());
    }

    private IEnumerator RoundCompleteRoutine()
    {
        levelTransitionActive = true;
        SetSpawnersEnabled(false);
        ShowGuidance("You finished Level 4!", null, true);
        yield return new WaitForSeconds(RoundCompleteDelaySeconds);
        EndRound();
        roundCompleteRoutine = null;
    }

    private void EnsureChildGuidance()
    {
        childGuidance = GetComponent<ChildGuidanceController>();
        if (childGuidance == null)
        {
            childGuidance = gameObject.AddComponent<ChildGuidanceController>();
        }

        nextIdleHintTime = Time.time + 4f;
    }

    private void EnsurePlayerSpeedTuner()
    {
        if (GetComponent<PlayerSpeedTuner>() == null)
        {
            gameObject.AddComponent<PlayerSpeedTuner>();
        }

        if (GetComponent<DirectPlayerMover>() == null)
        {
            gameObject.AddComponent<DirectPlayerMover>();
        }
    }

    private void EnsureChildFriendlyEnvironment()
    {
        if (GetComponent<ChildFriendlyEnvironment>() == null)
        {
            gameObject.AddComponent<ChildFriendlyEnvironment>();
        }
    }

    private void UpdateIdleGuidance()
    {
        if (!roundActive || levelTransitionActive || Time.time < nextIdleHintTime)
        {
            return;
        }

        NotifyPlayerAction(PlayerGuidanceAction.Idle);
        nextIdleHintTime = Time.time + IdleHintSeconds;
    }

    private void ShowIdleHint()
    {
        TrashItem dirtyItem = FindNearestTrashItem(true);
        if (dirtyItem != null)
        {
            WashingStation washingStation = FindAnyObjectByType<WashingStation>();
            ShowGuidance("Dirty trash goes to the sink first.", washingStation != null ? washingStation.transform : null);
            return;
        }

        Transform nearestTrash = FindNearestTrashItemTransform();
        if (nearestTrash != null)
        {
            ShowGuidance("Pick up this trash.");
            return;
        }

        ShowGuidance("Look for a trash item.");
    }

    private void UpdateSpawnerTutorialStage()
    {
        if (spawners == null)
        {
            return;
        }

        int stage = successfulSortCount <= 0 ? 0 : successfulSortCount == 1 ? 1 : 2;
        foreach (TrashSpawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.SetTutorialStage(stage);
            }
        }
    }

    private Transform FindNearestTrashItemTransform(bool dirtyOnly = false)
    {
        TrashItem item = FindNearestTrashItem(dirtyOnly);
        return item != null ? item.transform : null;
    }

    private TrashItem FindNearestTrashItem(bool dirtyOnly = false)
    {
        Vector3 origin = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        TrashItem bestItem = null;
        float bestDistance = float.MaxValue;

        foreach (TrashItem item in FindObjectsByType<TrashItem>(FindObjectsInactive.Exclude))
        {
            if (item == null || (dirtyOnly && !item.isDirty))
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(item.transform.position - origin);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestItem = item;
            }
        }

        return bestItem;
    }

    private Transform FindDestinationForItem(TrashItem item)
    {
        if (item == null)
        {
            return null;
        }

        if (item.isDirty)
        {
            WashingStation washingStation = FindAnyObjectByType<WashingStation>();
            return washingStation != null ? washingStation.transform : null;
        }

        foreach (BinValidator bin in FindObjectsByType<BinValidator>(FindObjectsInactive.Exclude))
        {
            if (bin != null && bin.targetCategory == item.itemType)
            {
                return bin.transform;
            }
        }

        return null;
    }

    private Transform GetGuidanceTargetForItem(TrashItem item)
    {
        LevelConfig levelConfig = GetLevelConfig(currentLevel);
        if (!levelConfig.spotlightEnabled || item == null)
        {
            return null;
        }

        if (levelConfig.washingLesson)
        {
            return item.isDirty ? FindDestinationForItem(item) : null;
        }

        return FindDestinationForItem(item);
    }

    private Transform GetGuidanceTargetAfterWashing(TrashItem item)
    {
        LevelConfig levelConfig = GetLevelConfig(currentLevel);
        if (!levelConfig.spotlightEnabled || !levelConfig.washingLesson)
        {
            return null;
        }

        return FindDestinationForItem(item);
    }

    private string GetDestinationHint(TrashItem item)
    {
        if (item == null)
        {
            return "Pick up a trash item.";
        }

        if (item.isDirty)
        {
            return "Wash dirty trash first.";
        }

        return "Put it in the " + GetChildBinName(item.itemType) + " bin.";
    }

    private static string GetChildBinName(TrashCategory category)
    {
        return ScoreManager.GetChildBinName(category);
    }

    private static LevelConfig GetLevelConfig(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, LevelConfigs.Length - 1);
        return LevelConfigs[index];
    }

    private static int GetMaxLevelItemCount()
    {
        int maxCount = 0;
        foreach (LevelConfig levelConfig in LevelConfigs)
        {
            maxCount = Mathf.Max(maxCount, levelConfig.itemCount);
        }

        return maxCount;
    }

    private void StartRound()
    {
        remainingSeconds = roundDurationSeconds;
        roundActive = true;
        successfulSortCount = 0;
        currentLevel = 1;
        StartLevel(currentLevel);
    }

    private void StartLevel(int level)
    {
        LevelConfig levelConfig = GetLevelConfig(level);
        levelTransitionActive = false;
        itemsSortedThisLevel = 0;
        itemsRequiredThisLevel = levelConfig.itemCount;
        ApplyLevelGuidanceMode();
        SetSpawnersEnabled(true);

        int spawnedCount = SpawnLevelTrash(itemsRequiredThisLevel);
        if (spawnedCount <= 0)
        {
            Debug.LogWarning("[GameSessionManager] No trash spawned for the level; ending round.");
            EndRound();
            return;
        }

        itemsRequiredThisLevel = spawnedCount;

        string message = string.Format(levelConfig.introMessage, itemsRequiredThisLevel);
        ShowGuidance(message, null, true);
    }

    private void EndRound()
    {
        if (!roundActive)
        {
            return;
        }

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

    private int SpawnLevelTrash(int count)
    {
        TrashSpawner spawner = GetPrimarySpawner();
        if (spawner == null)
        {
            Debug.LogWarning("[GameSessionManager] No TrashSpawner found for level batch.");
            return 0;
        }

        LevelConfig levelConfig = GetLevelConfig(currentLevel);
        GameObject[] spawnedItems = spawner.SpawnUniqueBatch(count, levelConfig.includeDirty, levelConfig.includeFood, levelConfig.minimumDirty);
        return spawnedItems != null ? spawnedItems.Length : 0;
    }

    private TrashSpawner GetPrimarySpawner()
    {
        if (spawners == null)
        {
            return null;
        }

        foreach (TrashSpawner spawner in spawners)
        {
            if (spawner != null && spawner.trashPrefabs != null && spawner.trashPrefabs.Length > 0)
            {
                return spawner;
            }
        }

        return null;
    }

    private void ApplyLevelGuidanceMode()
    {
        if (childGuidance == null)
        {
            EnsureChildGuidance();
        }

        if (childGuidance != null)
        {
            childGuidance.SetSpotlightEnabled(GetLevelConfig(currentLevel).spotlightEnabled);
        }
    }

    private void ConfigureSpawnersForLevelMode()
    {
        if (spawners == null)
        {
            return;
        }

        foreach (TrashSpawner spawner in spawners)
        {
            if (spawner == null)
            {
                continue;
            }

            spawner.batchSpawnOnDemand = true;
            spawner.childFriendlyStagedSpawning = false;
            spawner.maxActiveTrash = Mathf.Max(spawner.maxActiveTrash, GetMaxLevelItemCount());
        }
    }

    private void CaptureOriginalPlayerSpawnPosition()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        originalPlayerSpawnPosition = new Vector2(camera.transform.position.x, camera.transform.position.z);
        hasOriginalPlayerSpawnPosition = true;
    }

    private void RespawnPlayerAtOriginalSpawn()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        if (!hasOriginalPlayerSpawnPosition)
        {
            CaptureOriginalPlayerSpawnPosition();
        }

        Transform root = FindPlayerRigRoot(camera.transform);
        Vector3 cameraPosition = camera.transform.position;
        Vector3 correction = new Vector3(
            originalPlayerSpawnPosition.x - cameraPosition.x,
            0f,
            originalPlayerSpawnPosition.y - cameraPosition.z);
        root.position += correction;
    }

    private static Transform FindPlayerRigRoot(Transform cameraTransform)
    {
        Transform current = cameraTransform;
        Transform best = cameraTransform;
        while (current != null)
        {
            if (current.name.Contains("OVRCameraRig") || current.name.Contains("XR") || current.parent == null)
            {
                best = current;
            }

            current = current.parent;
        }

        return best;
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
            ShowGuidance(message, null, false);

            if (feedbackAudio != null)
            {
                AudioClip voiceClip = GetVoicePromptClip(message, false);
                if (voiceClip != null)
                {
                    feedbackAudio.PlayOneShot(voiceClip, 0.85f);
                }

                feedbackAudio.PlayOneShot(errorClip, 0.28f);
            }

            return;
        }

        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = message;
        feedbackText.color = PositiveDarkTextColor;
        feedbackText.gameObject.SetActive(true);
        feedbackText.transform.localScale = Vector3.one;
        feedbackClearTime = Time.time + 2.1f;
        feedbackPulseEndTime = Time.time + FeedbackPulseSeconds;
        ShowGuidance(message, null, true);

        if (feedbackAudio != null)
        {
            AudioClip voiceClip = GetVoicePromptClip(message, true);
            if (voiceClip != null)
            {
                feedbackAudio.PlayOneShot(voiceClip, 0.85f);
            }

            feedbackAudio.PlayOneShot(successClip, 0.25f);
        }
    }

    private AudioClip GetVoicePromptClip(string message, bool positive)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        if (!positive)
        {
            if (message.Contains("Wash"))
            {
                return washFirstVoiceClip != null ? washFirstVoiceClip : tryAgainVoiceClip;
            }

            return tryAgainVoiceClip;
        }

        if (message.Contains("recycle") || message.Contains("Recycle"))
        {
            return recycleEducationVoiceClip;
        }

        if (message.Contains("food waste") || message.Contains("compost") || message.Contains("Food"))
        {
            return foodEducationVoiceClip;
        }

        if (message.Contains("general trash") || message.Contains("General"))
        {
            return generalEducationVoiceClip;
        }

        return null;
    }

    private void OnStarEarned(int starCount)
    {
        EnsureStarCelebration();
        if (starCelebrationAnchor == null || starCelebrationText == null || starConfetti == null)
        {
            return;
        }

        starCelebrationText.text = $"Star earned!\n{starCount}/3 stars";
        ShowGuidance($"Star earned! {starCount}/3 stars.", null, true);
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

    private void ShowLearnCard(TrashItem item)
    {
        if (!ScoreManager.TryGetLearnCardInfo(item, out LearnCardInfo info))
        {
            return;
        }

        EnsureLearnCard();
        if (learnCardAnchor == null || learnCardTitleText == null || learnCardBodyText == null)
        {
            return;
        }

        learnCardTitleText.text = info.title;
        learnCardTitleText.color = info.accentColor;
        learnCardBodyText.text = info.body;
        learnCardBodyText.color = DarkTextColor;

        if (learnCardPanelRenderer != null && learnCardPanelRenderer.sharedMaterial != null)
        {
            Color panelColor = Color.Lerp(new Color(1f, 1f, 0.94f, 0.96f), info.accentColor, 0.18f);
            panelColor.a = 0.96f;
            SetMaterialColor(learnCardPanelRenderer.sharedMaterial, panelColor);
        }

        learnCardAnchor.gameObject.SetActive(true);
        PositionLearnCard();
        learnCardAnchor.localScale = Vector3.one;
        learnCardClearTime = Time.time + LearnCardSeconds;
        learnCardPulseEndTime = Time.time + LearnCardPulseSeconds;
    }

    private void EnsureLearnCard()
    {
        Camera camera = Camera.main;
        if (camera == null || learnCardTitleText != null)
        {
            return;
        }

        GameObject anchor = new GameObject("LearnCard");
        learnCardAnchor = anchor.transform;
        learnCardAnchor.SetParent(camera.transform, false);

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name = "LearnCardPanel";
        panel.transform.SetParent(learnCardAnchor, false);
        panel.transform.localPosition = new Vector3(0f, 0f, 0.015f);
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = LearnCardPanelScale;

        Collider panelCollider = panel.GetComponent<Collider>();
        if (panelCollider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(panelCollider);
            }
            else
            {
                DestroyImmediate(panelCollider);
            }
        }

        learnCardPanelRenderer = panel.GetComponent<Renderer>();
        learnCardPanelRenderer.sharedMaterial = CreateOverlayMaterial("Runtime_Learn_Card", new Color(1f, 1f, 0.94f, 0.96f), 5008);
        learnCardPanelRenderer.sortingOrder = 5008;

        learnCardTitleText = CreateLearnCardText("LearnCardTitle", new Vector3(0f, 0.045f, 0f), LearnCardTitleSize, 64, FontStyle.Bold);
        learnCardBodyText = CreateLearnCardText("LearnCardBody", new Vector3(0f, -0.05f, 0f), LearnCardBodySize, 56, FontStyle.Normal);
        learnCardAnchor.gameObject.SetActive(false);
    }

    private TextMesh CreateLearnCardText(string objectName, Vector3 localPosition, float characterSize, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(learnCardAnchor, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.alignment = TextAlignment.Center;
        text.anchor = TextAnchor.MiddleCenter;
        text.characterSize = characterSize;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.richText = false;
        text.color = DarkTextColor;

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.sharedMaterial = CreateOverlayTextMaterial(text.font, 5010);
        textRenderer.sortingOrder = 5010;
        return text;
    }

    private void UpdateLearnCard()
    {
        if (learnCardAnchor == null || !learnCardAnchor.gameObject.activeSelf)
        {
            return;
        }

        PositionLearnCard();

        if (Time.time >= learnCardClearTime)
        {
            learnCardAnchor.gameObject.SetActive(false);
            learnCardAnchor.localScale = Vector3.one;
            return;
        }

        float remaining = Mathf.Clamp01((learnCardPulseEndTime - Time.time) / LearnCardPulseSeconds);
        float pulse = Mathf.Sin(remaining * Mathf.PI * 4f) * 0.08f * remaining;
        learnCardAnchor.localScale = Vector3.one * (1f + pulse);
    }

    private void PositionLearnCard()
    {
        Camera camera = Camera.main;
        if (camera == null || learnCardAnchor == null)
        {
            return;
        }

        if (learnCardAnchor.parent != camera.transform)
        {
            learnCardAnchor.SetParent(camera.transform, false);
        }

        learnCardAnchor.localPosition = new Vector3(0f, -0.04f, Mathf.Max(camera.nearClipPlane + 0.12f, LearnCardDistance));
        learnCardAnchor.localRotation = Quaternion.identity;
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
        starCelebrationText.color = DarkTextColor;
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
            mistakeAlertActionText.text = "Find the matching bin";
            return;
        }

        if (message.Contains("Wash"))
        {
            mistakeAlertTitleText.text = "Wash first";
            mistakeAlertActionText.text = "Use the sink";
            return;
        }

        const string tryPrefix = "Try the ";
        const string binSuffix = " bin";
        if (message.StartsWith(tryPrefix) && message.EndsWith(binSuffix))
        {
            string binName = message.Substring(tryPrefix.Length, message.Length - tryPrefix.Length - binSuffix.Length);
            mistakeAlertTitleText.text = "Good try";
            mistakeAlertActionText.text = $"Use {binName} bin";
            return;
        }

        mistakeAlertTitleText.text = "Good try";
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
        panelRenderer.sharedMaterial = CreateOverlayMaterial("Runtime_Mistake_Alert", new Color(0.95f, 0.5f, 0.12f, 0.96f), 4990);
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

    private Material CreateCelebrationStarMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.name = materialName;
        SetMaterialColor(material, color);
        material.renderQueue = 3020;
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        return material;
    }

    private Material CreateCelebrationFlashMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.name = "Runtime_Sort_Celebration_Flash";
        SetMaterialColor(material, new Color(1f, 0.9f, 0.25f, 0.45f));
        material.renderQueue = 3015;
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

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
        int stars = scoreManager != null ? scoreManager.stars : 0;

        dashboardText.text =
            $"Level {currentLevel}/{FinalLevel}   Items {itemsSortedThisLevel}/{itemsRequiredThisLevel}   Stars {stars}/3\n" +
            $"Items helped {score}\n" +
            currentGuidanceMessage;
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
        textMesh.color = DarkTextColor;
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
        errorClip = CreateToneClip("SortTryAgainTone", 330f, 0.14f);
        starClip = CreateStarClip();
        successCelebrationClip = CreateSuccessCelebrationClip();
        bounceChimeClip = CreateBounceChimeClip();
    }

    private IEnumerator PlayDelayedOneShot(AudioClip clip, float delay, float volume)
    {
        if (clip == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(delay);
        if (feedbackAudio != null)
        {
            feedbackAudio.PlayOneShot(clip, volume);
        }
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

    private AudioClip CreateSuccessCelebrationClip()
    {
        const int sampleRate = 24000;
        const float duration = 0.42f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            int noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt((time / duration) * notes.Length));
            float noteStart = (duration / notes.Length) * noteIndex;
            float localTime = time - noteStart;
            float fadeIn = Mathf.Clamp01(localTime / 0.018f);
            float fadeOut = 1f - Mathf.Clamp01((time - 0.3f) / 0.12f);
            float sparkle = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * localTime);
            float bright = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 2f * localTime) * 0.28f;
            samples[i] = (sparkle + bright) * fadeIn * fadeOut * 0.34f;
        }

        AudioClip clip = AudioClip.Create("SortCelebrationTaDa", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateBounceChimeClip()
    {
        const int sampleRate = 24000;
        const float duration = 0.22f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float fade = 1f - Mathf.Clamp01(time / duration);
            float bell = Mathf.Sin(2f * Mathf.PI * 1318.51f * time);
            float shimmer = Mathf.Sin(2f * Mathf.PI * 1975.53f * time) * 0.35f;
            samples[i] = (bell + shimmer) * fade * fade * 0.24f;
        }

        AudioClip clip = AudioClip.Create("SortCelebrationBounceChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private ParticleSystem CreateCelebrationParticles(Vector3 position)
    {
        GameObject particleObject = new GameObject("SortCelebration");
        particleObject.transform.position = position + Vector3.up * 0.95f;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 2.45f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.45f, 3.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.05f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.065f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.88f, 0.18f),
            new Color(0.35f, 0.82f, 1f));

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 88),
            new ParticleSystem.Burst(0.32f, 44),
            new ParticleSystem.Burst(0.82f, 30),
            new ParticleSystem.Burst(1.35f, 18)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 46f;
        shape.radius = 0.2f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.75f, 2.45f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-220f, 220f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.92f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.45f, 0.85f), 0.45f),
                new GradientColorKey(new Color(0.45f, 0.95f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleOverlayMaterial();

        return particles;
    }

    private void CreateCelebrationFlash(Vector3 position)
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "SortCelebrationFlash";
        flash.transform.position = position + Vector3.up * 0.85f;
        flash.transform.localScale = Vector3.one * 0.32f;

        Collider collider = flash.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = flash.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateCelebrationFlashMaterial();
        }

        Light popLight = flash.AddComponent<Light>();
        popLight.type = LightType.Point;
        popLight.color = new Color(1f, 0.86f, 0.22f);
        popLight.range = 3.4f;
        popLight.intensity = 7.5f;
        popLight.shadows = LightShadows.None;
        StartCoroutine(AnimateCelebrationFlash(flash, renderer, popLight));
    }

    private IEnumerator AnimateCelebrationFlash(GameObject flash, Renderer renderer, Light popLight)
    {
        const float lifetime = 0.85f;
        Vector3 startScale = Vector3.one * 0.32f;
        Vector3 endScale = Vector3.one * 1.75f;
        Material material = renderer != null ? renderer.material : null;
        Color baseColor = material != null ? GetMaterialColor(material) : new Color(1f, 0.9f, 0.25f, 0.45f);
        float elapsed = 0f;

        while (elapsed < lifetime && flash != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            float fade = 1f - t;
            flash.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, t));

            if (material != null)
            {
                Color color = baseColor;
                color.a = 0.45f * fade;
                SetMaterialColor(material, color);
            }

            if (popLight != null)
            {
                popLight.intensity = 7.5f * fade;
                popLight.range = Mathf.Lerp(3.4f, 4.8f, t);
            }

            yield return null;
        }

        if (flash != null)
        {
            Destroy(flash);
        }
    }

    private void CreateCelebrationStars(Vector3 position)
    {
        const int starCount = 18;
        Color[] starColors =
        {
            new Color(1f, 0.92f, 0.08f),
            new Color(1f, 0.48f, 0.9f),
            new Color(0.34f, 0.92f, 1f),
            new Color(0.55f, 1f, 0.38f)
        };

        for (int i = 0; i < starCount; i++)
        {
            GameObject star = CreateStarObject();
            star.name = "SortCelebrationStar";
            star.transform.position = position + new Vector3(Random.Range(-0.2f, 0.2f), 0.8f, Random.Range(-0.2f, 0.2f));
            star.transform.rotation = Random.rotation;
            star.transform.localScale = Vector3.one * Random.Range(0.22f, 0.34f);

            Renderer renderer = star.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateCelebrationStarMaterial(
                    "Runtime_Sort_Celebration_Star",
                    starColors[i % starColors.Length]);
            }

            Rigidbody body = star.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.mass = 0.12f;
            body.linearDamping = 0.08f;
            body.angularDamping = 0.05f;
            Vector2 outward = Random.insideUnitCircle.normalized;
            if (outward.sqrMagnitude <= 0.01f)
            {
                outward = Vector2.right;
            }

            body.linearVelocity = new Vector3(outward.x * Random.Range(1.35f, 2.75f), Random.Range(3.45f, 5.35f), outward.y * Random.Range(1.35f, 2.75f));
            body.angularVelocity = Random.insideUnitSphere * 12f;
            StartCoroutine(AnimateCelebrationStar(star, renderer, Random.Range(5.4f, 6.4f), 1.35f));
        }
    }

    private GameObject CreateStarObject()
    {
        GameObject root = new GameObject("SortCelebrationStar");
        MeshFilter meshFilter = root.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateStarMesh();
        MeshRenderer meshRenderer = root.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateCelebrationStarMaterial("Runtime_Sort_Celebration_Star", new Color(1f, 0.9f, 0.1f));

        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.radius = 0.55f;
        PhysicsMaterial bounceMaterial = new PhysicsMaterial("Runtime_Star_Bounce")
        {
            bounciness = 0.82f,
            dynamicFriction = 0.08f,
            staticFriction = 0.08f,
            bounceCombine = PhysicsMaterialCombine.Maximum
        };
        collider.sharedMaterial = bounceMaterial;
        return root;
    }

    private IEnumerator AnimateCelebrationStar(GameObject star, Renderer renderer, float lifetime, float fadeSeconds)
    {
        if (star == null)
        {
            yield break;
        }

        Vector3 baseScale = star.transform.localScale;
        Material material = renderer != null ? renderer.material : null;
        Color baseColor = material != null ? GetMaterialColor(material) : Color.white;
        float fadeStart = Mathf.Max(0.1f, lifetime - fadeSeconds);
        float elapsed = 0f;

        while (elapsed < lifetime && star != null)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= fadeStart)
            {
                float fade = 1f - Mathf.Clamp01((elapsed - fadeStart) / fadeSeconds);
                star.transform.localScale = baseScale * Mathf.Lerp(0.35f, 1f, fade);
                if (material != null)
                {
                    Color color = baseColor;
                    color.a = fade;
                    SetMaterialColor(material, color);
                }
            }

            yield return null;
        }

        if (star != null)
        {
            Destroy(star);
        }
    }

    private Color GetMaterialColor(Material material)
    {
        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        return material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private Mesh CreateStarMesh()
    {
        const int points = 5;
        Vector3[] vertices = new Vector3[(points * 2) + 1];
        int[] triangles = new int[points * 6];
        vertices[0] = Vector3.zero;

        for (int i = 0; i < points * 2; i++)
        {
            float radius = i % 2 == 0 ? 0.55f : 0.24f;
            float angle = ((i / (float)(points * 2)) * Mathf.PI * 2f) + Mathf.PI * 0.5f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        for (int i = 0; i < points * 2; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i == (points * 2) - 1 ? 1 : i + 2;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Runtime_Celebration_Star_Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
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
        rejectedMaterial = CreateRuntimeMaterial("Runtime_Rejected_Orange", new Color(1f, 0.55f, 0.12f));
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
        DestroyStaleBin("BinMarker_General");
        DestroyStaleBin("BinMarker_Recyclable");
        DestroyStaleBin("BinMarker_Food");

        EnsureBin("Bin_General", "General", TrashCategory.General, generalMaterial, new Vector3(-2.6f, 0.6f, 4f));
        EnsureBin("Bin_Recyclable", "Recycle", TrashCategory.Recyclable, recyclableMaterial, new Vector3(0f, 0.6f, 4f));
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
        textMesh.color = DarkTextColor;
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
            ConfigureKnownTrashPrefabs(spawner.trashPrefabs);
            if (!ContainsPrefabNamed(spawner.trashPrefabs, OrganicFoodWastePrefabName))
            {
                spawner.trashPrefabs = AppendPrefabIfMissing(
                    spawner.trashPrefabs,
                    CreateRuntimeOrganicFoodWasteTemplate());
            }

            if (!ContainsPrefabNamed(spawner.trashPrefabs, BananaPeelPrefabName))
            {
                spawner.trashPrefabs = AppendPrefabIfMissing(
                    spawner.trashPrefabs,
                    CreateRuntimeTrashTemplate(BananaPeelPrefabName, PrimitiveType.Capsule, TrashCategory.Food, false, foodMaterial));
            }

            if (!ContainsPrefabNamed(spawner.trashPrefabs, BroccoliPrefabName))
            {
                spawner.trashPrefabs = AppendPrefabIfMissing(
                    spawner.trashPrefabs,
                    CreateRuntimeTrashTemplate(BroccoliPrefabName, PrimitiveType.Sphere, TrashCategory.Food, false, foodMaterial));
            }

            if (!ContainsPrefabNamed(spawner.trashPrefabs, DirtyPlasticPrefabName))
            {
                spawner.trashPrefabs = AppendPrefabIfMissing(
                    spawner.trashPrefabs,
                    CreateRuntimeTrashTemplate(DirtyPlasticPrefabName, PrimitiveType.Sphere, TrashCategory.Recyclable, true, dirtyMaterial));
            }

            if (!ContainsPrefabNamed(spawner.trashPrefabs, DirtyPaperPrefabName))
            {
                spawner.trashPrefabs = AppendPrefabIfMissing(
                    spawner.trashPrefabs,
                    CreateRuntimeTrashTemplate(DirtyPaperPrefabName, PrimitiveType.Cube, TrashCategory.General, true, dirtyMaterial));
            }

            return;
        }

        spawner.trashPrefabs = new[]
        {
            CreateRuntimeTrashTemplate("Trash_General_Template", PrimitiveType.Capsule, TrashCategory.General, false, generalMaterial),
            CreateRuntimeTrashTemplate("Trash_Plastic_Template", PrimitiveType.Sphere, TrashCategory.Recyclable, false, recyclableMaterial),
            CreateRuntimeTrashTemplate("Trash_Paper_Template", PrimitiveType.Cube, TrashCategory.Recyclable, false, recyclableMaterial),
            CreateRuntimeTrashTemplate("Trash_RawFood_Template", PrimitiveType.Capsule, TrashCategory.Food, false, foodMaterial),
            CreateRuntimeTrashTemplate("Trash_CookedFood_Template", PrimitiveType.Capsule, TrashCategory.Food, false, foodMaterial),
            CreateRuntimeTrashTemplate(DirtyPlasticPrefabName, PrimitiveType.Sphere, TrashCategory.Recyclable, true, dirtyMaterial),
            CreateRuntimeTrashTemplate(DirtyPaperPrefabName, PrimitiveType.Cube, TrashCategory.General, true, dirtyMaterial),
            CreateRuntimeOrganicFoodWasteTemplate(),
            CreateRuntimeTrashTemplate(BananaPeelPrefabName, PrimitiveType.Capsule, TrashCategory.Food, false, foodMaterial),
            CreateRuntimeTrashTemplate(BroccoliPrefabName, PrimitiveType.Sphere, TrashCategory.Food, false, foodMaterial)
        };
    }

    private void ConfigureKnownTrashPrefabs(GameObject[] prefabs)
    {
        if (prefabs == null)
        {
            return;
        }

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            TrashItem item = prefab.GetComponent<TrashItem>();
            if (item == null)
            {
                continue;
            }

            string prefabName = prefab.name;
            if (prefabName.Contains("General"))
            {
                item.itemType = TrashCategory.General;
                item.isDirty = false;
            }
            else if (prefabName.Contains("RawFood") || prefabName.Contains("CookedFood") || prefabName.Contains("FoodWaste") || prefabName.Contains("Organic"))
            {
                item.itemType = TrashCategory.Food;
                item.isDirty = false;
            }
            else if (prefabName.Contains("DirtyPlastic"))
            {
                item.itemType = TrashCategory.Recyclable;
                item.isDirty = true;
            }
            else if (prefabName.Contains("DirtyPaper"))
            {
                item.itemType = TrashCategory.General;
                item.isDirty = true;
            }
            else if (prefabName.Contains("Plastic") || prefabName.Contains("Paper"))
            {
                item.itemType = TrashCategory.Recyclable;
                item.isDirty = false;
            }
        }
    }

    private static GameObject[] AppendPrefabIfMissing(GameObject[] prefabs, GameObject prefabToAdd)
    {
        if (prefabToAdd == null)
        {
            return prefabs;
        }

        List<GameObject> mergedPrefabs = prefabs != null
            ? new List<GameObject>(prefabs)
            : new List<GameObject>();

        foreach (GameObject prefab in mergedPrefabs)
        {
            if (prefab != null && prefab.name == prefabToAdd.name)
            {
                return mergedPrefabs.ToArray();
            }
        }

        mergedPrefabs.Add(prefabToAdd);
        return mergedPrefabs.ToArray();
    }

    private static bool ContainsPrefabNamed(GameObject[] prefabs, string prefabName)
    {
        if (prefabs == null)
        {
            return false;
        }

        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null && prefab.name == prefabName)
            {
                return true;
            }
        }

        return false;
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

    private GameObject CreateRuntimeOrganicFoodWasteTemplate()
    {
        GameObject template = GameObject.Find(OrganicFoodWastePrefabName);
        if (template == null)
        {
            template = new GameObject(OrganicFoodWastePrefabName);
            template.transform.position = new Vector3(0f, -100f, 0f);
            template.SetActive(false);
            AddOrganicFoodWastePart(template.transform, "LeftoverRice", new Vector3(-0.12f, 0.055f, -0.05f), new Vector3(0f, 18f, 0f), new Vector3(0.18f, 0.035f, 0.12f), new Color(0.93f, 0.86f, 0.66f));
            AddOrganicFoodWastePart(template.transform, "VegetablePeel", new Vector3(0.08f, 0.07f, 0.06f), new Vector3(4f, -32f, 11f), new Vector3(0.08f, 0.025f, 0.34f), new Color(0.33f, 0.62f, 0.22f));
            AddOrganicFoodWastePart(template.transform, "SaucePatch", new Vector3(0.02f, 0.035f, -0.09f), new Vector3(0f, 41f, 0f), new Vector3(0.22f, 0.014f, 0.16f), new Color(0.48f, 0.16f, 0.06f));
            AddOrganicFoodWastePart(template.transform, "FoodScrap", new Vector3(0.16f, 0.075f, -0.02f), new Vector3(8f, 26f, -5f), new Vector3(0.11f, 0.045f, 0.16f), new Color(0.8f, 0.34f, 0.16f));
        }

        TrashItem trashItem = template.GetComponent<TrashItem>();
        if (trashItem == null)
        {
            trashItem = template.AddComponent<TrashItem>();
        }

        trashItem.itemType = TrashCategory.Food;
        trashItem.isDirty = false;
        trashItem.isCompoundItem = true;

        Rigidbody body = template.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = template.AddComponent<Rigidbody>();
        }

        body.mass = 0.55f;
        body.linearDamping = 1f;
        body.angularDamping = 1f;

        BoxCollider collider = template.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = template.AddComponent<BoxCollider>();
        }

        collider.size = new Vector3(0.72f, 0.12f, 0.46f);
        collider.center = new Vector3(0f, 0.06f, 0f);
        return template;
    }

    private void AddOrganicFoodWastePart(Transform parent, string objectName, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEulerAngles);
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateRuntimeMaterial("Runtime_" + objectName, color);
        }
    }

    private readonly struct LevelConfig
    {
        public readonly int levelNumber;
        public readonly int itemCount;
        public readonly bool includeDirty;
        public readonly bool includeFood;
        public readonly int minimumDirty;
        public readonly bool spotlightEnabled;
        public readonly bool washingLesson;
        public readonly string introMessage;

        public LevelConfig(
            int levelNumber,
            int itemCount,
            bool includeDirty,
            bool includeFood,
            int minimumDirty,
            bool spotlightEnabled,
            bool washingLesson,
            string introMessage)
        {
            this.levelNumber = levelNumber;
            this.itemCount = itemCount;
            this.includeDirty = includeDirty;
            this.includeFood = includeFood;
            this.minimumDirty = minimumDirty;
            this.spotlightEnabled = spotlightEnabled;
            this.washingLesson = washingLesson;
            this.introMessage = introMessage;
        }
    }
}
