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

    [Header("Rotation Settings")]
    [Tooltip("Height above object to check for wave (meters)")]
    public float raycastHeightOffset = 5f;

    [Tooltip("Duration of rotation transition when leaving wave (seconds)")]
    public float rotationTransitionDuration = 1f;

    [Tooltip("Gravity during death fall (m/s²)")]
    public float deathGravity = 9.8f;

    [Tooltip("Duration of death fall before destroy (seconds)")]
    public float deathFallDuration = 2f;

    [Tooltip("Z offset when entering death state (moves behind wave)")]
    public float deathZOffset = 10f;

    [Tooltip("Minimum rotation speed around local Y axis (degrees/second)")]
    public float minRotationSpeed = 90f;

    [Tooltip("Maximum rotation speed around local Y axis (degrees/second)")]
    public float maxRotationSpeed = 180f;

    [Header("Collision Settings")]
    [Tooltip("Collision radius for distance-based collision detection (meters)")]
    public float collisionRadius = 2f;

    [Tooltip("Energy value when hit (positive = restore, negative = damage)")]
    public float energyValue = -10f;

    [Tooltip("Child object to deactivate on collision (contains mesh/visuals)")]
    public GameObject childObject;

    [Tooltip("Particle system prefab to spawn on collision")]
    public GameObject disappearEffectPrefab;

    [Tooltip("Duration before destroying object after collision (seconds)")]
    public float destroyDelay = 2f;

    [Header("Debug")]
    [Tooltip("Show raycast debug line")]
    public bool showDebugRay = true;

    [Tooltip("Show rotation debug info")]
    public bool showRotationDebug = false;

    [Tooltip("Show collision radius in editor")]
    public bool showCollisionRadius = true;

    private float verticalVelocity = 0f;
    private float lastCheckTime = 0f;
    private float spawnTime;

    // Rotation
    private float rotationSpeed = 0f;
    private float accumulatedYRotation = 0f;

    // Leaving state
    private bool isLeaving = false;
    private float leavingStartTime = 0f;
    private float initialXRotation = -90f;

    // Death state
    private bool isDead = false;
    private float deathStartTime = 0f;

    // Collision state
    private bool hasBeenHit = false;
    private float disappearStartTime = 0f;

    void Start()
    {
        // Set initial rotation: X = -90 degrees
        transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        // Initialize random rotation speed around local Y axis
        rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);

        if (showRotationDebug)
        {
            Debug.Log($"FloatingObject spawned with rotation speed: {rotationSpeed:F1}°/s");
        }

        // Initialize vertical velocity to 0
        verticalVelocity = 0f;
        spawnTime = Time.time;
        lastCheckTime = Time.time;
    }

    void FixedUpdate()
    {
        // Update vertical velocity based on current state
        if (isDead)
        {
            // Death state: apply gravity
            verticalVelocity -= deathGravity * Time.fixedDeltaTime;
        }
        else
        {
            // Normal and Leaving states: continue accelerating upward
            verticalVelocity += upwardAcceleration * Time.fixedDeltaTime;
        }

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

        // Accumulate Y rotation
        accumulatedYRotation += rotationSpeed * Time.fixedDeltaTime;

        // Calculate X rotation based on state
        float targetXRotation = initialXRotation;
        if (isLeaving || isDead)
        {
            float elapsedTime = Time.time - leavingStartTime;
            float t = Mathf.Clamp01(elapsedTime / rotationTransitionDuration);
            targetXRotation = Mathf.Lerp(initialXRotation, 0f, t);
        }

        // Apply rotation in correct order for local Y rotation:
        // 1. First rotate around Y (self-rotation)
        // 2. Then rotate around X (lying down/standing up)
        Quaternion yRotation = Quaternion.Euler(0f, accumulatedYRotation, 0f);
        Quaternion xRotation = Quaternion.Euler(targetXRotation, 0f, 0f);
        transform.rotation = xRotation * yRotation;

        // Handle death state timing
        if (isDead)
        {
            float deathElapsed = Time.time - deathStartTime;
            if (deathElapsed >= deathFallDuration)
            {
                //Debug.Log($"FloatingObject destroyed after death fall at position {transform.position}");
                Destroy(gameObject);
            }
        }

        // Check if object should be destroyed (not at every frame for performance)
        if (!isDead && Time.time >= lastCheckTime + checkInterval)
        {
            CheckWaveCollision();
            lastCheckTime = Time.time;
        }
    }

    /// <summary>
    /// Checks if the object is still above the wave using a raycast
    /// Triggers leaving transition if no wave is detected
    /// In leaving state, checks from current position to detect boundary exit
    /// </summary>
    void CheckWaveCollision()
    {
        // Don't check until after the delay period
        if (Time.time < spawnTime + detectionStartDelay)
        {
            return;
        }

        // Raycast origin depends on current state
        Vector3 rayOrigin;
        if (isLeaving)
        {
            // In leaving state: check from current position to detect boundary
            rayOrigin = transform.position;
        }
        else
        {
            // Normal state: check from 5m above for early detection
            rayOrigin = transform.position + Vector3.up * raycastHeightOffset;
        }

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

        // State transitions based on wave detection
        if (!hitWave)
        {
            if (isLeaving)
            {
                // In leaving state and no wave detected → enter death state
                EnterDeathState();
            }
            else
            {
                // Normal state and no wave detected → enter leaving state
                //Debug.Log($"FloatingObject entering leaving state at position {transform.position}");
                isLeaving = true;
                leavingStartTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Enters the death state: moves object behind wave and starts falling
    /// </summary>
    void EnterDeathState()
    {
        if (isDead) return; // Already dead

        //Debug.Log($"FloatingObject entering death state at position {transform.position}");

        isDead = true;
        deathStartTime = Time.time;

        // Move object behind wave (Z + 10)
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, currentPos.y, currentPos.z + deathZOffset);

        //Debug.Log($"FloatingObject moved to death position {transform.position} (Z+{deathZOffset})");
    }

    /// <summary>
    /// Called by player when collision is detected
    /// Returns true if collision was processed, false if object can't be hit
    /// </summary>
    public bool OnPlayerCollision()
    {
        // Can only be hit in normal state (not leaving, not dead, not already hit)
        if (hasBeenHit || isLeaving || isDead)
        {
            return false;
        }

        // Mark as hit
        hasBeenHit = true;
        disappearStartTime = Time.time;

        // Apply energy modification
        if (energyValue != 0f)
        {
            if (EnergyManager.Instance != null)
            {
                if (energyValue > 0)
                {
                    EnergyManager.Instance.AddEnergy(energyValue);
                }
                else
                {
                    EnergyManager.Instance.RemoveEnergy(-energyValue);
                }
            }
            else
            {
                Debug.LogWarning("FloatingObject: EnergyManager not found!");
            }
        }

        // Deactivate child object (mesh/visuals)
        if (childObject != null)
        {
            childObject.SetActive(false);
        }

        // Spawn disappear particle effect
        if (disappearEffectPrefab != null)
        {
            GameObject effect = Instantiate(disappearEffectPrefab, transform.position, transform.rotation);

            // Get particle system and play it
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();

                // Auto-destroy the effect after it finishes
                float effectDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(effect, effectDuration);
            }
            else
            {
                // Fallback: destroy after 2 seconds if no ParticleSystem found
                Destroy(effect, 2f);
            }
        }

        // Schedule destruction of this object
        Destroy(gameObject, destroyDelay);

        return true;
    }

    /// <summary>
    /// Checks if this object can collide with player (only in normal state)
    /// </summary>
    public bool CanCollide()
    {
        return !hasBeenHit && !isLeaving && !isDead;
    }

    /// <summary>
    /// Gets the collision radius for distance checking
    /// </summary>
    public float GetCollisionRadius()
    {
        return collisionRadius;
    }

    /// <summary>
    /// Optional: Visualize the object in the editor
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugRay)
            return;

        // Draw object position with color based on state
        if (isDead)
            Gizmos.color = Color.black;
        else if (isLeaving)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // Draw raycast origin
        Vector3 rayOrigin;
        if (isLeaving)
        {
            rayOrigin = transform.position;
        }
        else
        {
            rayOrigin = transform.position + Vector3.up * raycastHeightOffset;
        }

        Gizmos.color = isDead ? Color.black : (isLeaving ? Color.red : Color.yellow);
        Gizmos.DrawWireSphere(rayOrigin, 0.3f);

        // Draw raycast direction
        Gizmos.color = isDead ? Color.gray : (isLeaving ? Color.red : Color.cyan);
        Gizmos.DrawRay(rayOrigin, Vector3.forward * 2f);

        // Draw line connecting object to raycast origin (if different)
        if (!isLeaving)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(transform.position, rayOrigin);
        }

        // Draw rotation indicator (forward direction arrow)
        if (showRotationDebug)
        {
            Gizmos.color = Color.magenta;
            Vector3 forward = transform.forward * 1.5f;
            Gizmos.DrawRay(transform.position, forward);

            // Draw arrow head
            Vector3 arrowTip = transform.position + forward;
            Vector3 right = transform.right * 0.3f;
            Gizmos.DrawLine(arrowTip, arrowTip - forward.normalized * 0.3f + right);
            Gizmos.DrawLine(arrowTip, arrowTip - forward.normalized * 0.3f - right);
        }

        // Draw collision radius (XY plane circle)
        if (showCollisionRadius)
        {
            Color radiusColor;
            if (hasBeenHit)
                radiusColor = Color.gray;
            else if (CanCollide())
                radiusColor = new Color(0f, 1f, 0f, 0.3f); // Green for active
            else
                radiusColor = new Color(1f, 1f, 0f, 0.3f); // Yellow for inactive

            Gizmos.color = radiusColor;

            // Draw circle in XY plane (perpendicular to Z axis)
            int segments = 32;
            Vector3 prevPoint = transform.position + new Vector3(collisionRadius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 newPoint = transform.position + new Vector3(
                    Mathf.Cos(angle) * collisionRadius,
                    Mathf.Sin(angle) * collisionRadius,
                    0f
                );
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}
