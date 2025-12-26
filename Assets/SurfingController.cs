using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem.XR;

public interface IState
{
    void Enter();
    void Exit();
    void FixedUpdate();
}

// Waiting state implementation
public class WaitingState : IState
{
    private SurfingController surfingController;
    public float localSpeed;
    private Rigidbody rb;
    private Transform trans;
    public WaitingState(SurfingController controller) {
        surfingController = controller;
        rb = controller.rb;
        localSpeed = controller.wave.speed - controller.waveBaseSpeed;
        //set local speed 
        trans = controller.transform;
    }
    public void Enter() { }
    public void Exit() { }
    public void FixedUpdate() {
        CheckState();
        HandleRotation();
        HandleMovement();
    }
    public void HandleRotation() {
       
    }
    private void CheckState() {
        float globalTotalRotation = surfingController.globalTotalRotation;
        if (globalTotalRotation != 0) {
            Debug.Log("Game Start!");
            surfingController.SwitchState(surfingController.surfingState);
        }
    }
    public void HandleMovement()
    {
        rb.useGravity = false;

        rb.linearVelocity = -trans.right * localSpeed - Vector3.right * surfingController.waveBaseSpeed;//constant speed
        surfingController.localspeed = localSpeed;
    }
}

// Surfing state implementation
public class SurfingState : IState
{
    private SurfingController surfingController;
    public float localspeed;
    private Rigidbody rb;
    private Transform trans;

    public SurfingState(SurfingController controller) {
        surfingController = controller;
        rb = controller.rb;
        localspeed = controller.wave.speed;
        trans = controller.transform;
    }
    public void Enter() { }
    public void Exit() { }
    public void FixedUpdate() {
        CheckState();
        HandleRotation();
        HandleMovement();
    }
    public void HandleRotation() {
        float globalTotalRotation = surfingController.globalTotalRotation;
        float cur_rot_degree = surfingController.cur_rot_degree;
        Quaternion startRotation = surfingController.startRotation;
        float rotationSpeed = surfingController.rotationSpeed_surf;
        if (Mathf.Abs(cur_rot_degree - globalTotalRotation) > rotationSpeed * Time.fixedDeltaTime)
        {
            if (cur_rot_degree < globalTotalRotation)
            {
                cur_rot_degree += rotationSpeed * Time.fixedDeltaTime;
            }
            else
            {
                cur_rot_degree -= rotationSpeed * Time.fixedDeltaTime;
            }
        }
        else
        {
            cur_rot_degree = Mathf.Lerp(cur_rot_degree, globalTotalRotation, Time.fixedDeltaTime * 2f);
        }
        surfingController.cur_rot_degree = cur_rot_degree;
        trans.localRotation = Quaternion.Lerp(trans.localRotation, startRotation * Quaternion.Euler(0f, 0f, cur_rot_degree), Time.fixedDeltaTime * 10f);
    }
    public void HandleMovement()
    {
        rb.useGravity = false;
        localspeed = surfingController.localspeed;
        float acceleration = surfingController.acceleration;
        float acc = Mathf.Clamp(trans.right.y, -1, 1) * acceleration;
        if (acc < 0) acc *= 0.5f;
        localspeed += acc * Time.fixedDeltaTime;
        localspeed = Mathf.Clamp(localspeed, 0, surfingController.speedMax - surfingController.waveBaseSpeed);
        rb.linearVelocity = -trans.right * localspeed - Vector3.right * surfingController.waveBaseSpeed;
        surfingController.localspeed  = localspeed;
    }
    private void CheckState() {
        Vector3 rayOrigin = trans.position;
        Vector3 rayDirection =new Vector3(0,0,1f);
        Debug.DrawRay(rayOrigin, rayDirection * 30f, Color.green);

        int combinedLayerMask = surfingController.waterLayerMask | surfingController.dangerLayerMask;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit2, 300f, surfingController.dangerLayerMask)) {
            surfingController.SwitchState(surfingController.deadState);
            return;
        }
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, 300f, combinedLayerMask)) {
  
        } 
        else {
            Debug.Log("jump!");
            surfingController.SwitchState(surfingController.jumpState);
        }
    }
}

// Jump state implementation
public class JumpState : IState
{
    private SurfingController surfingController;
    public float wavespeed;
    private Rigidbody rb;
    private Transform trans;
    private float start_rotation;
    private float end_rotation;
    public JumpState(SurfingController controller) {
        surfingController = controller;
        rb = controller.rb;
        wavespeed = controller.wave.speed;
        trans = controller.transform;
    }
    public void FixedUpdate() {
        CheckState();
        HandleRotation();
        HandleMovement();
    }
    private void CheckState() {
        Vector3 rayOrigin = trans.position;
        Vector3 rayDirection = new Vector3(0, 0, 1f);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit2, 300f, surfingController.dangerLayerMask)) {
            surfingController.SwitchState(surfingController.deadState);
            return;
        }

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, 300f, surfingController.waterLayerMask)) {
            float proj = Vector3.Dot((rb.linearVelocity - surfingController.waveBaseSpeed * Vector3.left).normalized, (-trans.right).normalized);
            Debug.Log("proj is " + proj);

            if (proj < surfingController.tolerrance) {
               surfingController.SwitchState(surfingController.deadState);
            } else {
                end_rotation = surfingController.globalTotalRotation;
                float rotationDegrees = Mathf.Abs(end_rotation - start_rotation);
                float rotationCircles = rotationDegrees / 360f;

                // Debug.Log($"<color=lime>Landing successful! Start: {start_rotation}°, End: {end_rotation}°, Degrees: {rotationDegrees}°, Circles: {rotationCircles}</color>");

                // Call new scoring system with rotation circles
                if (ScoreManager.instance != null)
                {
                    // Debug.Log("Calling ScoreManager.CalculateCircleScore...");
                    ScoreManager.instance.CalculateCircleScore(rotationCircles);
                }

                surfingController.localspeed = (proj*rb.linearVelocity - surfingController.waveBaseSpeed * Vector3.left).magnitude;
                surfingController.SwitchState(surfingController.surfingState);
            }
        }
    }
    public void Enter() {
        start_rotation = surfingController.globalTotalRotation;
        surfingController.waterDrops.Play();
    }
    public void Exit() {
        surfingController.waterDrops.Stop();
    }
    public void HandleRotation() {
        float globalTotalRotation = surfingController.globalTotalRotation;
        float cur_rot_degree = surfingController.cur_rot_degree;
        Quaternion startRotation = surfingController.startRotation;
        float rotationSpeed = surfingController.rotationSpeed_surf*1.5f;
        if (Mathf.Abs(cur_rot_degree - globalTotalRotation) > rotationSpeed * Time.fixedDeltaTime) {
            if (cur_rot_degree < globalTotalRotation) {
                cur_rot_degree += rotationSpeed * Time.fixedDeltaTime;
            } else {
                cur_rot_degree -= rotationSpeed * Time.fixedDeltaTime;
            }
        } else {
            cur_rot_degree = Mathf.Lerp(cur_rot_degree, globalTotalRotation, Time.fixedDeltaTime * 2f);
        }
        surfingController.cur_rot_degree = cur_rot_degree;
        trans.localRotation = Quaternion.Lerp(trans.localRotation, startRotation * Quaternion.Euler(0f, 0f, cur_rot_degree), Time.fixedDeltaTime * 10f);
    }
    public void HandleMovement()
    {
        rb.useGravity = true;
    }
}

public class DeadState : IState
{
    private SurfingController surfingController;
    public float localspeed;
    private Rigidbody rb;
    private Transform trans;
    private float splashSpeed = 40f;
    public float torqueAmount = 720f; // degrees per second

    public DeadState(SurfingController surfingController) {
        this.surfingController = surfingController;
        this.rb = surfingController.rb;
        this.trans = surfingController.transform;
        splashSpeed = surfingController.splashSpeed;
    }

    public void Enter() {
        DeadAnim();
     }
    public void Exit() {

    }
    public void FixedUpdate() {

    }
    private void DeadAnim() {
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, rb.linearVelocity.z);
        //rb.AddForce(Vector3.up * splashSpeed, ForceMode.VelocityChange);
        Vector3 torque = trans.forward * torqueAmount;
        trans.position = trans.position + new Vector3(0, 0, 3f);
        rb.AddTorque(torque, ForceMode.VelocityChange);
        LevelManager.Instance.OnPlayerDied();
    }

}
public class SurfingController : MonoBehaviour
{
    public ParticleSystem waterDrops;
    private IState currentState;
    public float acceleration = 10f;
    // Create state instances
    public WaitingState waitingState;
    public SurfingState surfingState;
    public JumpState jumpState;
    public DeadState deadState;
    public float splashSpeed = 30f;
    public float speedMax = 15f;
    private float lastAngle = 0f;            // Last recorded angle
    public float globalTotalRotation = 0f;
    public float rotationSpeed_surf = 400f;
    public WaveController wave;
    private PlayerTouchMovement m_ptm;
    public LayerMask waterLayerMask;
    public LayerMask dangerLayerMask;
    public Animator animator;
    public float tolerrance = 0.7f;
    public Rigidbody rb { get; private set; }
    public Quaternion startRotation { get; private set; }
    public float cur_rot_degree { get; set; }
    public float localspeed { get; set; }
    public float waveBaseSpeed = 5f;

    [Header("Keyboard Controls")]
    [Tooltip("Max rotation speed from keyboard (degrees per second) - shares with joystick")]
    public float maxKeyboardRotationSpeed = 400f;
    [Tooltip("Keyboard rotation speed multiplier when jumping/in air (matches jump rotation speed)")]
    public float keyboardJumpRotationMultiplier = 1.5f;
    private bool wasKeyboardActiveLastFrame = false;

    [Header("Collision Settings")]
    [Tooltip("Player collision radius (meters)")]
    public float playerRadius = 3f;

    [Tooltip("Max X distance to check for collision optimization (meters)")]
    public float maxXCheckDistance = 20f;

    [Tooltip("Tag for floating objects")]
    public string floatingObjectTag = "FloatingObject";

    [Tooltip("Show collision debug info")]
    public bool showCollisionDebug = false;
    void Start()
    {
        m_ptm = GetComponent<PlayerTouchMovement>();
        rb = GetComponent<Rigidbody>();
        startRotation = transform.localRotation;
        cur_rot_degree = 0f;

        // Sync keyboard max speed with joystick rotation speed
        maxKeyboardRotationSpeed = rotationSpeed_surf;

        // Initialize states passing the state machine (for potential callbacks)
        waitingState = new WaitingState(this);
        surfingState = new SurfingState(this);
        jumpState = new JumpState(this);
        deadState = new DeadState(this);
        // Start in the waiting state
        currentState = waitingState;

        // Subscribe to energy depletion event
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.OnEnergyDepleted += OnEnergyDepleted;
        }
        else
        {
            Debug.LogWarning("SurfingController: EnergyManager not found!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.OnEnergyDepleted -= OnEnergyDepleted;
        }
    }

    /// <summary>
    /// Called when player energy is depleted
    /// </summary>
    private void OnEnergyDepleted()
    {
        Debug.Log("<color=red>Player died from energy depletion!</color>");
        SwitchState(deadState);
    }

    public void SwitchState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void FixedUpdate() {
        Vector2 joystickMovement = m_ptm.GetMovement();
        float keyboardRotation = GetKeyboardRotationInput();

        TrackGlobalRotation(joystickMovement, keyboardRotation);//record rotation all the time.
        currentState?.FixedUpdate();

        // Check for collisions with floating objects
        CheckFloatingObjectCollisions();
    }

    public void ResetPlayer() {

    }

    /// <summary>
    /// Handles keyboard rotation input (Up/Down arrow keys)
    /// Returns the rotation delta for this frame
    /// </summary>
    private float GetKeyboardRotationInput() {
        float rotationDelta = 0f;

        // Apply multiplier if in jump state (in air)
        float speedMultiplier = (currentState == jumpState) ? keyboardJumpRotationMultiplier : 1f;
        float effectiveSpeed = maxKeyboardRotationSpeed * speedMultiplier;

        // Check for arrow key input - instant response
        if (Input.GetKey(KeyCode.UpArrow)) {
            // Up arrow = counter-clockwise (negative rotation)
            rotationDelta = -effectiveSpeed * Time.fixedDeltaTime;
        }
        else if (Input.GetKey(KeyCode.DownArrow)) {
            // Down arrow = clockwise (positive rotation)
            rotationDelta = effectiveSpeed * Time.fixedDeltaTime;
        }

        // Return the rotation change for this frame
        return rotationDelta;
    }

    private void TrackGlobalRotation(Vector2 movement, float keyboardRotation) {//called each frame
        float totalDeltaAngle = 0f;
        bool hasJoystickInput = false;

        // Handle joystick input
        if (movement.magnitude > 0.1f) {
            hasJoystickInput = true;
            // Calculate current angle (0-360 degrees)
            float currentAngle = Mathf.Atan2(-movement.y, -movement.x) * Mathf.Rad2Deg;
            if (currentAngle < 0) currentAngle += 360f;

            // Calculate angle change
            float deltaAngle = currentAngle - lastAngle;

            // Handle angle wrap-around at 0/360 boundary
            if (deltaAngle > 180f) deltaAngle -= 360f;
            if (deltaAngle < -180f) deltaAngle += 360f;

            totalDeltaAngle += deltaAngle;

            // Update lastAngle
            lastAngle = currentAngle;
        }

        // Check keyboard input
        bool hasKeyboardInput = Mathf.Abs(keyboardRotation) > 0.01f;

        // KEYBOARD ONLY: Stop rotation immediately when key is released
        if (wasKeyboardActiveLastFrame && !hasKeyboardInput) {
            // Keyboard was just released - sync to stop rotation immediately
            globalTotalRotation = cur_rot_degree;
        }

        // Add keyboard rotation (only if keyboard is active)
        if (hasKeyboardInput) {
            totalDeltaAngle += keyboardRotation;
        }

        // Update global total rotation
        globalTotalRotation += totalDeltaAngle;

        // Track keyboard state for next frame
        wasKeyboardActiveLastFrame = hasKeyboardInput;

        // Update animator based on rotation direction
        if (totalDeltaAngle < -0.1f) {
            animator.SetBool("Right", true);
            animator.SetBool("Left", false);
        } else if(totalDeltaAngle > 0.1f) {
            animator.SetBool("Right", false);
            animator.SetBool("Left", true);
        } else {
            animator.SetBool("Right", false);
            animator.SetBool("Left", false);
        }
    }

    /// <summary>
    /// Checks for collisions with floating objects using distance-based detection
    /// Optimized by checking X distance first
    /// </summary>
    private void CheckFloatingObjectCollisions()
    {
        // Get player position (XY plane only)
        Vector3 playerPos = transform.position;

        // Find all floating objects by tag
        GameObject[] floatingObjects = GameObject.FindGameObjectsWithTag(floatingObjectTag);

        if (floatingObjects.Length == 0)
            return;

        foreach (GameObject obj in floatingObjects)
        {
            if (obj == null)
                continue;

            // Get FloatingObject component
            FloatingObject floatingObj = obj.GetComponent<FloatingObject>();
            if (floatingObj == null)
                continue;

            // Only collide if object can be hit
            if (!floatingObj.CanCollide())
                continue;

            // Optimization: Check X distance first
            Vector3 objPos = obj.transform.position;
            float xDistance = Mathf.Abs(playerPos.x - objPos.x);

            // Skip if X distance is too far
            if (xDistance > maxXCheckDistance)
                continue;

            // Calculate distance in XY plane only (ignore Z)
            float distanceXY = Vector2.Distance(
                new Vector2(playerPos.x, playerPos.y),
                new Vector2(objPos.x, objPos.y)
            );

            // Get combined collision radius
            float combinedRadius = playerRadius + floatingObj.GetCollisionRadius();

            // Check collision
            if (distanceXY <= combinedRadius)
            {
                // Collision detected!
                bool hit = floatingObj.OnPlayerCollision();

                if (hit && showCollisionDebug)
                {
                    Debug.Log($"<color=yellow>Collision with {obj.name} at distance {distanceXY:F2}m (combined radius: {combinedRadius:F2}m)</color>");
                }
            }
        }
    }
}
