using UnityEngine;

public class DeconstructableItem : MonoBehaviour
{
    public Transform lidObject;

    private const float SeparationDistance = 0.2f;

    private void Update()
    {
        if (lidObject == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, lidObject.position);
        if (distance <= SeparationDistance)
        {
            return;
        }

        lidObject.SetParent(null);

        Rigidbody lidRigidbody = lidObject.GetComponent<Rigidbody>();
        if (lidRigidbody != null)
        {
            lidRigidbody.isKinematic = false;
        }

        Debug.Log("[Deconstruct] Lid separated");
        enabled = false;
    }
}
