using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Vector3 offset;
    public Transform target;
    void Start()
    {
        offset = transform.position - target.position; 
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 IdealPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, IdealPos, Time.fixedDeltaTime * 3f);
    }
}
