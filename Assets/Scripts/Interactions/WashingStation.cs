using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WashingStation : MonoBehaviour
{
    public Material cleanMaterial;

    private const float WashDuration = 2f;
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
            return;
        }

        washTimers.TryGetValue(trashItem, out float elapsedTime);
        elapsedTime += Time.deltaTime;

        if (elapsedTime < WashDuration)
        {
            washTimers[trashItem] = elapsedTime;
            return;
        }

        trashItem.isDirty = false;

        MeshRenderer[] meshRenderers = trashItem.GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers.Length > 0 && cleanMaterial != null)
        {
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.material = cleanMaterial;
            }
        }

        washTimers.Remove(trashItem);
        Debug.Log("[Wash] Item is now clean");
    }

    private void OnTriggerExit(Collider other)
    {
        TrashItem trashItem = other.GetComponentInParent<TrashItem>();
        if (trashItem != null)
        {
            washTimers.Remove(trashItem);
        }
    }
}
