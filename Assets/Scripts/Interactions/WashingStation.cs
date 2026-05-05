using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WashingStation : MonoBehaviour
{
    private const float ProgressTextSize = 0.075f;

    public Material cleanMaterial;
    public TextMesh progressText;
    public float washDuration = 2f;

    private readonly Dictionary<TrashItem, float> washTimers = new Dictionary<TrashItem, float>();

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

        MeshRenderer meshRenderer = trashItem.GetComponent<MeshRenderer>();
        if (meshRenderer != null && cleanMaterial != null)
        {
            meshRenderer.material = cleanMaterial;
        }

        washTimers.Remove(trashItem);
        UpdateProgressText(1f, true);
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RaiseFeedback("Cleaned item", true);
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
        progressText.color = Color.cyan;
        progressText.text = "Wash dirty items";
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
            progressText.color = Color.cyan;
            return;
        }

        int percent = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f);
        progressText.text = $"Washing {percent}%";
        progressText.color = progress >= 1f ? Color.green : Color.cyan;
    }
}
