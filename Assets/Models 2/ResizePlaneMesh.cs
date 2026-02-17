using UnityEngine;

public class ResizePlaneMesh : MonoBehaviour
{
    public float width = 5f;  // Set the new width
    public float height = 5f; // Set the new height

    void Start()
    {
        ResizePlane(width, height);
    }

    void ResizePlane(float newWidth, float newHeight)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        // Unity's default plane has 10x10 units, so adjust accordingly
        float widthFactor = newWidth / 10f;
        float heightFactor = newHeight / 10f;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(vertices[i].x * widthFactor, vertices[i].y, vertices[i].z * heightFactor);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
}
