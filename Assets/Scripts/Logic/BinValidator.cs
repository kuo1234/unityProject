using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BinValidator : MonoBehaviour
{
    public TrashCategory targetCategory;

    private const float BounceForce = 6f;
    private const float UpwardForce = 4f;

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
        TrashItem trashItem = other.GetComponent<TrashItem>();
        if (trashItem == null)
        {
            return;
        }

        if (trashItem.itemType == targetCategory && !trashItem.isDirty)
        {
            Destroy(other.gameObject);
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddSortedItem();
            }

            Debug.Log("[Success] Sorted correctly");
            return;
        }

        Rigidbody itemRigidbody = other.GetComponent<Rigidbody>();
        if (itemRigidbody != null)
        {
            Vector3 outwardDirection = (other.transform.position - transform.position).normalized;
            Vector3 bounceDirection = (outwardDirection * BounceForce) + (Vector3.up * UpwardForce);
            itemRigidbody.AddForce(bounceDirection, ForceMode.Impulse);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddSortingMistake();
        }

#if OCULUS_INTEGRATION || META_XR_CORE_SDK || OVRPLUGIN_PRESENT
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.All);
#else
        Debug.LogWarning("OVRInput is not available. Install or enable the Meta XR SDK to use haptic error feedback.");
#endif
    }
}
