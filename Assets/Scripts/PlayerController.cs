using UnityEngine;
using UnityEngine.UI; 

public class PlayerController : MonoBehaviour
{
    // Movement and physics components
    private PlayerTouchMovement m_ptm;
    private Rigidbody rb;

    // Speed settings
    [Header("Player Speed")]
    public float speed = 10f;                // Base movement speed
    public float maxSpeed = 15f;             // Maximum possible speed
    public float rotationSpeed = 100f;       // Max rotation speed
    public float accelerationRate = 3f;      // How quickly to accelerate
    private float currentSpeed = 0f;         // Current speed

    [SerializeField]
    private ScoreManager scoreManager;

    // Environment settings
    public float heightThreshold = 5f;       // Height that separates wave and air
    public float deathHeight = -5f;          // Height at which player dies
    public Vector3 startPosition;            // Starting position for reset
    
    // Game state
    private bool isGameStarted = false;      // Track if game has started
    
    // Input handling
    private bool inputEnabled = true;
    private bool joystickReset = true;       // Track if joystick has returned to center
    
    // Transition tracking
    private bool wasOnWave = false;
    private Vector3 airVelocity;
    private bool wasInAir = false;

    // Death text timer
    public float deathTextDuration = 2.0f;   // How long to show death text
    private float deathTextTimer = 0f;
    private bool isShowingDeathText = false;

    // Lives system
    public int maxLives = 3;                 // Maximum number of lives
    private int currentLives;                // Current number of lives

    // Entry angle detection
    public float minSafeEntryAngle = 30f;    // Minimum safe angle for water entry (in degrees)

    // Play stun moves
    private bool isTrackingRotation = false; // Are we tracking rotation
    private bool trackingHeight = false;     // Are we tracking height for scoring
    private float maxHeightReached = 0f;     // Track max height during jump
    private bool hasStartedRotation = false; // Has rotation tracking started
    private float lastAngle = 0f;            // Last recorded angle
    private float totalRotation = 0f;
    private bool[] rotationRewardsEarned = new bool[3] { false, false, false }; // Track which rotation rewards have been earned

    void Start()
    {
        // Initialize components
        m_ptm = GetComponent<PlayerTouchMovement>();
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        
        // Initialize game state
        currentLives = maxLives;
        
        // Initialize UI
        TextManager.Instance.SetTextFieldActive(TextType.death, false);
        TextManager.Instance.SetTextFieldActive(TextType.jumpHeight, false);

        TextManager.Instance.SetText(TextType.lives, "Lives: " + currentLives);
    }

    void FixedUpdate() 
    {
        float currentHeight = transform.position.y;

        // Handle UI timers
        HandleTextTimers();
        
        // Check for player death
        if (currentHeight < deathHeight) 
        {
            ResetPlayer();
            return;
        }

        // Get input
        Vector2 movement = m_ptm.GetMovement();
        
        // Check if joystick has been reset to center position
        if (!inputEnabled && !isShowingDeathText) 
        {
            if (movement.magnitude < 0.2f) 
            {
                joystickReset = true;
                inputEnabled = true;
            }
            
            // Keep player stationary while input is disabled
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }
        
        // Before game starts, keep player completely stationary
        if (!isGameStarted) 
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Check if game should start (joystick pointing downward)
            if (movement.y < -0.7f) 
            {
                isGameStarted = true;
                currentSpeed = 0f;
            }
            
            // Allow rotation control even before game starts
            HandleRotation(movement);
            return;
        }
        
        // Game has started - normal gameplay logic
        HandleRotation(movement);

        if (currentHeight < heightThreshold) 
        {
            // On the wave
            HandleWaveMovement(movement);
        } 
        else 
        {
            // In the air
            HandleAirMovement(movement, currentHeight);
        }
    }

    private void HandleTextTimers()
    {
        // Death text timer
        if (isShowingDeathText) 
        {
            deathTextTimer += Time.fixedDeltaTime;
            if (deathTextTimer >= deathTextDuration) 
            {
                TextManager.Instance.SetTextFieldActive(TextType.death, false);
                isShowingDeathText = false;
            }
            else
            {
                // Keep player stationary while showing death text
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                inputEnabled = false;
                return;
            }
        }
    }

    private void HandleRotation(Vector2 movement)
    {
        if (movement.magnitude > 0.1f) 
        {
            // Calculate target rotation angle
            float targetAngle = Mathf.Atan2(-movement.y, -movement.x) * Mathf.Rad2Deg;
            
            // Smoothly rotate to target angle
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void HandleWaveMovement(Vector2 movement)
    {
        rb.useGravity = false;
        
        // If we were in air and now entering water
        if (wasInAir) 
        {
            HandleWaterEntry(movement);
        }
        
        // Acceleration logic
        if (currentSpeed < speed) 
        {
            currentSpeed = Mathf.Min(currentSpeed + accelerationRate * Time.fixedDeltaTime, speed);
        }
        
        // Additional acceleration based on joystick
        if (movement.y > 0.2f) 
        {
            // Accelerate when pointing upward
            currentSpeed = Mathf.Min(currentSpeed + accelerationRate * movement.y * Time.fixedDeltaTime, maxSpeed);
        }
        
        // Apply speed in the direction the player is facing
        rb.linearVelocity = -transform.right * currentSpeed;
        
        // Reset wave tracking
        wasOnWave = true;
    }

    private void HandleWaterEntry(Vector2 movement)
    {
        // Store the last significant movement direction for entry angle check
        Vector2 entryDirection = movement;
        
        // If current movement is too small, use the player's current rotation instead
        if (movement.magnitude <= 0.1f)
        {
            // Convert player's rotation to a direction vector
            float playerAngle = transform.rotation.eulerAngles.z;
            entryDirection = new Vector2(
                -Mathf.Cos(playerAngle * Mathf.Deg2Rad), 
                -Mathf.Sin(playerAngle * Mathf.Deg2Rad)
            );
        }
        
        // Check entry angle if game has started
        if (isGameStarted)
        {
            // Calculate angle between entry direction and vertical (down)
            float entryAngle = Mathf.Abs(Vector2.Angle(entryDirection, Vector2.down));
            
            // If angle is too close to horizontal, trigger death
            if (entryAngle > 90f - minSafeEntryAngle)
            {
                ResetPlayer();
                return;
            }
        }
        
        wasInAir = false;
        isTrackingRotation = false;
        
        // Calculate score based on max height reached
        if (trackingHeight)
        {
            int heightScore = Mathf.FloorToInt(maxHeightReached - heightThreshold);
            if (heightScore > 0)
            {
                scoreManager.AddScore(heightScore);
            }
            
            // Reset height tracking
            trackingHeight = false;
            maxHeightReached = 0f;
            
            // Hide jump height text
            TextManager.Instance.SetTextFieldActive(TextType.jumpHeight, false);
        }
        
        // Calculate rotation rewards
        scoreManager.CalculateRotationRewards(totalRotation);
        
        // Get the magnitude of the horizontal air velocity
        Vector3 horizontalAirVelocity = new Vector3(airVelocity.x, 0, airVelocity.z);
        float airSpeed = horizontalAirVelocity.magnitude;
        
        // Use the air speed directly, but ensure it's not less than current speed
        currentSpeed = Mathf.Max(airSpeed, currentSpeed);
        
        // Apply the speed immediately to avoid a sudden change
        rb.linearVelocity = -transform.right * currentSpeed;
    }

    private void HandleAirMovement(Vector2 movement, float currentHeight)
    {
        rb.useGravity = true;
        
        // Start tracking height when we leave the wave
        if (wasOnWave)
        {
            // Initialize tracking
            trackingHeight = true;
            maxHeightReached = currentHeight;
            
            // Start tracking rotation
            isTrackingRotation = true;
            totalRotation = 0f;
            hasStartedRotation = false;
            rotationRewardsEarned = new bool[3] { false, false, false };

            // Show jump height text
            TextManager.Instance.SetTextFieldActive(TextType.jumpHeight, true);
            TextManager.Instance.SetText(TextType.jumpHeight, "0m");
        }
        else if (trackingHeight)
        {
            // Update max height if we're going higher
            if (currentHeight > maxHeightReached)
            {
                maxHeightReached = currentHeight;
                
                // Update jump height display
                int currentJumpHeight = Mathf.FloorToInt(maxHeightReached - heightThreshold);
                TextManager.Instance.SetText(TextType.jumpHeight, currentJumpHeight + "m");
            }

            // Track rotation
            TrackRotation(movement);
        }
        
        // Store current air velocity for when we return to water
        airVelocity = rb.linearVelocity;
        wasInAir = true;
        
        // Only set horizontal velocity once when transitioning from wave to air
        if (wasOnWave) 
        {
            wasOnWave = false;
            
            // Preserve current horizontal velocity but allow gravity to affect vertical
            Vector3 horizontalVelocity = -transform.right * currentSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    private void TrackRotation(Vector2 movement)
    {
        if (isTrackingRotation && movement.magnitude > 0.5f)
        {
            // Calculate current angle (0-360 degrees)
            float currentAngle = Mathf.Atan2(-movement.y, -movement.x) * Mathf.Rad2Deg;
            if (currentAngle < 0) currentAngle += 360f;
            
            // If this is the first valid input, initialize lastAngle
            if (!hasStartedRotation)
            {
                lastAngle = currentAngle;
                hasStartedRotation = true;
            }
            else
            {
                // Calculate angle change
                float deltaAngle = currentAngle - lastAngle;
                
                // Handle angle wrap-around at 0/360 boundary
                if (deltaAngle > 180f) deltaAngle -= 360f;
                if (deltaAngle < -180f) deltaAngle += 360f;
                
                // Add absolute value of angle change to total rotation
                totalRotation += Mathf.Abs(deltaAngle);
                
                // Update lastAngle
                lastAngle = currentAngle;
            }
        }
    }

    private void ResetPlayer() 
    {
        // Reset position and physics
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Reset game state
        isGameStarted = false;
        currentSpeed = 0f;
        
        // Disable input until joystick is reset
        inputEnabled = false;
        joystickReset = false;
        
        // Decrease lives
        currentLives--;
        TextManager.Instance.SetText(TextType.lives, "Lives: " + currentLives);

        // Reset rotation tracking
        totalRotation = 0f;
        isTrackingRotation = false;

        // Check if game over (no lives left)
        if (currentLives <= 0)
        {
            // Reset lives
            currentLives = maxLives;
            TextManager.Instance.SetText(TextType.lives, "Lives: " + currentLives);
        }

        // Show death text and start timer
        TextManager.Instance.SetTextFieldActive(TextType.death, true);
        isShowingDeathText = true;
        deathTextTimer = 0f;

        // Reset tracking variables
        wasOnWave = false;
        wasInAir = false;
        airVelocity = Vector3.zero;
        
        // Reset height tracking
        trackingHeight = false;
        maxHeightReached = 0f;
        TextManager.Instance.SetTextFieldActive(TextType.jumpHeight, false);

        scoreManager.ResetText();
    }
}
