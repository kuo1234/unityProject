using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class BinValidator : MonoBehaviour
{
    public TrashCategory targetCategory;

    private const float BounceForce = 6f;
    private const float UpwardForce = 4f;
    private static readonly Color ErrorColor = new Color(1f, 0.08f, 0.04f);

    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine feedbackRoutine;
    private readonly Dictionary<TrashItem, float> rejectedItems = new Dictionary<TrashItem, float>();

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ScoreManager.Instance != null && !ScoreManager.Instance.IsRoundActive)
        {
            return;
        }

        TrashItem trashItem = other.GetComponentInParent<TrashItem>();
        if (trashItem == null)
        {
            return;
        }

        if (trashItem.isResolved)
        {
            return;
        }

        if (trashItem.itemType == targetCategory && !trashItem.isDirty)
        {
            trashItem.isResolved = true;
            Destroy(trashItem.gameObject);
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddSortedItem();
            }

            Debug.Log("[Success] Sorted correctly");
            return;
        }

        if (rejectedItems.TryGetValue(trashItem, out float rejectUntilTime) && Time.time < rejectUntilTime)
        {
            return;
        }

        rejectedItems[trashItem] = Time.time + 0.75f;
        TriggerWrongBinFeedback();

        Rigidbody itemRigidbody = trashItem.GetComponent<Rigidbody>();
        if (itemRigidbody != null)
        {
            Vector3 outwardDirection = (trashItem.transform.position - transform.position).normalized;
            Vector3 bounceDirection = (outwardDirection * BounceForce) + (Vector3.up * UpwardForce);
            itemRigidbody.AddForce(bounceDirection, ForceMode.Impulse);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddSortingMistake(trashItem.isDirty ? "Wash first" : "Wrong bin");
        }

#if OCULUS_INTEGRATION || META_XR_CORE_SDK || OVRPLUGIN_PRESENT
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.All);
#else
        Debug.LogWarning("OVRInput is not available. Install or enable the Meta XR SDK to use haptic error feedback.");
#endif
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;
            originalColors[i] = material.color;
        }
    }

    private void TriggerWrongBinFeedback()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(FlashWrongBin());
    }

    private IEnumerator FlashWrongBin()
    {
        if (renderers == null || renderers.Length == 0)
        {
            CacheRenderers();
        }

        Vector3 originalScale = transform.localScale;

        for (int pulse = 0; pulse < 3; pulse++)
        {
            SetRendererColors(ErrorColor);
            transform.localScale = originalScale * 1.06f;
            yield return new WaitForSeconds(0.12f);

            RestoreRendererColors();
            transform.localScale = originalScale;
            yield return new WaitForSeconds(0.1f);
        }

        feedbackRoutine = null;
    }

    private void SetRendererColors(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = color;
        }
    }

    private void RestoreRendererColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }
    }
}
