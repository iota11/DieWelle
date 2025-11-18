using UnityEngine;

/// <summary>
/// Spawns floating objects along a 170m line at y=-14
/// Can be placed anywhere in the scene, tracks wave position automatically
/// </summary>
public class FloatingObjectSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("Length of the spawner line")]
    public float spawnerLength = 170f;

    [Tooltip("Y position where objects spawn")]
    public float spawnHeight = -14f;

    [Tooltip("Prefab of the floating object to spawn")]
    public GameObject floatingObjectPrefab;

    [Tooltip("Reference to the wave object (optional, uses world position if null)")]
    public Transform waveTransform;

    [Header("Spawn Timing")]
    [Tooltip("Minimum time between spawns (seconds)")]
    public float minSpawnInterval = 0.1f;

    [Tooltip("Maximum time between spawns (seconds)")]
    public float maxSpawnInterval = 0.3f;

    [Tooltip("Number of objects to spawn each time")]
    public int objectsPerSpawn = 3;

    [Header("Debug")]
    [Tooltip("Show spawner line in editor")]
    public bool showDebugLine = true;

    [Tooltip("Show spawn debug logs")]
    public bool showDebugLogs = false;

    private float nextSpawnTime;
    private int totalSpawned = 0;

    void Start()
    {
        // Validation
        if (floatingObjectPrefab == null)
        {
            Debug.LogError("FloatingObjectSpawner: No prefab assigned in Inspector!");
        }

        if (objectsPerSpawn <= 0)
        {
            Debug.LogWarning($"FloatingObjectSpawner: objectsPerSpawn is {objectsPerSpawn}, setting to 1");
            objectsPerSpawn = 1;
        }

        // Schedule first spawn
        ScheduleNextSpawn();

        if (showDebugLogs)
        {
            Debug.Log($"FloatingObjectSpawner started. First spawn at {nextSpawnTime}");
            Debug.Log($"Settings - Prefab: {(floatingObjectPrefab != null ? floatingObjectPrefab.name : "NULL")}, ObjectsPerSpawn: {objectsPerSpawn}, Interval: {minSpawnInterval}-{maxSpawnInterval}s");
        }
    }

    void Update()
    {
        // Ensure spawner keeps working
        if (!enabled)
        {
            Debug.LogWarning("FloatingObjectSpawner is disabled!");
            return;
        }

        // Check if it's time to spawn
        if (Time.time >= nextSpawnTime)
        {
            if (showDebugLogs)
            {
                Debug.Log($"Spawning batch at time {Time.time}. Total spawned so far: {totalSpawned}");
            }

            // Spawn multiple objects
            for (int i = 0; i < objectsPerSpawn; i++)
            {
                bool spawned = SpawnFloatingObject();
                if (!spawned && showDebugLogs)
                {
                    Debug.LogWarning($"Failed to spawn object {i + 1}/{objectsPerSpawn}");
                }
            }
            ScheduleNextSpawn();
        }
    }

    void OnDisable()
    {
        Debug.LogWarning($"FloatingObjectSpawner DISABLED! Total spawned: {totalSpawned}");
        Debug.LogWarning($"Spawner position: {transform.position}");
        Debug.LogWarning($"Parent: {(transform.parent != null ? transform.parent.name : "NULL")}");
        Debug.LogWarning($"Parent active: {(transform.parent != null ? transform.parent.gameObject.activeInHierarchy.ToString() : "N/A")}");
    }

    void OnDestroy()
    {
        Debug.LogWarning($"FloatingObjectSpawner DESTROYED! Total spawned: {totalSpawned}");
    }

    /// <summary>
    /// Spawns a floating object at a random position along the spawner line
    /// </summary>
    /// <returns>True if spawned successfully, false otherwise</returns>
    bool SpawnFloatingObject()
    {
        if (showDebugLogs)
        {
            Debug.Log("SpawnFloatingObject() called");
        }

        if (floatingObjectPrefab == null)
        {
            Debug.LogWarning("FloatingObjectSpawner: No prefab assigned!");
            return false;
        }

        if (showDebugLogs)
        {
            Debug.Log("Prefab check passed");
        }

        // Calculate random X position along the spawner length
        // Spawner is centered, so range is from -length/2 to +length/2
        float randomX = Random.Range(-spawnerLength / 2f, spawnerLength / 2f);

        // Get the base position (either from wave or from this transform)
        Vector3 basePosition = waveTransform != null ? waveTransform.position : transform.position;

        // Spawn position in world space
        Vector3 spawnPosition = basePosition + new Vector3(randomX, spawnHeight, 0f);

        if (showDebugLogs)
        {
            Debug.Log($"About to instantiate at {spawnPosition}");
        }

        // Instantiate the floating object
        GameObject spawnedObject = Instantiate(floatingObjectPrefab, spawnPosition, Quaternion.identity);

        if (showDebugLogs)
        {
            Debug.Log($"Instantiate returned: {(spawnedObject != null ? "SUCCESS" : "NULL")}");
        }

        if (spawnedObject == null)
        {
            Debug.LogError("Failed to instantiate floating object!");
            return false;
        }

        totalSpawned++;

        if (showDebugLogs)
        {
            Debug.Log($"Spawned object #{totalSpawned} at position {spawnPosition}");
        }

        // Set as child of wave if available, otherwise keep independent
        if (waveTransform != null)
        {
            spawnedObject.transform.SetParent(waveTransform);
        }

        return true;
    }

    /// <summary>
    /// Schedules the next spawn time randomly within the interval range
    /// </summary>
    void ScheduleNextSpawn()
    {
        float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
        nextSpawnTime = Time.time + interval;

        if (showDebugLogs)
        {
            Debug.Log($"Next spawn scheduled at {nextSpawnTime} (in {interval}s)");
        }
    }

    /// <summary>
    /// Draws debug visualization in the editor
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugLine)
            return;

        // Get the base position (either from wave or from this transform)
        Vector3 basePosition = waveTransform != null ? waveTransform.position : transform.position;

        Gizmos.color = Color.cyan;

        // Draw the spawner line
        Vector3 lineStart = basePosition + new Vector3(-spawnerLength / 2f, spawnHeight, 0f);
        Vector3 lineEnd = basePosition + new Vector3(spawnerLength / 2f, spawnHeight, 0f);

        Gizmos.DrawLine(lineStart, lineEnd);

        // Draw markers at the ends
        Gizmos.DrawWireSphere(lineStart, 0.5f);
        Gizmos.DrawWireSphere(lineEnd, 0.5f);

        // Draw center marker
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(basePosition + new Vector3(0f, spawnHeight, 0f), 0.3f);
    }
}
