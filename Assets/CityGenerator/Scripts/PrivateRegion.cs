using UnityEngine;
using System.Collections;

public class PrivateRegion : MonoBehaviour
{
    GameObject quad;

    public PrivateRegion(int point, int normal, int size, PCBuilding belongsTo)
    {
    }

    // Use this for initialization
    void Start()
    {
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.AddComponent<PrivateRegion>();


    }

    // Update is called once per frame
    void Update()
    {

    }
}
