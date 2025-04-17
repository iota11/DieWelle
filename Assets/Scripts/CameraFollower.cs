using UnityEngine;
using System.Collections;
public class CameraFollower : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Vector3 offset;
    public Transform target;
    public bool isFollowing =  true;
    public float followingSpeed = 3f;
    private float curFollowingSpeed = 0.0f;
    void Start()
    {
        offset = transform.position - target.position;
        Follow();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 IdealPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, IdealPos, Time.fixedDeltaTime * curFollowingSpeed);
    }

    public void Unfollow() {
        this.isFollowing = false;
        StartCoroutine(Decreasefollow());
    }
    IEnumerator Decreasefollow() {
        while (followingSpeed > 0.0f) {
            curFollowingSpeed = curFollowingSpeed - Time.fixedDeltaTime * 2f;
            yield return null;
        }
        curFollowingSpeed = 0.0f;
    }
    public void Follow() {
        isFollowing = true;
        curFollowingSpeed = followingSpeed;
    }
    public void Follow(Transform _target) {
        target = _target;
        isFollowing = true;
        curFollowingSpeed = followingSpeed;
    }
}
