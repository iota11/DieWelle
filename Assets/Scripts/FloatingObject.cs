using UnityEngine;

/// <summary>
/// Controls a floating object that rises from the water
/// Moves at 3 m/s in X direction, accelerates upward at 5 m/s²
/// Destroys itself if not above wave mesh
/// </summary>
public class FloatingObject : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal world velocity (m/s) - negative for left, positive for right")]
    public float horizontalSpeed = -3f;

    [Tooltip("Upward acceleration (m/s²)")]
    public float upwardAcceleration = 5f;

    [Tooltip("Use relative movement (move with wave parent)")]
    public bool useRelativeMovement = false;

    [Header("Wave Detection")]
    [Tooltip("Layer mask for the wave mesh collider")]
    public LayerMask waveLayerMask;

    [Tooltip("Distance to raycast for wave detection")]
    public float raycastDistance = 50f;

    [Tooltip("How often to check if object is above wave (seconds)")]
    public float checkInterval = 0.2f;

    [Tooltip("Delay before starting wave detection (seconds)")]
    public float detectionStartDelay = 1f;

    [Header("Debug")]
    [Tooltip("Show raycast debug line")]
    public bool showDebugRay = true;

    private float verticalVelocity = 0f;
    private float lastCheckTime = 0f;
    private float spawnTime;

    void Start()
    {
        // Initialize vertical velocity to 0
        verticalVelocity = 0f;
        spawnTime = Time.time;
        lastCheckTime = Time.time;
    }

    void FixedUpdate()
    {
        // Update vertical velocity with acceleration
        verticalVelocity += upwardAcceleration * Time.fixedDeltaTime;

        // Calculate movement delta
        Vector3 movement;

        if (useRelativeMovement)
        {
            // Move relative to parent (wave) - only vertical movement
            movement = new Vector3(
                0f,
                verticalVelocity * Time.fixedDeltaTime,
                0f
            );
        }
        else
        {
            // Move in world space
            movement = new Vector3(
                horizontalSpeed * Time.fixedDeltaTime,
                verticalVelocity * Time.fixedDeltaTime,
                0f
            );
        }

        // Apply movement
        transform.position += movement;

        // Check if object should be destroyed (not at every frame for performance)
        if (Time.time >= lastCheckTime + checkInterval)
        {
            CheckWaveCollision();
            lastCheckTime = Time.time;
        }
    }

    /// <summary>
    /// Checks if the object is still above the wave using a raycast
    /// Destroys the object if no wave is detected below
    /// </summary>
    void CheckWaveCollision()
    {
        // Don't check until after the delay period
        if (Time.time < spawnTime + detectionStartDelay)
        {
            return;
        }

        // Raycast origin is the object's position
        Vector3 rayOrigin = transform.position;

        // Raycast direction is forward along Z-axis
        Vector3 rayDirection = Vector3.forward;

        // Perform the raycast
        bool hitWave = Physics.Raycast(
            rayOrigin,
            rayDirection,
            out RaycastHit hit,
            raycastDistance,
            waveLayerMask
        );

        // Debug visualization
        if (showDebugRay)
        {
            if (hitWave)
            {
                Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(rayOrigin, rayDirection * raycastDistance, Color.red);
            }
        }

        // If no wave detected, destroy this object
        if (!hitWave)
        {
            Debug.Log($"FloatingObject destroyed - no wave detected at position {transform.position}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Optional: Visualize the object in the editor
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugRay)
            return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // Draw raycast direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, Vector3.forward * 2f);
    }
}
