using UnityEngine;
using UnityEngine.Windows;
using UnityEngine.UI; 

public class PlayerController : MonoBehaviour
{
    // Movement and physics components
    private PlayerTouchMovement m_ptm;
    private Rigidbody rb;
    
    // Speed settings
    public float speed = 10f;                // Base movement speed
    public float maxSpeed = 15f;             // Maximum possible speed
    public float rotationSpeed = 100f;       // Max rotation speed
    public float accelerationRate = 3f;      // How quickly to accelerate
    private float currentSpeed = 0f;         // Current speed
    
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
    
    // UI references
    public Text deathText;                   // Text to show on death
    public Text scoreText;                   // Text to show score
    public Text jumpHeightText;              // Text to show current jump height
    public Text livesText;                   // Text to show remaining lives
    
    // Death text timer
    public float deathTextDuration = 2.0f;   // How long to show death text
    private float deathTextTimer = 0f;
    private bool isShowingDeathText = false;

    // Scoring system
    private int currentScore = 0;            // Current player score
    private float maxHeightReached = 0f;     // Track max height during jump
    private bool trackingHeight = false;     // Are we tracking height for scoring

    // Lives system
    public int maxLives = 3;                 // Maximum number of lives
    private int currentLives;                // Current number of lives

    void Start()
    {
        m_ptm = GetComponent<PlayerTouchMovement>();
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        
        // Initialize lives
        currentLives = maxLives;
        
        // Hide the death text at start
        if (deathText != null)
        {
            deathText.gameObject.SetActive(false);
        }
        
        // Initialize score display
        UpdateScoreDisplay();
        
        // Initialize lives display
        UpdateLivesDisplay();
        
        // Hide jump height text initially
        if (jumpHeightText != null)
        {
            jumpHeightText.gameObject.SetActive(false);
        }
    }

    void FixedUpdate() 
    {
        float currentHeight = transform.position.y;

        // Handle death text timer
        if (isShowingDeathText) 
        {
            deathTextTimer += Time.fixedDeltaTime;
            if (deathTextTimer >= deathTextDuration) 
            {
                // After set duration, hide the text
                if (deathText != null) 
                {
                    deathText.gameObject.SetActive(false);
                }
                isShowingDeathText = false;
            }
            else
            {
                // Only return early if we're still showing the text
                // Keep player stationary while showing death text
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                
                // Keep input disabled during death text display
                inputEnabled = false;
                
                // Skip the rest of the update while showing death text
                return;
            }
        }

        // Check for player death
        if (currentHeight < deathHeight) 
        {
            ResetPlayer();
            return; // Skip the rest of the update
        }

        // Get input
        Vector2 movement = m_ptm.GetMovement();
        
        // Check if joystick has been reset to center position
        if (!inputEnabled && !isShowingDeathText) 
        {
            // If joystick is close to center, mark it as reset
            if (movement.magnitude < 0.2f) 
            {
                joystickReset = true;
                inputEnabled = true; // Re-enable input when joystick is reset
            }
            
            // Keep player stationary while input is disabled
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return; // Skip the rest of the update until input is re-enabled
        }
        
        // Before game starts, keep player completely stationary
        if (!isGameStarted) 
        {
            // Disable gravity and zero out all velocities
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Check if game should start (joystick pointing downward)
            if (movement.y < -0.7f) 
            {
                isGameStarted = true;
                currentSpeed = 0f; // Start with zero speed
            }
            
            // Allow rotation control even before game starts
            if (movement.magnitude > 0.1f) 
            {
                // Calculate target rotation angle (based on input direction)
                float targetAngle = Mathf.Atan2(-movement.y, -movement.x) * Mathf.Rad2Deg;
                
                // Smoothly rotate to target angle
                Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
            
            return; // Skip the rest of the update until game starts
        }
        
        // Game has started - normal gameplay logic
        
        // Directly control character orientation
        if (movement.magnitude > 0.1f) 
        {
            // Calculate target rotation angle (based on input direction)
            float targetAngle = Mathf.Atan2(-movement.y, -movement.x) * Mathf.Rad2Deg;
            
            // Smoothly rotate to target angle
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        if (currentHeight < heightThreshold) 
        {
            // On the wave
            rb.useGravity = false;
            
            // If we were in air and now entering water, calculate score
            if (wasInAir) 
            {
                wasInAir = false;
                
                // Calculate score based on max height reached
                if (trackingHeight)
                {
                    int heightScore = Mathf.FloorToInt(maxHeightReached - heightThreshold);
                    if (heightScore > 0)
                    {
                        // Add to score
                        currentScore += heightScore;
                        
                        // Update score display
                        UpdateScoreDisplay();
                    }
                    
                    // Reset height tracking
                    trackingHeight = false;
                    maxHeightReached = 0f;
                    
                    // Hide jump height text when landing
                    if (jumpHeightText != null)
                    {
                        jumpHeightText.gameObject.SetActive(false);
                    }
                }
                
                // Get the magnitude of the horizontal air velocity
                Vector3 horizontalAirVelocity = new Vector3(airVelocity.x, 0, airVelocity.z);
                float airSpeed = horizontalAirVelocity.magnitude;
                
                // Use the air speed directly, but ensure it's not less than current speed
                currentSpeed = Mathf.Max(airSpeed, currentSpeed);
                
                // Apply the speed immediately to avoid a sudden change
                rb.linearVelocity = -transform.right * currentSpeed;
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
        else 
        {
            // In the air
            rb.useGravity = true;
            
            // Start tracking height when we leave the wave
            if (wasOnWave)
            {
                trackingHeight = true;
                maxHeightReached = currentHeight;
                
                // Show jump height text when leaving wave
                if (jumpHeightText != null)
                {
                    jumpHeightText.gameObject.SetActive(true);
                    UpdateJumpHeightDisplay(0); // Start at 0
                }
            }
            else if (trackingHeight)
            {
                // Update max height if we're going higher
                if (currentHeight > maxHeightReached)
                {
                    maxHeightReached = currentHeight;
                    
                    // Update jump height display with current height
                    int currentJumpHeight = Mathf.FloorToInt(maxHeightReached - heightThreshold);
                    UpdateJumpHeightDisplay(currentJumpHeight);
                }
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
            // Let physics handle the rest while in air - don't modify velocity every frame
        }
    }

    private void ResetPlayer() 
    {
        // Reset position
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        
        // Reset physics
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
        UpdateLivesDisplay();
        
        // Check if game over (no lives left)
        if (currentLives <= 0)
        {
            // Reset lives
            currentLives = maxLives;
            UpdateLivesDisplay();
            
            // Reset score when all lives are lost
            currentScore = 0;
            UpdateScoreDisplay();
            
            // Could add game over text or other game over logic here
        }
        
        // Show death text and start timer
        if (deathText != null) 
        {
            deathText.gameObject.SetActive(true);
            isShowingDeathText = true;
            deathTextTimer = 0f;
        }
        
        // Reset tracking variables
        wasOnWave = false;
        wasInAir = false;
        airVelocity = Vector3.zero;
        
        // Hide jump height text on reset
        if (jumpHeightText != null)
        {
            jumpHeightText.gameObject.SetActive(false);
        }
        
        // Reset height tracking
        trackingHeight = false;
        maxHeightReached = 0f;
    }

    // Add this method to update the score display
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
    }

    // Add this method to update the jump height display
    private void UpdateJumpHeightDisplay(int height)
    {
        if (jumpHeightText != null)
        {
            jumpHeightText.text = height + "m";
        }
    }

    // Add this method to update the lives display
    private void UpdateLivesDisplay()
    {
        if (livesText != null)
        {
            livesText.text = "Lives: " + currentLives;
        }
    }
}
