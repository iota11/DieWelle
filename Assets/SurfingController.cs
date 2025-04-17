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

            if (proj < 0.8f) {
               surfingController.SwitchState(surfingController.deadState);
            } else {
                end_rotation = surfingController.globalTotalRotation;
                int rotationItvl = (int)Mathf.Abs(end_rotation - start_rotation);
                ScoreManager.instance.AddScore(rotationItvl);
                surfingController.localspeed = (proj*rb.linearVelocity - surfingController.waveBaseSpeed * Vector3.left).magnitude;
                surfingController.SwitchState(surfingController.surfingState);
            }
        }
    }
    public void Enter() {
        start_rotation = surfingController.globalTotalRotation;
    }
    public void Exit() { }
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
    private IState currentState;
    public float acceleration = 10f;
    // Create state instances
    public WaitingState waitingState;
    public SurfingState surfingState;
    public JumpState jumpState;
    public DeadState deadState;
    public float splashSpeed = 30f;
    private float lastAngle = 0f;            // Last recorded angle
    public float globalTotalRotation = 0f;
    public float rotationSpeed_surf = 400f;
    public WaveController wave;
    private PlayerTouchMovement m_ptm;
    public LayerMask waterLayerMask;
    public LayerMask dangerLayerMask;
    public Animator animator; 
    public Rigidbody rb { get; private set; }
    public Quaternion startRotation { get; private set; }
    public float cur_rot_degree { get; set; }
    public float localspeed { get; set; }
    public float waveBaseSpeed = 5f;
    void Start()
    {
        m_ptm = GetComponent<PlayerTouchMovement>();
        rb = GetComponent<Rigidbody>();
        startRotation = transform.localRotation;
        cur_rot_degree = 0f;

        // Initialize states passing the state machine (for potential callbacks)
        waitingState = new WaitingState(this);
        surfingState = new SurfingState(this);
        jumpState = new JumpState(this);
        deadState = new DeadState(this);
        // Start in the waiting state
        currentState = waitingState;
    }

    public void SwitchState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void FixedUpdate() {
        Vector2 movement = m_ptm.GetMovement();

        TrackGlobalRotation(movement);//record rotation all the time.
        currentState?.FixedUpdate();

    }

    public void ResetPlayer() {

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
            if (deltaAngle < 0f) {
                animator.SetBool("Right", true);
                animator.SetBool("Left", false);

            } else if(deltaAngle >0f) {
                animator.SetBool("Right", false);
                animator.SetBool("Left", true);
            } else {
                animator.SetBool("Right", false);
                animator.SetBool("Left", false);
            }
            // Update lastAngle
            lastAngle = currentAngle;
        }

    }
}
