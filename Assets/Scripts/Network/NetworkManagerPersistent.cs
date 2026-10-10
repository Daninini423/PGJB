using UnityEngine;

public class NetworkManagerPersistent : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}