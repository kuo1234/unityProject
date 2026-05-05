using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BinValidator : MonoBehaviour
{
    public TrashCategory targetCategory;
    public Material acceptedFlashMaterial;
    public Material rejectedFlashMaterial;

    private const float BounceForce = 6f;
    private const float UpwardForce = 4f;
    private const float FlashSeconds = 0.35f;

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
            Destroy(trashItem.gameObject);
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddSortedItem();
            }

            Debug.Log("[Success] Sorted correctly");
            return;
        }

        Rigidbody itemRigidbody = trashItem.GetComponent<Rigidbody>();
        if (itemRigidbody != null)
        {
            Vector3 outwardDirection = (trashItem.transform.position - transform.position).normalized;
            Vector3 bounceDirection = (outwardDirection * BounceForce) + (Vector3.up * UpwardForce);
            itemRigidbody.AddForce(bounceDirection, ForceMode.Impulse);
        }

        FlashBin(false);
        if (ScoreManager.Instance != null)
        {
            string reason = trashItem.isDirty ? "Wash dirty items first" : "Wrong bin";
            ScoreManager.Instance.AddSortingMistake(reason);
        }

#if OCULUS_INTEGRATION || META_XR_CORE_SDK || OVRPLUGIN_PRESENT
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.All);
#else
        Debug.LogWarning("OVRInput is not available. Install or enable the Meta XR SDK to use haptic error feedback.");
#endif
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
