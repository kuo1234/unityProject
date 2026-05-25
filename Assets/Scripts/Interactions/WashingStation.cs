using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WashingStation : MonoBehaviour
{
    private const float ProgressTextSize = 0.075f;
    private const float ProgressBarWidth = 0.85f;
    private static readonly Color WashTextColor = new Color(0.08f, 0.12f, 0.16f);
    private static readonly Color WashCompleteTextColor = new Color(0.05f, 0.32f, 0.12f);

    public Material cleanMaterial;
    public TextMesh progressText;
    public float washDuration = 2f;

    private readonly Dictionary<TrashItem, float> washTimers = new Dictionary<TrashItem, float>();
    private Transform progressBarBack;
    private Transform progressBarFill;
    private Renderer progressBarFillRenderer;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        EnsureProgressText();
        EnsureProgressBar();
    }

    private void OnTriggerStay(Collider other)
    {
        TrashItem trashItem = other.GetComponentInParent<TrashItem>();
        if (trashItem == null)
        {
            return;
        }

        if (!trashItem.isDirty)
        {
            washTimers.Remove(trashItem);
            UpdateProgressText(0f, false);
            return;
        }

        washTimers.TryGetValue(trashItem, out float elapsedTime);
        elapsedTime += Time.deltaTime;

        if (elapsedTime < washDuration)
        {
            washTimers[trashItem] = elapsedTime;
            UpdateProgressText(elapsedTime / washDuration, true);
            return;
        }

        trashItem.isDirty = false;
        PlaceWashedTrashOnStation(trashItem);

        WashableVisualState washableVisualState = trashItem.GetComponent<WashableVisualState>();
        if (washableVisualState != null)
        {
            washableVisualState.ApplyClean();
        }

        MeshRenderer meshRenderer = trashItem.GetComponent<MeshRenderer>();
        if (washableVisualState == null && meshRenderer != null && cleanMaterial != null)
        {
            meshRenderer.material = cleanMaterial;
        }

        washTimers.Remove(trashItem);
        UpdateProgressText(1f, true);
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RaiseFeedback("Cleaned item", true);
        }

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.NotifyPlayerAction(PlayerGuidanceAction.WashedTrash, trashItem);
        }

        Debug.Log("[Wash] Item is now clean");
    }

    private void OnTriggerExit(Collider other)
    {
        TrashItem trashItem = other.GetComponentInParent<TrashItem>();
        if (trashItem != null)
        {
            washTimers.Remove(trashItem);
        }

        if (washTimers.Count == 0)
        {
            UpdateProgressText(0f, false);
        }
    }

    private void EnsureProgressText()
    {
        if (progressText != null)
        {
            return;
        }

        GameObject textObject = new GameObject("WashProgressText");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        textObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

        progressText = textObject.AddComponent<TextMesh>();
        progressText.alignment = TextAlignment.Center;
        progressText.anchor = TextAnchor.MiddleCenter;
        progressText.fontSize = 64;
        progressText.characterSize = ProgressTextSize;
        progressText.color = WashTextColor;
        progressText.text = "Wash dirty items";
    }

    private void EnsureProgressBar()
    {
        if (progressBarFill != null)
        {
            return;
        }

        progressBarBack = CreateProgressBarPart("WashProgressBack", new Color(0.05f, 0.12f, 0.16f, 0.85f));
        progressBarFill = CreateProgressBarPart("WashProgressFill", new Color(0.2f, 0.95f, 1f, 0.95f));
        progressBarFillRenderer = progressBarFill.GetComponent<Renderer>();
        UpdateProgressText(0f, false);
    }

    private Transform CreateProgressBarPart(string objectName, Color color)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = objectName;
        bar.transform.SetParent(transform, false);
        bar.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        bar.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
        bar.transform.localScale = new Vector3(ProgressBarWidth, 0.055f, 0.025f);

        Collider barCollider = bar.GetComponent<Collider>();
        if (barCollider != null)
        {
            Destroy(barCollider);
        }

        Renderer renderer = bar.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateBarMaterial(color);
        return bar.transform;
    }

    private Material CreateBarMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.name = "Runtime_Wash_Progress";
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }

    private void UpdateProgressText(float progress, bool active)
    {
        if (progressText == null)
        {
            return;
        }

        if (!active)
        {
            progressText.text = "Wash dirty items";
            progressText.color = WashTextColor;
            SetProgressBar(0f, false);
            return;
        }

        int percent = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f);
        progressText.text = $"Washing {percent}%";
        progressText.color = progress >= 1f ? WashCompleteTextColor : WashTextColor;
        SetProgressBar(progress, true);
    }

    private void PlaceWashedTrashOnStation(TrashItem trashItem)
    {
        if (trashItem == null)
        {
            return;
        }

        Rigidbody itemBody = trashItem.GetComponent<Rigidbody>();
        if (itemBody != null)
        {
            itemBody.linearVelocity = Vector3.zero;
            itemBody.angularVelocity = Vector3.zero;
        }

        Collider stationCollider = GetComponent<Collider>();
        Bounds stationBounds = stationCollider != null ? stationCollider.bounds : GetObjectBounds(gameObject);
        Bounds itemBounds = GetObjectBounds(trashItem.gameObject);
        Vector3 position = trashItem.transform.position;
        position.x = Mathf.Clamp(position.x, stationBounds.min.x + 0.12f, stationBounds.max.x - 0.12f);
        position.z = Mathf.Clamp(position.z, stationBounds.min.z + 0.12f, stationBounds.max.z - 0.12f);
        position.y += stationBounds.max.y + 0.04f - itemBounds.min.y;
        trashItem.transform.position = position;
        CreateWashedTrashRest(trashItem.transform, stationBounds, itemBounds);
    }

    private void CreateWashedTrashRest(Transform trashTransform, Bounds stationBounds, Bounds itemBounds)
    {
        GameObject rest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rest.name = "WashedTrashRest";
        rest.transform.position = new Vector3(
            trashTransform.position.x,
            stationBounds.max.y + 0.02f,
            trashTransform.position.z);
        rest.transform.localScale = new Vector3(
            Mathf.Clamp(itemBounds.size.x * 1.35f, 0.42f, 0.9f),
            0.04f,
            Mathf.Clamp(itemBounds.size.z * 1.35f, 0.42f, 0.9f));

        Renderer renderer = rest.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        WashedTrashRest tracker = rest.AddComponent<WashedTrashRest>();
        tracker.Track(trashTransform);
    }

    private static Bounds GetObjectBounds(GameObject target)
    {
        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        Collider collider = target.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            return collider.bounds;
        }

        return new Bounds(target.transform.position, target.transform.lossyScale);
    }

    private void SetProgressBar(float progress, bool active)
    {
        if (progressBarBack != null)
        {
            progressBarBack.gameObject.SetActive(active);
        }

        if (progressBarFill == null)
        {
            return;
        }

        progressBarFill.gameObject.SetActive(active);
        if (!active)
        {
            return;
        }

        float clampedProgress = Mathf.Clamp01(progress);
        progressBarFill.localScale = new Vector3(Mathf.Max(0.02f, ProgressBarWidth * clampedProgress), 0.065f, 0.035f);
        progressBarFill.localPosition = new Vector3(((clampedProgress - 1f) * ProgressBarWidth) * 0.5f, 0.9f, -0.015f);

        if (progressBarFillRenderer != null)
        {
            progressBarFillRenderer.sharedMaterial.color = progress >= 1f ? Color.green : Color.cyan;
        }
    }
}

public class WashedTrashRest : MonoBehaviour
{
    private const float MaxDistanceBeforeCleanup = 0.85f;
    private const float MaxLifetime = 18f;

    private Transform trackedTrash;
    private float cleanupTime;

    public void Track(Transform trashTransform)
    {
        trackedTrash = trashTransform;
        cleanupTime = Time.time + MaxLifetime;
    }

    private void Update()
    {
        if (trackedTrash == null || Time.time >= cleanupTime)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 offset = trackedTrash.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > MaxDistanceBeforeCleanup * MaxDistanceBeforeCleanup)
        {
            Destroy(gameObject);
        }
    }
}
