using UnityEngine;
public interface IState
{
    void HandleRotation();
}

// Waiting state implementation
public class WaitingState : IState
{
    private SurfingController surfingController;

    public WaitingState(SurfingController controller) {
        surfingController = controller;
    }

    public void HandleRotation() {
        // Insert waiting-specific rotation logic here
        Debug.Log("WaitingState: Handling Rotation");
    }
}

// Surfing state implementation
public class SurfingState : IState
{
    private SurfingController surfingController;

    public SurfingState(SurfingController controller) {
        surfingController = controller;
    }

    public void HandleRotation() {
        // Insert surfing-specific rotation logic here
        Debug.Log("SurfingState: Handling Rotation");
    }
}

// Jump state implementation
public class JumpState : IState
{
    private SurfingController surfingController;

    public JumpState(SurfingController controller) {
        surfingController = controller;
    }

    public void HandleRotation() {
        // Insert jump-specific rotation logic here
        Debug.Log("JumpState: Handling Rotation");
    }
}
public class SurfingController : MonoBehaviour
{
    private IState currentState;

    // Create state instances
    private WaitingState waitingState;
    private SurfingState surfingState;
    private JumpState jumpState;
    private float lastAngle = 0f;            // Last recorded angle
    private float totalRotation = 0f;
    public float globalTotalRotation = 0f;
    public float rotationSpeed = 10f;
    private PlayerTouchMovement m_ptm;
    private Rigidbody rb;
    private Quaternion startRotation;
    float cur_rot_degree = 0.0f;

    void Start()
    {
        m_ptm = GetComponent<PlayerTouchMovement>();
        rb = GetComponent<Rigidbody>();
        startRotation = transform.localRotation;
        // Initialize states passing the state machine (for potential callbacks)
        waitingState = new WaitingState(this);
        surfingState = new SurfingState(this);
        jumpState = new JumpState(this);

        // Start in the waiting state
        currentState = waitingState;
    }

    private void FixedUpdate() {
        Vector2 movement = m_ptm.GetMovement();

        TrackGlobalRotation(movement);
        // Call HandleRotation of the current state
        currentState?.HandleRotation();
       if(Mathf.Abs(cur_rot_degree - globalTotalRotation) > rotationSpeed * Time.fixedDeltaTime) {
            if(cur_rot_degree < globalTotalRotation) {
                cur_rot_degree +=rotationSpeed * Time.fixedDeltaTime;
            } else {
                cur_rot_degree -= rotationSpeed * Time.fixedDeltaTime;
            }
        } else {
            cur_rot_degree = Mathf.Lerp(cur_rot_degree, globalTotalRotation, Time.fixedDeltaTime*2f);
        }
        transform.localRotation = Quaternion.Lerp(transform.localRotation, startRotation * Quaternion.Euler(0f, 0f, cur_rot_degree), Time.fixedDeltaTime*10f);
    }


    private void TrackGlobalRotation(Vector2 movement) {//called each frame
        if (movement.magnitude > 0.1f) {
            // Calculate current angle (0-360 degrees)
            float currentAngle = Mathf.Atan2(-movement.y, -movement.x) * Mathf.Rad2Deg;
            if (currentAngle < 0) currentAngle += 360f;

            // Calculate angle change
            float deltaAngle = currentAngle - lastAngle;

            // Handle angle wrap-around at 0/360 boundary
            if (deltaAngle > 180f) deltaAngle -= 360f;
            if (deltaAngle < -180f) deltaAngle += 360f;

            // Add absolute value of angle change to total rotation
            globalTotalRotation += deltaAngle;

            // Update lastAngle
            lastAngle = currentAngle;
        }

    }
}
