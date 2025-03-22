using UnityEngine;
using UnityEngine.Windows;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private PlayerTouchMovement m_ptm;
    private Vector3 velocity;
    public float speed = 5.0f;
    public float heightThreshold = 5f;
    public float rotationSpeed = 100f; // Max rotation speed
    public float forwardSpeed = 10f;
    private Rigidbody rb;

    void Start()
    {
        m_ptm = GetComponent<PlayerTouchMovement>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void FixedUpdate() {
        float currentHeight = transform.position.y;

        // Rotation always applies
        float rotationZ = m_ptm.GetMovement().y * rotationSpeed * Time.fixedDeltaTime;
        transform.Rotate(0f, 0f, -rotationZ); // Negative to rotate correctly with surf-style input

        /*
        if (currentHeight < heightThreshold) {
            velocity = -transform.right * forwardSpeed;
            transform.Rotate(0f, 0f, -rotationZ/3f); // Negative to rotate correctly with surf-style input


        } else {
            velocity += new Vector3(0,-9.8f,0) *Time.fixedDeltaTime;
            transform.Rotate(0f, 0f, -rotationZ); // Negative to rotate correctly with surf-style input


        }
        transform.Translate(velocity * Time.fixedDeltaTime);
        */
        if (currentHeight < heightThreshold) {
            // On the wave
            rb.useGravity = false;
            rb.linearVelocity =- transform.right * forwardSpeed;

           
        } else {
            // In the air
            rb.useGravity = true;

        }
    }

}
