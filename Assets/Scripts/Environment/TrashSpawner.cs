using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    public GameObject[] trashPrefabs;
    public Transform spawnPoint;
    public float spawnInterval = 2.4f;
    public int maxActiveTrash = 3;
    public bool childFriendlyStagedSpawning = true;
    public bool batchSpawnOnDemand = true;
    public float spawnScatterRadius = 2.2f;
    public float spawnScatterForwardOffset = 0.8f;
    public float initialScatterImpulse = 0.85f;
    public float batchSpawnMinDistance = 1.05f;
    public float groundPadding = 0.03f;
    public Vector2 safeSpawnCenter = new Vector2(0f, 1.1f);
    public Vector2 safeSpawnHalfSize = new Vector2(3.8f, 3.25f);
    public Vector2 binExclusionCenter = new Vector2(0f, 4f);
    public Vector2 binExclusionHalfSize = new Vector2(3.7f, 1.15f);

    private static readonly Vector2[] BatchSpawnPattern =
    {
        new Vector2(0f, 1.25f),
        new Vector2(-1.15f, 0.95f),
        new Vector2(1.15f, 0.95f),
        new Vector2(-2.05f, 0.35f),
        new Vector2(2.05f, 0.35f),
        new Vector2(-0.62f, 0.25f),
        new Vector2(0.62f, 0.25f),
        new Vector2(-1.7f, 1.65f),
        new Vector2(1.7f, 1.65f),
        new Vector2(0f, 0.05f)
    };

    private bool spawningEnabled;
    private int tutorialStage;

    private void Start()
    {
        if (childFriendlyStagedSpawning)
        {
            spawnInterval = Mathf.Max(spawnInterval, 2.4f);
            maxActiveTrash = Mathf.Max(maxActiveTrash, 3);
        }

        if (!batchSpawnOnDemand)
        {
            StartCoroutine(SpawnTrashRoutine());
        }
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

        if (Object.FindObjectsByType<TrashItem>(FindObjectsInactive.Exclude).Length >= GetActiveTrashLimit())
        {
            return;
        }

        GameObject prefab = ChoosePrefab();
        if (prefab == null)
        {
            return;
        }

        InstantiateTrash(prefab, GetScatteredSpawnPosition(), true);
    }

    public GameObject[] SpawnUniqueBatch(int count)
    {
        return SpawnUniqueBatch(count, true, true);
    }

    public GameObject[] SpawnUniqueBatch(int count, bool includeDirty, bool includeFood)
    {
        return SpawnUniqueBatch(count, includeDirty, includeFood, 0);
    }

    public GameObject[] SpawnUniqueBatch(int count, bool includeDirty, bool includeFood, int minimumDirty)
    {
        if (spawnPoint == null || trashPrefabs == null || trashPrefabs.Length == 0 || count <= 0)
        {
            return System.Array.Empty<GameObject>();
        }

        List<GameObject> candidates = GetUniquePrefabCandidates(includeDirty, includeFood);
        List<GameObject> selectedPrefabs = SelectBatchPrefabs(candidates, count, includeDirty ? minimumDirty : 0);

        if (candidates.Count < count)
        {
            Debug.LogWarning($"[TrashSpawner] Requested {count} unique trash items, but only {candidates.Count} usable prefabs are configured.");
        }

        int spawnCount = selectedPrefabs.Count;
        List<GameObject> spawnedItems = new List<GameObject>(spawnCount);
        List<Vector3> usedPositions = new List<Vector3>(spawnCount);
        List<Vector3> patternedPositions = GetPatternedBatchPositions(spawnCount);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 position = i < patternedPositions.Count
                ? patternedPositions[i]
                : GetBatchSpawnPosition(usedPositions);
            GameObject spawned = InstantiateTrash(selectedPrefabs[i], position, false);
            if (spawned == null)
            {
                continue;
            }

            usedPositions.Add(spawned.transform.position);
            spawnedItems.Add(spawned);
        }

        ArrangeSpawnedBatch(spawnedItems);
        StartCoroutine(HoldBatchLayoutBriefly(spawnedItems));
        return spawnedItems.ToArray();
    }

    private List<GameObject> SelectBatchPrefabs(List<GameObject> candidates, int count, int minimumDirty)
    {
        List<GameObject> selected = new List<GameObject>(Mathf.Min(count, candidates.Count));
        List<GameObject> dirtyCandidates = new List<GameObject>();

        foreach (GameObject candidate in candidates)
        {
            TrashItem item = candidate != null ? candidate.GetComponent<TrashItem>() : null;
            if (item != null && item.isDirty)
            {
                dirtyCandidates.Add(candidate);
            }
        }

        Shuffle(dirtyCandidates);
        int dirtyTarget = Mathf.Min(minimumDirty, count, dirtyCandidates.Count);
        if (minimumDirty > 0 && dirtyCandidates.Count < minimumDirty)
        {
            Debug.LogWarning($"[TrashSpawner] Requested {minimumDirty} dirty trash items, but only {dirtyCandidates.Count} dirty prefabs are configured.");
        }

        for (int i = 0; i < dirtyTarget; i++)
        {
            selected.Add(dirtyCandidates[i]);
        }

        foreach (GameObject selectedPrefab in selected)
        {
            candidates.Remove(selectedPrefab);
        }

        Shuffle(candidates);
        while (selected.Count < count && candidates.Count > 0)
        {
            selected.Add(candidates[0]);
            candidates.RemoveAt(0);
        }

        Shuffle(selected);
        return selected;
    }

    private List<Vector3> GetPatternedBatchPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>(Mathf.Min(count, BatchSpawnPattern.Length));
        float y = spawnPoint != null ? spawnPoint.position.y : transform.position.y;

        for (int i = 0; i < BatchSpawnPattern.Length && positions.Count < count; i++)
        {
            Vector2 offset = BatchSpawnPattern[i];
            Vector3 candidate = new Vector3(
                safeSpawnCenter.x + offset.x,
                y,
                safeSpawnCenter.y + offset.y);

            candidate = ClampToSafeSpawnArea(candidate);
            if (IsInsideBinExclusionArea(candidate) || !HasEnoughSpacing(candidate, positions))
            {
                continue;
            }

            positions.Add(candidate);
        }

        return positions;
    }

    private void ArrangeSpawnedBatch(List<GameObject> spawnedItems)
    {
        if (spawnedItems == null || spawnedItems.Count == 0)
        {
            return;
        }

        List<Vector3> patternedPositions = GetPatternedBatchPositions(spawnedItems.Count);
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] == null)
            {
                continue;
            }

            Vector3 position = i < patternedPositions.Count
                ? patternedPositions[i]
                : GetBatchSpawnPosition(patternedPositions);
            PlaceAtVisibleBatchSlot(spawnedItems[i], position, i);
        }
    }

    private IEnumerator HoldBatchLayoutBriefly(List<GameObject> spawnedItems)
    {
        float endTime = Time.time + 0.45f;
        while (Time.time < endTime)
        {
            ArrangeSpawnedBatch(spawnedItems);
            yield return new WaitForFixedUpdate();
        }
    }

    private void PlaceAtVisibleBatchSlot(GameObject spawnedTrash, Vector3 position, int index)
    {
        if (spawnedTrash == null)
        {
            return;
        }

        spawnedTrash.transform.position = position;
        spawnedTrash.transform.rotation = Quaternion.Euler(0f, 22.5f * index, 0f);
        PlaceOnGround(spawnedTrash);

        Rigidbody body = spawnedTrash.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.Sleep();
    }

    private int GetActiveTrashLimit()
    {
        return childFriendlyStagedSpawning ? Mathf.Clamp(maxActiveTrash, 1, 3) : Mathf.Max(1, maxActiveTrash);
    }

    private Vector3 GetScatteredSpawnPosition()
    {
        if (spawnPoint == null)
        {
            return transform.position;
        }

        for (int i = 0; i < 12; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnScatterRadius;
            Vector3 rightOffset = spawnPoint.right * randomCircle.x;
            Vector3 forwardOffset = spawnPoint.forward * (spawnScatterForwardOffset + randomCircle.y);
            Vector3 candidate = spawnPoint.position + rightOffset + forwardOffset;
            if (IsInsideSafeSpawnArea(candidate) && !IsInsideBinExclusionArea(candidate))
            {
                return candidate;
            }
        }

        return GetRandomSafeGroundPosition();
    }

    private Vector3 GetBatchSpawnPosition(List<Vector3> usedPositions)
    {
        for (int i = 0; i < 32; i++)
        {
            Vector3 candidate = GetRandomSafeGroundPosition();

            if (HasEnoughSpacing(candidate, usedPositions))
            {
                return candidate;
            }
        }

        return GetRandomSafeGroundPosition();
    }

    private Vector3 GetRandomSafeGroundPosition()
    {
        for (int i = 0; i < 32; i++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(safeSpawnCenter.x - safeSpawnHalfSize.x, safeSpawnCenter.x + safeSpawnHalfSize.x),
                spawnPoint != null ? spawnPoint.position.y : transform.position.y,
                Random.Range(safeSpawnCenter.y - safeSpawnHalfSize.y, safeSpawnCenter.y + safeSpawnHalfSize.y));

            if (!IsInsideBinExclusionArea(candidate))
            {
                return candidate;
            }
        }

        return new Vector3(
            safeSpawnCenter.x,
            spawnPoint != null ? spawnPoint.position.y : transform.position.y,
            safeSpawnCenter.y - (safeSpawnHalfSize.y * 0.45f));
    }

    private bool HasEnoughSpacing(Vector3 candidate, List<Vector3> usedPositions)
    {
        float minDistanceSqr = batchSpawnMinDistance * batchSpawnMinDistance;
        foreach (Vector3 usedPosition in usedPositions)
        {
            Vector2 candidateXz = new Vector2(candidate.x, candidate.z);
            Vector2 usedXz = new Vector2(usedPosition.x, usedPosition.z);
            if ((candidateXz - usedXz).sqrMagnitude < minDistanceSqr)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsInsideSafeSpawnArea(Vector3 position)
    {
        return position.x >= safeSpawnCenter.x - safeSpawnHalfSize.x &&
               position.x <= safeSpawnCenter.x + safeSpawnHalfSize.x &&
               position.z >= safeSpawnCenter.y - safeSpawnHalfSize.y &&
               position.z <= safeSpawnCenter.y + safeSpawnHalfSize.y;
    }

    private Vector3 ClampToSafeSpawnArea(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, safeSpawnCenter.x - safeSpawnHalfSize.x, safeSpawnCenter.x + safeSpawnHalfSize.x);
        position.z = Mathf.Clamp(position.z, safeSpawnCenter.y - safeSpawnHalfSize.y, safeSpawnCenter.y + safeSpawnHalfSize.y);
        return position;
    }

    private bool IsInsideBinExclusionArea(Vector3 position)
    {
        return position.x >= binExclusionCenter.x - binExclusionHalfSize.x &&
               position.x <= binExclusionCenter.x + binExclusionHalfSize.x &&
               position.z >= binExclusionCenter.y - binExclusionHalfSize.y &&
               position.z <= binExclusionCenter.y + binExclusionHalfSize.y;
    }

    private void ApplyScatterImpulse(Rigidbody targetRigidbody, Transform targetTransform)
    {
        if (targetRigidbody == null || targetTransform == null)
        {
            return;
        }

        Vector3 randomDirection = (targetTransform.forward + new Vector3(Random.Range(-0.55f, 0.55f), 0.2f, Random.Range(-0.35f, 0.35f))).normalized;
        targetRigidbody.AddForce(randomDirection * initialScatterImpulse, ForceMode.Impulse);
    }

    private GameObject InstantiateTrash(GameObject prefab, Vector3 position, bool applyImpulse)
    {
        if (prefab == null)
        {
            return null;
        }

        Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * spawnPoint.rotation;
        GameObject spawnedTrash = Instantiate(prefab, position, spawnRotation);
        spawnedTrash.SetActive(true);
        PlaceOnGround(spawnedTrash);

        Rigidbody spawnedRigidbody = spawnedTrash.GetComponent<Rigidbody>();
        if (spawnedRigidbody != null)
        {
            spawnedRigidbody.linearVelocity = Vector3.zero;
            spawnedRigidbody.angularVelocity = Vector3.zero;
            spawnedRigidbody.Sleep();

            if (applyImpulse)
            {
                ApplyScatterImpulse(spawnedRigidbody, spawnedTrash.transform);
            }
        }

        return spawnedTrash;
    }

    private void PlaceOnGround(GameObject spawnedTrash)
    {
        if (spawnedTrash == null)
        {
            return;
        }

        Bounds bounds = GetObjectBounds(spawnedTrash);
        Vector3 position = spawnedTrash.transform.position;
        position.y += groundPadding - bounds.min.y;
        spawnedTrash.transform.position = position;
    }

    private static Bounds GetObjectBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
        {
            return bounds;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (hasBounds)
        {
            return bounds;
        }

        return new Bounds(target.transform.position, target.transform.lossyScale);
    }

    private List<GameObject> GetUniquePrefabCandidates(bool includeDirty, bool includeFood)
    {
        List<GameObject> candidates = new List<GameObject>();

        foreach (GameObject prefab in trashPrefabs)
        {
            if (prefab == null || candidates.Contains(prefab))
            {
                continue;
            }

            TrashItem item = prefab.GetComponent<TrashItem>();
            if (item == null)
            {
                continue;
            }

            if (!includeDirty && item.isDirty)
            {
                continue;
            }

            if (!includeFood && item.itemType == TrashCategory.Food)
            {
                continue;
            }

            candidates.Add(prefab);
        }

        return candidates;
    }

    private static void Shuffle(List<GameObject> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            int swapIndex = Random.Range(i, items.Count);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }
    }

    private GameObject ChoosePrefab()
    {
        if (!childFriendlyStagedSpawning)
        {
            return trashPrefabs[Random.Range(0, trashPrefabs.Length)];
        }

        GameObject[] candidates = new GameObject[trashPrefabs.Length];
        int candidateCount = 0;

        foreach (GameObject prefab in trashPrefabs)
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

            bool allowed = tutorialStage switch
            {
                0 => !item.isDirty && item.itemType != TrashCategory.Food,
                1 => !item.isDirty,
                _ => true
            };

            if (allowed)
            {
                candidates[candidateCount] = prefab;
                candidateCount++;
            }
        }

        if (candidateCount == 0)
        {
            return trashPrefabs[Random.Range(0, trashPrefabs.Length)];
        }

        return candidates[Random.Range(0, candidateCount)];
    }

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
    }

    public void SetTutorialStage(int stage)
    {
        tutorialStage = Mathf.Max(0, stage);
    }

    public GameObject SpawnSpecificCategory(TrashCategory category, bool dirty)
    {
        if (spawnPoint == null || trashPrefabs == null)
        {
            return null;
        }

        foreach (GameObject prefab in trashPrefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            TrashItem item = prefab.GetComponent<TrashItem>();
            if (item == null || item.itemType != category || item.isDirty != dirty)
            {
                continue;
            }

            return InstantiateTrash(prefab, GetScatteredSpawnPosition(), true);
        }

        return null;
    }
}
