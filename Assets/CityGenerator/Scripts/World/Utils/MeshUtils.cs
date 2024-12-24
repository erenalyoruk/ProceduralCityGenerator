using UnityEngine;

public static class MeshUtils
{
    /// <summary>
    /// Merges multiple mesh segments into a single mesh, including UVs.
    /// </summary>
    /// <param name="objectsToMerge">Array of GameObjects whose meshes will be merged.</param>
    /// <returns>Parent GameObject with the merged mesh.</returns>
    public static GameObject MergeMeshes(GameObject[] objectsToMerge)
    {
        if (objectsToMerge == null || objectsToMerge.Length == 0)
        {
            Debug.LogError("No objects to merge.");
            return null;
        }

        GameObject mergedObject = new GameObject("MergedMesh");
        MeshFilter parentMeshFilter = mergedObject.AddComponent<MeshFilter>();
        MeshRenderer parentMeshRenderer = mergedObject.AddComponent<MeshRenderer>();

        Mesh combinedMesh = new Mesh();
        CombineInstance[] combine = new CombineInstance[objectsToMerge.Length];

        Vector2 uvOffset = Vector2.zero;
        Vector2 uvScale = Vector2.one / objectsToMerge.Length;

        for (int i = 0; i < objectsToMerge.Length; i++)
        {
            MeshFilter meshFilter = objectsToMerge[i].GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning(
                    $"GameObject {objectsToMerge[i].name} does not have a valid MeshFilter or Mesh."
                );
                continue;
            }

            Mesh mesh = Object.Instantiate(meshFilter.sharedMesh);

            combine[i].mesh = mesh;
            combine[i].transform = meshFilter.transform.localToWorldMatrix;
        }

        combinedMesh.CombineMeshes(combine, true, true, true);

        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();
        combinedMesh.RecalculateTangents();

        parentMeshFilter.sharedMesh = combinedMesh;
        parentMeshRenderer.sharedMaterial = objectsToMerge[0]
            .GetComponent<MeshRenderer>()
            .sharedMaterial;

        return mergedObject;
    }
}
