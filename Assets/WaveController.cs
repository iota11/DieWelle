using UnityEngine;

public class WaveController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Material mat;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        mat.SetFloat("_time", Time.time);
    }
}
