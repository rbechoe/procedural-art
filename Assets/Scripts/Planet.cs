using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [Range(2, 256)]
    public int resolution = 10;

    private MeshFilter[] meshFilters;
    private Terrainface[] terrainFaces;

    private void Initialize()
    {
        meshFilters = new MeshFilter[6];
        terrainFaces = new Terrainface[6];

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < 6; i++)
        {
            GameObject meshObj = new GameObject("mesh");
            meshObj.transform.parent = transform;

            meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
            meshFilters[i] = meshObj.AddComponent<MeshFilter>();
            meshFilters[i].sharedMesh = new Mesh();

            terrainFaces[i] = new Terrainface(meshFilters[i].sharedMesh, resolution, directions[i]);
        }
        // 13.30 in eerste filmpje
    }
}
