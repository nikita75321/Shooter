using UnityEngine;

[DisallowMultipleComponent]
public abstract class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private bool dontDestroy = true;
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("MonoSingleton: object of type already exists, instance will be destroyed = " + typeof(T).Name);
            Destroy(gameObject);
            return;
        }

        if (dontDestroy)
        {
            DontDestroyOnLoad(this);
        }
      
        Instance = this as T;
    }
}
