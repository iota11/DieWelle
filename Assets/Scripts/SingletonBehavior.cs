using UnityEngine;

public class SingletonBehavior<T> : MonoBehaviour where T : MonoBehaviour
{
    // The singleton instance of the class
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Multiple instances of: {typeof(T)}");
            Destroy(Instance.gameObject);
        }

        Instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (this as T == Instance)
        {
            Instance = null;
        }
    }
}
