using UnityEngine;
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
    public Text rotationText;                // Text to show rotation achievements
    public Text comboText;                   // Text to show current combo
    
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

    // Entry angle detection
    public float minSafeEntryAngle = 30f;    // Minimum safe angle for water entry (in degrees)

    // Rotation tracking
    private float totalRotation = 0f;        // Total rotation accumulated in air
    private bool isTrackingRotation = false; // Are we tracking rotation
    private bool hasStartedRotation = false; // Has rotation tracking started
    private float lastAngle = 0f;            // Last recorded angle
    private bool[] rotationRewardsEarned = new bool[3] { false, false, false }; // Track which rotation rewards have been earned

    // Rotation text display
    private float rotationTextDuration = 2.0f;   // How long to show rotation text
    private float rotationTextTimer = 0f;
    private bool isShowingRotationText = false;

    // Combo system
    private int comboCount = 0;              // Current combo count
    private bool lastJumpHadRotation = false; // Did the last jump have a rotation reward
    private float comboTextDuration = 2.0f;   // How long to show combo text
    private float comboTextTimer = 0f;
    private bool isShowingComboText = false;

    void Start()
    {
        // Initialize components
        m_ptm = GetComponent<PlayerTouchMovement>();
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        
        // Initialize game state
        currentLives = maxLives;
        
        // Initialize UI
        if (deathText != null) deathText.gameObject.SetActive(false);
        if (jumpHeightText != null) jumpHeightText.gameObject.SetActive(false);
        if (rotationText != null) rotationText.gameObject.SetActive(false);
        if (comboText != null) comboText.gameObject.SetActive(false);
        
        UpdateScoreDisplay();
        UpdateLivesDisplay();
        
        // Initialize timers
        isShowingComboText = false;
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
                if (deathText != null) deathText.gameObject.SetActive(false);
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

        // Rotation text timer
        if (isShowingRotationText) 
        {
            rotationTextTimer += Time.fixedDeltaTime;
            if (rotationTextTimer >= rotationTextDuration) 
            {
                if (rotationText != null) rotationText.gameObject.SetActive(false);
                isShowingRotationText = false;
            }
        }

        // Combo text timer
        if (isShowingComboText) 
        {
            comboTextTimer += Time.fixedDeltaTime;
            if (comboTextTimer >= comboTextDuration) 
            {
                if (comboText != null) comboText.gameObject.SetActive(false);
                isShowingComboText = false;
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
                currentScore += heightScore;
                UpdateScoreDisplay();
            }
            
            // Reset height tracking
            trackingHeight = false;
            maxHeightReached = 0f;
            
            // Hide jump height text
            if (jumpHeightText != null) jumpHeightText.gameObject.SetActive(false);
        }
        
        // Calculate rotation rewards
        CalculateRotationRewards();
        
        // Reset rotation tracking
        totalRotation = 0f;
        hasStartedRotation = false;
        rotationRewardsEarned = new bool[3] { false, false, false };
        
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
            if (jumpHeightText != null)
            {
                jumpHeightText.gameObject.SetActive(true);
                UpdateJumpHeightDisplay(0);
            }
        }
        else if (trackingHeight)
        {
            // Update max height if we're going higher
            if (currentHeight > maxHeightReached)
            {
                maxHeightReached = currentHeight;
                
                // Update jump height display
                int currentJumpHeight = Mathf.FloorToInt(maxHeightReached - heightThreshold);
                UpdateJumpHeightDisplay(currentJumpHeight);
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
        
        // Reset height tracking
        trackingHeight = false;
        maxHeightReached = 0f;
        if (jumpHeightText != null) jumpHeightText.gameObject.SetActive(false);
        
        // Reset rotation tracking
        isTrackingRotation = false;
        totalRotation = 0f;
        if (rotationText != null) rotationText.gameObject.SetActive(false);
        isShowingRotationText = false;

        // Reset combo
        comboCount = 0;
        lastJumpHadRotation = false;
        if (comboText != null) comboText.gameObject.SetActive(false);
        isShowingComboText = false;
    }

    private void CalculateRotationRewards()
    {
        // First rotation (270+ degrees counts as complete)
        if (totalRotation >= 270f)
        {
            int rotationScore = 0;
            string rotationMessage = "";
            
            // Third rotation (990 degrees = 270 + 360 + 360)
            if (totalRotation >= 990f)
            {
                rotationScore = 1000;
                rotationMessage = "TRIPLE 360!";
            }
            // Second rotation (630 degrees = 270 + 360)
            else if (totalRotation >= 630f)
            {
                rotationScore = 100;
                rotationMessage = "DOUBLE 360!";
            }
            // First rotation (270+ degrees)
            else
            {
                rotationScore = 10;
                rotationMessage = "360!";
            }
            
            // Increment combo if we had a rotation in the last jump
            if (lastJumpHadRotation)
            {
                comboCount++;
            }
            else
            {
                // First rotation in a new combo
                comboCount = 1;
            }
            
            // Apply combo multiplier to score
            int comboMultiplier = comboCount;
            int finalScore = rotationScore * comboMultiplier;
            
            // Add rotation score to total score
            currentScore += finalScore;
            UpdateScoreDisplay();
            
            // Update combo text
            UpdateComboText();
            
            // Show rotation text with combo info
            if (comboCount > 1)
            {
                rotationMessage += "\nCOMBO x" + comboCount + "!";
                rotationMessage += "\n+" + finalScore + " POINTS!";
            }
            else
            {
                rotationMessage += "\n+" + finalScore + " POINTS!";
            }
            
            // Show rotation text
            ShowRotationText(rotationMessage);
            
            // Mark that this jump had a rotation
            lastJumpHadRotation = true;
        }
        else
        {
            // No rotation this jump, break the combo
            comboCount = 0;
            lastJumpHadRotation = false;
            UpdateComboText(); // This will hide the combo text
        }
    }

    private void ShowRotationText(string message)
    {
        if (rotationText != null)
        {
            rotationText.text = message;
            rotationText.gameObject.SetActive(true);
            isShowingRotationText = true;
            rotationTextTimer = 0f;
        }
    }

    private void UpdateComboText()
    {
        if (comboText != null)
        {
            if (comboCount > 1)
            {
                comboText.text = "COMBO x" + comboCount;
                comboText.gameObject.SetActive(true);
                isShowingComboText = true;
                comboTextTimer = 0f;
            }
            else
            {
                comboText.gameObject.SetActive(false);
                isShowingComboText = false;
            }
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
    }

    private void UpdateJumpHeightDisplay(int height)
    {
        if (jumpHeightText != null)
        {
            jumpHeightText.text = height + "m";
        }
    }

    private void UpdateLivesDisplay()
    {
        if (livesText != null)
        {
            livesText.text = "Lives: " + currentLives;
        }
    }
}
