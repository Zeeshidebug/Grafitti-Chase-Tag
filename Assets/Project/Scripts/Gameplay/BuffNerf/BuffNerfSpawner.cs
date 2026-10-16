using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffNerfSpawner : MonoBehaviour
{
    [Header("Token Prefabs")]
    [SerializeField] private List<BuffNerfToken> tokenPrefabs = new();

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider spawnArea;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Spawn Settings")]
    [SerializeField] private int tokensPerBatch = 5;
    [SerializeField] private float spawnInterval = 20f;
    [SerializeField] private float minSpawnDistance = 3f;
    [SerializeField] private int maxAttemptsPerToken = 30;
    [SerializeField] private float raycastHeight = 50f;
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private float clearanceRadius = 0.5f;
    [SerializeField] private float groundOffset = 0.1f;

    private readonly List<GameObject> spawnedTokens = new();

    private Coroutine spawnRoutine;
    private bool isSpawning;

    public void StartSpawning()
    {
        if (isSpawning)
            return;

        isSpawning = true;
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        isSpawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        ClearSpawnedTokens();
    }

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!isSpawning)
                yield break;

            ClearSpawnedTokens();
            SpawnBatch();
        }

        spawnRoutine = null;
    }

    private void SpawnBatch()
    {
        List<BuffNerfToken> availableTokens =
            GetUniqueTokenPrefabs();

        if (availableTokens.Count < tokensPerBatch)
        {
            Debug.LogWarning(
                $"BuffNerfSpawner: Need {tokensPerBatch} unique " +
                $"token types, but only found {availableTokens.Count}.",
                this
            );

            return;
        }

        Shuffle(availableTokens);

        int spawnedCount = 0;

        for (int i = 0; i < tokensPerBatch; i++)
        {
            BuffNerfToken tokenPrefab = availableTokens[i];

            if (!TryGetSpawnPosition(out Vector3 position))
            {
                Debug.LogWarning(
                    $"BuffNerfSpawner: Could not find a valid " +
                    $"position for {tokenPrefab.name}.",
                    this
                );

                continue;
            }

            BuffNerfToken token = Instantiate(
                tokenPrefab,
                position,
                tokenPrefab.transform.rotation,
                transform
            );

            spawnedTokens.Add(token.gameObject);
            spawnedCount++;
        }

        Debug.Log(
            $"BuffNerfSpawner: Spawned {spawnedCount}/" +
            $"{tokensPerBatch} tokens."
        );
    }

    private List<BuffNerfToken> GetUniqueTokenPrefabs()
    {
        List<BuffNerfToken> uniqueTokens = new();
        HashSet<StatType> usedTypes = new();

        foreach (BuffNerfToken prefab in tokenPrefabs)
        {
            if (prefab == null)
                continue;

            if (usedTypes.Add(prefab.StatType))
            {
                uniqueTokens.Add(prefab);
            }
            else
            {
                Debug.LogWarning(
                    $"BuffNerfSpawner: Duplicate token type " +
                    $"found on prefab {prefab.name}.",
                    prefab
                );
            }
        }

        return uniqueTokens;
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;

        if (spawnArea == null)
            return false;

        Bounds bounds = spawnArea.bounds;

        for (int attempt = 0;
             attempt < maxAttemptsPerToken;
             attempt++)
        {
            float x = Random.Range(
                bounds.min.x,
                bounds.max.x
            );

            float z = Random.Range(
                bounds.min.z,
                bounds.max.z
            );

            Vector3 rayOrigin = new Vector3(
                x,
                bounds.max.y + raycastHeight,
                z
            );

            if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    raycastDistance,
                    groundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            Vector3 candidatePosition =
                hit.point + Vector3.up * groundOffset;

            if (IsTooCloseToOtherTokens(candidatePosition))
                continue;

            if (IsPositionBlocked(candidatePosition))
                continue;

            position = candidatePosition;
            return true;
        }

        return false;
    }

    private bool IsTooCloseToOtherTokens(Vector3 position)
    {
        foreach (GameObject token in spawnedTokens)
        {
            if (token == null)
                continue;

            float distance = Vector3.Distance(
                position,
                token.transform.position
            );

            if (distance < minSpawnDistance)
                return true;
        }

        return false;
    }

    private bool IsPositionBlocked(Vector3 position)
    {
        if (obstacleLayers.value == 0)
            return false;

        Vector3 checkPosition =
            position + Vector3.up * clearanceRadius;

        return Physics.CheckSphere(
            checkPosition,
            clearanceRadius,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private void ClearSpawnedTokens()
    {
        foreach (GameObject token in spawnedTokens)
        {
            if (token != null)
                Destroy(token);
        }

        spawnedTokens.Clear();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (list[i], list[randomIndex]) =
                (list[randomIndex], list[i]);
        }
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnArea == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            spawnArea.bounds.center,
            spawnArea.bounds.size
        );
    }
}