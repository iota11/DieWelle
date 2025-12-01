using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates continuous objects along -X axis using object pooling
/// Wave moves faster (10m/s) than objects (3m/s), so new objects are spawned ahead
/// Objects are recycled when they fall behind the wave
/// </summary>
public class WaveObjectGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("Prefab to spawn")]
    public GameObject objectPrefab;

    [Tooltip("Reference to the wave object (required)")]
    public Transform waveTransform;

    [Tooltip("Wave length (transform is at center, so head = center + length/2)")]
    public float waveLength = 265f;

    [Tooltip("Spacing between each object (meters)")]
    public float objectSpacing = 3f;

    [Tooltip("Y offset from the wave position")]
    public float yOffset = 0f;

    [Tooltip("Z offset from the wave position")]
    public float zOffset = 0f;

    [Header("Movement Settings")]
    [Tooltip("Wave velocity in -X direction (m/s)")]
    public float waveVelocity = 10f;

    [Tooltip("Object velocity in -X direction (m/s)")]
    public float objectVelocity = 3f;

    [Header("Spawning Settings")]
    [Tooltip("Distance ahead of wave head to maintain object coverage (meters)")]
    public float spawnAheadOfHead = 30f;

    [Tooltip("Distance behind wave tail before recycling object (meters)")]
    public float recycleBehindTail = 10f;

    [Tooltip("Initial pool size")]
    public int initialPoolSize = 100;

    [Header("Debug")]
    [Tooltip("Show debug lines in editor")]
    public bool showDebugLines = true;

    [Tooltip("Show generation logs")]
    public bool showDebugLogs = false;

    // Object pool
    private Queue<GameObject> objectPool = new Queue<GameObject>();
    private List<GameObject> activeObjects = new List<GameObject>();

    void Start()
    {
        Debug.Log("[WaveObjectGenerator] Start() called");
        InitializePool();
        SpawnInitialObjects();
        Debug.Log($"[WaveObjectGenerator] Start() completed. Active objects: {activeObjects.Count}");
    }

    /// <summary>
    /// Initialize the object pool
    /// </summary>
    void InitializePool()
    {
        if (objectPrefab == null)
        {
            Debug.LogError("WaveObjectGenerator: No object prefab assigned!");
            return;
        }

        if (waveTransform == null)
        {
            Debug.LogError("WaveObjectGenerator: No wave transform assigned!");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"WaveObjectGenerator: Initializing pool with {initialPoolSize} objects");
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obj = CreateNewObject();
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }
    }

    /// <summary>
    /// Create a new object with proper setup
    /// </summary>
    GameObject CreateNewObject()
    {
        Quaternion rotation = Quaternion.Euler(0f, -90f, 0f);
        GameObject obj = Instantiate(objectPrefab, Vector3.zero, rotation);

        // Add movement component if it doesn't exist
        BaseObjectMover mover = obj.GetComponent<BaseObjectMover>();
        if (mover == null)
        {
            mover = obj.AddComponent<BaseObjectMover>();
        }
        mover.velocity = new Vector3(-objectVelocity, 0f, 0f);
        mover.generator = this;

        return obj;
    }

    /// <summary>
    /// Spawn initial objects to cover the area ahead of wave
    /// </summary>
    void SpawnInitialObjects()
    {
        float waveHeadX = GetWaveHeadX();  // Left side (smaller X)
        float waveTailX = GetWaveTailX();  // Right side (larger X)

        float startX = waveHeadX - spawnAheadOfHead;  // 30m ahead of wave head (further left, smaller)
        float endX = waveTailX + recycleBehindTail;    // 10m behind wave tail (further right, larger)

        int objectsToSpawn = Mathf.CeilToInt((endX - startX) / objectSpacing);  // Distance from left to right

        Debug.Log($"[WaveObjectGenerator] SpawnInitialObjects: Wave center={waveTransform.position.x:F2}, head={waveHeadX:F2}, tail={waveTailX:F2}");
        Debug.Log($"[WaveObjectGenerator] SpawnInitialObjects: Spawning {objectsToSpawn} initial objects from X={startX:F2} (left) to X={endX:F2} (right)");

        for (int i = 0; i < objectsToSpawn; i++)
        {
            float xPos = startX + (i * objectSpacing);  // Start from left, go right
            SpawnObjectAt(xPos);
        }

        Debug.Log($"[WaveObjectGenerator] SpawnInitialObjects completed. Active objects: {activeObjects.Count}");
    }

    /// <summary>
    /// Spawn or reuse an object at the specified X position
    /// </summary>
    void SpawnObjectAt(float xPosition)
    {
        GameObject obj = GetObjectFromPool();

        if (obj == null)
        {
            Debug.LogError("[WaveObjectGenerator] SpawnObjectAt: Got null object from pool!");
            return;
        }

        // Set position
        Vector3 spawnPosition = new Vector3(xPosition, waveTransform.position.y + yOffset, waveTransform.position.z + zOffset);
        obj.transform.position = spawnPosition;

        // Ensure the mover component has the correct velocity
        BaseObjectMover mover = obj.GetComponent<BaseObjectMover>();
        if (mover != null)
        {
            mover.velocity = new Vector3(-objectVelocity, 0f, 0f);
            mover.generator = this;
        }
        else
        {
            Debug.LogError($"[WaveObjectGenerator] SpawnObjectAt: Object at {xPosition:F2} is missing BaseObjectMover component!");
        }

        // Activate the object
        obj.SetActive(true);
        activeObjects.Add(obj);

        if (activeObjects.Count <= 5 || activeObjects.Count % 20 == 0)
        {
            Debug.Log($"[WaveObjectGenerator] SpawnObjectAt: Spawned object #{activeObjects.Count} at X={xPosition:F2}, velocity={-objectVelocity}m/s");
        }
    }

    /// <summary>
    /// Get an object from pool or create new one if pool is empty
    /// </summary>
    GameObject GetObjectFromPool()
    {
        if (objectPool.Count > 0)
        {
            return objectPool.Dequeue();
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log("Pool empty, creating new object");
            }
            return CreateNewObject();
        }
    }

    /// <summary>
    /// Return an object to the pool
    /// </summary>
    void ReturnObjectToPool(GameObject obj)
    {
        // Deactivate the object
        obj.SetActive(false);

        // Return to pool
        objectPool.Enqueue(obj);
        activeObjects.Remove(obj);

        if (showDebugLogs)
        {
            Debug.Log($"Recycled object from X={obj.transform.position.x:F2}. Pool size: {objectPool.Count}, Active: {activeObjects.Count}");
        }
    }

    /// <summary>
    /// Calculate wave head position (front, moving direction = -X direction)
    /// Since wave moves in -X direction (left), head is on the left side
    /// </summary>
    float GetWaveHeadX()
    {
        return waveTransform.position.x - (waveLength / 2f);
    }

    /// <summary>
    /// Calculate wave tail position (back, opposite of moving direction = +X direction)
    /// Since wave moves in -X direction (left), tail is on the right side
    /// </summary>
    float GetWaveTailX()
    {
        return waveTransform.position.x + (waveLength / 2f);
    }

    void Update()
    {
        if (waveTransform == null)
        {
            Debug.LogError("WaveObjectGenerator: waveTransform is null!");
            return;
        }

        // Calculate wave boundaries
        float waveHeadX = GetWaveHeadX();
        float waveTailX = GetWaveTailX();

        // Calculate spawn and recycle thresholds
        // Head is on the left (-X), so spawn ahead means further left (subtract)
        // Tail is on the right (+X), so recycle behind means further right (add)
        float spawnThreshold = waveHeadX - spawnAheadOfHead;  // 30m ahead of wave head (further left)
        float recycleThreshold = waveTailX + recycleBehindTail;  // 10m behind wave tail (further right)

        // ALWAYS log every 60 frames for debugging
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[WaveObjectGenerator] Wave - Center: {waveTransform.position.x:F2}, Head: {waveHeadX:F2}, Tail: {waveTailX:F2}");
            Debug.Log($"[WaveObjectGenerator] SpawnThreshold: {spawnThreshold:F2}, RecycleThreshold: {recycleThreshold:F2}");
            Debug.Log($"[WaveObjectGenerator] Active objects: {activeObjects.Count}, Pool size: {objectPool.Count}");
        }

        // Find the leftmost active object (since wave moves in -X direction)
        float leftmostObjectX = float.MaxValue;
        GameObject leftmostObject = null;
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null && obj.transform.position.x < leftmostObjectX)
            {
                leftmostObjectX = obj.transform.position.x;
                leftmostObject = obj;
            }
        }

        if (Time.frameCount % 60 == 0)
        {
            if (leftmostObject != null)
            {
                BaseObjectMover mover = leftmostObject.GetComponent<BaseObjectMover>();
                Debug.Log($"[WaveObjectGenerator] Leftmost object at X={leftmostObjectX:F2}, velocity={mover?.velocity}");
            }
            else
            {
                Debug.Log($"[WaveObjectGenerator] No leftmost object found!");
            }
        }

        // Spawn new objects to maintain coverage
        // Since wave moves faster (10m/s) than objects (3m/s), spawnThreshold moves faster
        // We need to continuously spawn objects at the spawnThreshold position (ahead of wave head)

        // Check if we need to spawn based on the gap between leftmost object and spawn threshold
        // If leftmost > spawnThreshold + objectSpacing, we need to spawn
        // We maintain at least objectSpacing distance between spawn and leftmost
        bool shouldSpawn = activeObjects.Count == 0 || leftmostObjectX > spawnThreshold + objectSpacing;

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[WaveObjectGenerator] Should spawn? {shouldSpawn} (leftmost={leftmostObjectX:F2}, threshold={spawnThreshold:F2})");
        }

        if (shouldSpawn)
        {
            float newSpawnX;
            if (activeObjects.Count == 0)
            {
                // First spawn at the spawn threshold
                newSpawnX = spawnThreshold;
            }
            else
            {
                // Spawn exactly objectSpacing to the left of the leftmost object
                newSpawnX = leftmostObjectX - objectSpacing;

                // Don't spawn beyond the threshold (further left)
                if (newSpawnX < spawnThreshold)
                    newSpawnX = spawnThreshold;
            }

            SpawnObjectAt(newSpawnX);

            // ALWAYS log spawning
            float gapDistance = leftmostObjectX - spawnThreshold;
            Debug.Log($"[WaveObjectGenerator] SPAWNED object at X={newSpawnX:F2}, leftmost was {leftmostObjectX:F2}, threshold={spawnThreshold:F2}, gap={gapDistance:F2}");
        }

        // Check for objects to recycle (behind wave tail, which is on the right)
        // If object X > recycleThreshold, it's too far behind (right side)
        int recycledCount = 0;
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i] != null && activeObjects[i].transform.position.x > recycleThreshold)
            {
                float objX = activeObjects[i].transform.position.x;
                ReturnObjectToPool(activeObjects[i]);
                recycledCount++;

                // ALWAYS log recycling
                Debug.Log($"[WaveObjectGenerator] RECYCLED object at X={objX:F2}, recycleThreshold={recycleThreshold:F2}");
            }
        }

        if (Time.frameCount % 60 == 0)
        {
            if (recycledCount == 0)
            {
                Debug.Log($"[WaveObjectGenerator] No objects recycled this check");
            }
            else
            {
                Debug.Log($"[WaveObjectGenerator] Recycled {recycledCount} objects this check");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugLines || waveTransform == null)
            return;

        Vector3 wavePos = waveTransform.position;
        float waveHeadX = GetWaveHeadX();
        float waveTailX = GetWaveTailX();
        float spawnThresholdX = waveHeadX + spawnAheadOfHead;
        float recycleThresholdX = waveTailX - recycleBehindTail;

        float yPos = wavePos.y + yOffset;
        float zPos = wavePos.z + zOffset;

        // Draw wave center (cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(wavePos.x, yPos, zPos), 1f);

        // Draw wave head (blue)
        Gizmos.color = Color.blue;
        Vector3 headStart = new Vector3(waveHeadX, yPos - 3f, zPos);
        Vector3 headEnd = new Vector3(waveHeadX, yPos + 3f, zPos);
        Gizmos.DrawLine(headStart, headEnd);
        Gizmos.DrawWireSphere(new Vector3(waveHeadX, yPos, zPos), 0.8f);

        // Draw wave tail (magenta)
        Gizmos.color = Color.magenta;
        Vector3 tailStart = new Vector3(waveTailX, yPos - 3f, zPos);
        Vector3 tailEnd = new Vector3(waveTailX, yPos + 3f, zPos);
        Gizmos.DrawLine(tailStart, tailEnd);
        Gizmos.DrawWireSphere(new Vector3(waveTailX, yPos, zPos), 0.8f);

        // Draw wave length line (white)
        Gizmos.color = Color.white;
        Gizmos.DrawLine(new Vector3(waveHeadX, yPos, zPos), new Vector3(waveTailX, yPos, zPos));

        // Draw spawn threshold (green) - 30m ahead of wave head
        Gizmos.color = Color.green;
        Vector3 spawnStart = new Vector3(spawnThresholdX, yPos - 5f, zPos);
        Vector3 spawnEnd = new Vector3(spawnThresholdX, yPos + 5f, zPos);
        Gizmos.DrawLine(spawnStart, spawnEnd);
        Gizmos.DrawWireSphere(new Vector3(spawnThresholdX, yPos, zPos), 1.2f);

        // Draw recycle threshold (red) - 10m behind wave tail
        Gizmos.color = Color.red;
        Vector3 recycleStart = new Vector3(recycleThresholdX, yPos - 5f, zPos);
        Vector3 recycleEnd = new Vector3(recycleThresholdX, yPos + 5f, zPos);
        Gizmos.DrawLine(recycleStart, recycleEnd);
        Gizmos.DrawWireSphere(new Vector3(recycleThresholdX, yPos, zPos), 1.2f);

        // Draw coverage area (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(spawnThresholdX, yPos, zPos), new Vector3(recycleThresholdX, yPos, zPos));
    }
}

/// <summary>
/// Simple movement component for generated objects
/// Moves object with constant world velocity
/// </summary>
public class BaseObjectMover : MonoBehaviour
{
    [HideInInspector]
    public Vector3 velocity = Vector3.zero;

    [HideInInspector]
    public WaveObjectGenerator generator;

    void FixedUpdate()
    {
        // Move the object
        transform.position += velocity * Time.fixedDeltaTime;
    }
}
