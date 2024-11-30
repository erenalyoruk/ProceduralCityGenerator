using UnityEngine;
using UnityEngine.Assertions.Must;

public class WorldBuilder : MonoBehaviour
{
    public WallBuilder WallBuilder { get; private set; }

    public static WorldBuilder Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
