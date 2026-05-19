using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BinValidator : MonoBehaviour
{
    public TrashCategory targetCategory;
    public Material acceptedFlashMaterial;
    public Material rejectedFlashMaterial;

    private const float FlashSeconds = 0.35f;
    private const float FallbackRespawnDistance = 1.8f;
    private const float FallbackRespawnHeight = 0.7f;

    private Material originalMaterial;
    private Coroutine flashRoutine;

    private void Awake()
    {
        Renderer binRenderer = GetComponent<Renderer>();
        if (binRenderer != null)
        {
            originalMaterial = binRenderer.sharedMaterial;
        }
    }

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

    private void OnTriggerEnter(Collider other)
    {
        TrashItem trashItem = other.GetComponentInParent<TrashItem>();
        if (trashItem == null)
        {
            return;
        }

        if (trashItem.itemType == targetCategory && !trashItem.isDirty)
        {
            FlashBin(true);
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.PlaySortCelebration(transform.position);
            }

            Destroy(trashItem.gameObject);
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddSortedItem();
            }

            Debug.Log("[Success] Sorted correctly");
            return;
        }

        RespawnRejectedTrash(trashItem);
        FlashBin(false);
        if (ScoreManager.Instance != null)
        {
            string reason = trashItem.isDirty ? "Wash dirty items first" : $"Wrong bin: try {trashItem.itemType}";
            ScoreManager.Instance.AddSortingMistake(reason);
        }

#if OCULUS_INTEGRATION || META_XR_CORE_SDK || OVRPLUGIN_PRESENT
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.All);
#else
        Debug.LogWarning("OVRInput is not available. Install or enable the Meta XR SDK to use haptic error feedback.");
#endif
    }

    private void RespawnRejectedTrash(TrashItem trashItem)
    {
        if (trashItem == null)
        {
            return;
        }

        Rigidbody itemRigidbody = trashItem.GetComponent<Rigidbody>();
        Transform respawnPoint = FindRespawnPoint();
        Vector3 respawnPosition;
        Quaternion respawnRotation;

        if (respawnPoint != null)
        {
            respawnPosition = respawnPoint.position;
            respawnRotation = respawnPoint.rotation;
        }
        else
        {
            Vector3 outwardDirection = trashItem.transform.position - transform.position;
            outwardDirection.y = 0f;

            if (outwardDirection.sqrMagnitude <= 0.001f)
            {
                outwardDirection = -transform.forward;
            }

            respawnPosition = transform.position +
                (outwardDirection.normalized * FallbackRespawnDistance) +
                (Vector3.up * FallbackRespawnHeight);
            respawnRotation = trashItem.transform.rotation;
        }

        if (itemRigidbody != null)
        {
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
            itemRigidbody.position = respawnPosition;
            itemRigidbody.rotation = respawnRotation;
        }
        else
        {
            trashItem.transform.SetPositionAndRotation(respawnPosition, respawnRotation);
        }
    }

    private Transform FindRespawnPoint()
    {
        foreach (TrashSpawner spawner in FindObjectsByType<TrashSpawner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (spawner != null && spawner.spawnPoint != null)
            {
                return spawner.spawnPoint;
            }
        }

        return null;
    }

    private void FlashBin(bool accepted)
    {
        Renderer binRenderer = GetComponent<Renderer>();
        Material flashMaterial = accepted ? acceptedFlashMaterial : rejectedFlashMaterial;
        if (binRenderer != null && flashMaterial != null)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine(binRenderer, flashMaterial));
        }
    }

    private IEnumerator FlashRoutine(Renderer binRenderer, Material flashMaterial)
    {
        binRenderer.sharedMaterial = flashMaterial;
        yield return new WaitForSeconds(FlashSeconds);

        if (binRenderer != null && originalMaterial != null)
        {
            binRenderer.sharedMaterial = originalMaterial;
        }

        flashRoutine = null;
    }
}
