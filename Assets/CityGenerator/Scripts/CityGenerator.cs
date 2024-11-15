using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    public static CityGenerator Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern for CityGenerator thus
        // we need only one instance of it
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy the new instance
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the object alive between scenes
        }

    }

    public void GenerateCity()
    {
        // TODO: Generate the city
    }
}