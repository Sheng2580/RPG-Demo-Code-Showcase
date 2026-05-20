using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemyCreateManager : MonoBehaviour
{
    [Serializable]
    public class EnemySpawnInfo
    {
        public string enemyName = "OrdinaryEnemy";
        public int count = 1;
    }

    [Serializable]
    public class EnemyWave
    {
        public List<EnemySpawnInfo> enemies = new List<EnemySpawnInfo>();
        public float spawnInterval = 0.2f;
    }

    private const string EnemyAbName = "enemy";

    [Header("Wave")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private List<EnemyWave> waves = new List<EnemyWave>();
    [SerializeField] private UnityEvent onAllWavesCompleted;

    [Header("Spawn Area")]
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float navMeshSampleRadius = 3f;
    [SerializeField] private int maxSampleCount = 40;

    [Header("Distance Limit")]
    [SerializeField] private float minDistanceFromPlayer = 6f;
    [SerializeField] private float minDistanceBetweenEnemies = 2f;
    [SerializeField] private float playerForwardBlockDistance = 8f;
    [SerializeField, Range(0f, 180f)] private float playerForwardBlockAngle = 70f;

    public event Action AllWavesCompleted;

    private readonly List<EnemyBase> aliveEnemies = new List<EnemyBase>();
    private readonly List<Vector3> currentWaveSpawnPositions = new List<Vector3>();
    private Coroutine spawnCoroutine;
    private Transform player;
    private bool isSpawning;
    private bool hasCompleted;
    private NavMeshTriangulation navMeshTriangulation;

    private void Start()
    {
        if (autoStart)
        {
            StartSpawn();
        }
    }

    public void StartSpawn()
    {
        if (isSpawning)
        {
            return;
        }

        StopSpawn();
        hasCompleted = false;
        spawnCoroutine = StartCoroutine(SpawnWaves());
    }

    public void StartSpawn(List<EnemyWave> waveConfigs)
    {
        waves = waveConfigs != null ? waveConfigs : new List<EnemyWave>();
        StartSpawn();
    }

    public void StartSpawn(int waveCount, string enemyName, int enemyCountPerWave)
    {
        waves = BuildSameEnemyWaves(waveCount, enemyName, enemyCountPerWave);
        StartSpawn();
    }

    public void StopSpawn()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        isSpawning = false;
        currentWaveSpawnPositions.Clear();
        ClearAliveEnemyListeners();
    }

    private IEnumerator SpawnWaves()
    {
        isSpawning = true;
        player = FindPlayer();
        navMeshTriangulation = NavMesh.CalculateTriangulation();

        if (waves == null || waves.Count == 0)
        {
            CompleteAllWaves();
            yield break;
        }

        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
        {
            EnemyWave wave = waves[waveIndex];
            if (wave == null || wave.enemies == null || wave.enemies.Count == 0)
            {
                continue;
            }

            currentWaveSpawnPositions.Clear();

            for (int enemyInfoIndex = 0; enemyInfoIndex < wave.enemies.Count; enemyInfoIndex++)
            {
                EnemySpawnInfo enemyInfo = wave.enemies[enemyInfoIndex];
                if (enemyInfo == null || enemyInfo.count <= 0 || string.IsNullOrEmpty(enemyInfo.enemyName))
                {
                    continue;
                }

                for (int i = 0; i < enemyInfo.count; i++)
                {
                    yield return SpawnEnemy(enemyInfo.enemyName);

                    if (wave.spawnInterval > 0f)
                    {
                        yield return new WaitForSeconds(wave.spawnInterval);
                    }
                }
            }

            while (aliveEnemies.Count > 0)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        CompleteAllWaves();
    }

    private List<EnemyWave> BuildSameEnemyWaves(int waveCount, string enemyName, int enemyCountPerWave)
    {
        List<EnemyWave> newWaves = new List<EnemyWave>();
        int validWaveCount = Mathf.Max(0, waveCount);

        for (int i = 0; i < validWaveCount; i++)
        {
            EnemyWave wave = new EnemyWave();
            wave.enemies.Add(new EnemySpawnInfo
            {
                enemyName = enemyName,
                count = enemyCountPerWave
            });
            newWaves.Add(wave);
        }

        return newWaves;
    }

    private IEnumerator SpawnEnemy(string enemyName)
    {
        GameObject enemyObj = null;
        bool loaded = false;

        PoolManager.Instance.GetObjForAB(EnemyAbName, enemyName, obj =>
        {
            enemyObj = obj;
            loaded = true;
        });

        if (!loaded)
        {
            yield return new WaitUntil(() => loaded);
        }

        if (enemyObj == null)
        {
            Debug.LogError($"[EnemyCreateManager] Create enemy failed: {enemyName}");
            yield break;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        PlaceEnemy(enemyObj, spawnPosition);

        EnemyBase enemy = enemyObj.GetComponentInChildren<EnemyBase>();
        if (enemy == null)
        {
            Debug.LogError($"[EnemyCreateManager] {enemyName} missing EnemyBase.");
            PoolManager.Instance.pushObj(enemyName, enemyObj);
            yield break;
        }

        enemy.ResetForSpawn(player);

        aliveEnemies.Add(enemy);
        enemy.OnDead += OnEnemyDead;
        currentWaveSpawnPositions.Add(spawnPosition);
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 bestPoint = GetSpawnCenter();
        float bestScore = float.MinValue;
        bool hasBestPoint = false;

        for (int i = 0; i < maxSampleCount; i++)
        {
            Vector3 candidate = GetRandomCandidate();
            if (!TryGetValidNavMeshPoint(candidate, out Vector3 navPoint))
            {
                continue;
            }

            float score = GetSpawnScore(navPoint);
            if (!hasBestPoint || score > bestScore)
            {
                hasBestPoint = true;
                bestScore = score;
                bestPoint = navPoint;
            }

            if (IsFarEnough(navPoint))
            {
                return navPoint;
            }
        }

        if (hasBestPoint)
        {
            Debug.LogWarning("[EnemyCreateManager] Use best available spawn point. Check spawn area or increase spawn radius.");
            return bestPoint;
        }

        Debug.LogWarning("[EnemyCreateManager] No NavMesh point found, use manager position.");
        return GetSpawnCenter();
    }

    private Vector3 GetRandomCandidate()
    {
        if (TryGetRandomNavMeshPointInSpawnArea(out Vector3 navMeshPoint))
        {
            return navMeshPoint;
        }

        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            if (point != null)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * Mathf.Max(spawnRadius, minDistanceBetweenEnemies);
                return point.position + new Vector3(offset.x, 0f, offset.y);
            }
        }

        Vector2 random = UnityEngine.Random.insideUnitCircle * Mathf.Max(0f, spawnRadius);
        Vector3 center = GetSpawnCenter();
        return center + new Vector3(random.x, 0f, random.y);
    }

    private bool TryGetRandomNavMeshPointInSpawnArea(out Vector3 point)
    {
        point = Vector3.zero;

        if (navMeshTriangulation.vertices == null ||
            navMeshTriangulation.indices == null ||
            navMeshTriangulation.indices.Length < 3)
        {
            return false;
        }

        int triangleCount = navMeshTriangulation.indices.Length / 3;
        int attempts = Mathf.Max(20, maxSampleCount * 4);

        for (int i = 0; i < attempts; i++)
        {
            int triangleIndex = UnityEngine.Random.Range(0, triangleCount) * 3;
            Vector3 a = navMeshTriangulation.vertices[navMeshTriangulation.indices[triangleIndex]];
            Vector3 b = navMeshTriangulation.vertices[navMeshTriangulation.indices[triangleIndex + 1]];
            Vector3 c = navMeshTriangulation.vertices[navMeshTriangulation.indices[triangleIndex + 2]];

            float r1 = Mathf.Sqrt(UnityEngine.Random.value);
            float r2 = UnityEngine.Random.value;
            Vector3 candidate = (1f - r1) * a + r1 * (1f - r2) * b + r1 * r2 * c;

            if (!IsInSpawnArea(candidate))
            {
                continue;
            }

            point = candidate;
            return true;
        }

        return false;
    }

    private bool IsInSpawnArea(Vector3 position)
    {
        float radiusSqr = spawnRadius * spawnRadius;

        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Transform point = spawnPoints[i];
                if (point == null)
                {
                    continue;
                }

                Vector3 delta = position - point.position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= radiusSqr)
                {
                    return true;
                }
            }

            return false;
        }

        Vector3 centerDelta = position - GetSpawnCenter();
        centerDelta.y = 0f;
        return centerDelta.sqrMagnitude <= radiusSqr;
    }

    private float GetSpawnScore(Vector3 position)
    {
        float score = 0f;

        if (player != null)
        {
            Vector3 playerDelta = position - player.position;
            playerDelta.y = 0f;
            score += playerDelta.sqrMagnitude;
        }

        for (int i = 0; i < currentWaveSpawnPositions.Count; i++)
        {
            Vector3 enemyDelta = position - currentWaveSpawnPositions[i];
            enemyDelta.y = 0f;
            score += enemyDelta.sqrMagnitude * 2f;
        }

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            EnemyBase enemy = aliveEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            Vector3 enemyDelta = position - enemy.transform.position;
            enemyDelta.y = 0f;
            score += enemyDelta.sqrMagnitude * 2f;
        }

        return score;
    }

    private Vector3 GetSpawnCenter()
    {
        return spawnCenter != null ? spawnCenter.position : transform.position;
    }

    private bool TryGetValidNavMeshPoint(Vector3 point, out Vector3 navPoint)
    {
        navPoint = point;
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            return false;
        }

        navPoint = hit.position;
        return true;
    }

    private bool IsFarEnough(Vector3 position)
    {
        if (player != null)
        {
            Vector3 toSpawn = position - player.position;
            toSpawn.y = 0f;

            if (toSpawn.sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
            {
                return false;
            }

            if (toSpawn.sqrMagnitude < playerForwardBlockDistance * playerForwardBlockDistance)
            {
                float angle = Vector3.Angle(player.forward, toSpawn);
                if (angle <= playerForwardBlockAngle * 0.5f)
                {
                    return false;
                }
            }
        }

        float enemyMinDistanceSqr = minDistanceBetweenEnemies * minDistanceBetweenEnemies;
        for (int i = 0; i < currentWaveSpawnPositions.Count; i++)
        {
            Vector3 enemyDelta = position - currentWaveSpawnPositions[i];
            enemyDelta.y = 0f;
            if (enemyDelta.sqrMagnitude < enemyMinDistanceSqr)
            {
                return false;
            }
        }

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            EnemyBase enemy = aliveEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            Vector3 enemyDelta = position - enemy.transform.position;
            enemyDelta.y = 0f;
            if (enemyDelta.sqrMagnitude < enemyMinDistanceSqr)
            {
                return false;
            }
        }

        return true;
    }

    private void PlaceEnemy(GameObject enemyObj, Vector3 position)
    {
        CharacterController characterController = enemyObj.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        enemyObj.transform.SetPositionAndRotation(position, Quaternion.identity);

        NavMeshAgent agent = enemyObj.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.Warp(position);
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private Transform FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        return playerObject != null ? playerObject.transform : null;
    }

    private void OnEnemyDead(EnemyBase enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.OnDead -= OnEnemyDead;
        aliveEnemies.Remove(enemy);
        StartCoroutine(ReturnEnemyToPool(enemy));
    }

    private IEnumerator ReturnEnemyToPool(EnemyBase enemy)
    {
        yield return new WaitForSeconds(2f);

        if (enemy == null)
        {
            yield break;
        }

        PoolManager.Instance.pushObj(enemy.gameObject.name, enemy.gameObject);
    }

    private void CompleteAllWaves()
    {
        if (hasCompleted)
        {
            return;
        }

        hasCompleted = true;
        isSpawning = false;
        spawnCoroutine = null;
        AllWavesCompleted?.Invoke();
        onAllWavesCompleted?.Invoke();
    }

    private void ClearAliveEnemyListeners()
    {
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            if (aliveEnemies[i] != null)
            {
                aliveEnemies[i].OnDead -= OnEnemyDead;
            }
        }

        aliveEnemies.Clear();
    }

    private void OnDestroy()
    {
        ClearAliveEnemyListeners();
    }
}


