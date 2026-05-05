using System.Collections;
using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    public GameObject[] trashPrefabs;
    public Transform spawnPoint;
    public float spawnInterval = 2f;
    public int maxActiveTrash = 8;

    private const float SpawnForce = 0.5f;
    private bool spawningEnabled;

    private void Start()
    {
        StartCoroutine(SpawnTrashRoutine());
    }

    private IEnumerator SpawnTrashRoutine()
    {
        while (true)
        {
            SpawnTrash();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnTrash()
    {
        if (!spawningEnabled)
        {
            return;
        }

        if (spawnPoint == null || trashPrefabs == null || trashPrefabs.Length == 0)
        {
            return;
        }

        if (Object.FindObjectsByType<TrashItem>(FindObjectsInactive.Exclude).Length >= maxActiveTrash)
        {
            return;
        }

        GameObject prefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];
        if (prefab == null)
        {
            return;
        }

        GameObject spawnedTrash = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        spawnedTrash.SetActive(true);

        Rigidbody spawnedRigidbody = spawnedTrash.GetComponent<Rigidbody>();

        if (spawnedRigidbody != null)
        {
            StartCoroutine(ApplyLocalZForce(spawnedRigidbody, spawnedTrash.transform));
        }
    }

    private IEnumerator ApplyLocalZForce(Rigidbody targetRigidbody, Transform targetTransform)
    {
        while (targetRigidbody != null && targetTransform != null)
        {
            targetRigidbody.AddForce(targetTransform.forward * SpawnForce, ForceMode.Acceleration);
            yield return new WaitForFixedUpdate();
        }
    }

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
    }
}
