using UnityEngine;

public class WaveController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Material mat;
    public float speed = 10.0f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = transform.position + new Vector3( -speed * Time.fixedDeltaTime, 0, 0);
        mat.SetFloat("_time", Time.time);
    }
}
