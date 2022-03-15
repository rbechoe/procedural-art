using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace
{
    private Mesh mesh;

    private int resolution;

    private Vector3 localUp;
    private Vector3 axisA;
    private Vector3 axisB;

    private ShapeGenerator shapeGenerator;

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp)
    {
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;
        this.shapeGenerator = shapeGenerator;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    public void ConstructMesh()
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triangleIndex = 0;
        Vector2[] uv = mesh.uv;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnSphere = pointOnCube.normalized;
                vertices[i] = shapeGenerator.CalculatePointOnPlanet(pointOnSphere);

                // make a quad by assigning 2 triangles that are not on the edge of the plane
                if (x != resolution - 1 && y != resolution - 1)
                {
                    triangles[triangleIndex] = i;
                    triangles[triangleIndex + 1] = i + resolution + 1;
                    triangles[triangleIndex + 2] = i + resolution;

                    triangles[triangleIndex + 3] = i;
                    triangles[triangleIndex + 4] = i + 1;
                    triangles[triangleIndex + 5] = i + 1 + resolution;

                    triangleIndex += 6;
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.uv = uv;
    }

    public void UpdateUVs(ColorGenerator colorGenerator)
    {
        Vector2[] uv = new Vector2[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnSphere = pointOnCube.normalized;

                uv[i] = new Vector2(colorGenerator.BiomePercentFromPoint(pointOnSphere), 0);
            }
        }
        mesh.uv = uv;
    }
}
