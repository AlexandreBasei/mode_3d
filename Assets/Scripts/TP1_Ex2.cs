using UnityEngine;

public class TP1_Ex2 : MonoBehaviour
{
    private MeshFilter mf;
    public int radius = 2;
    public int height = 5;
    public int nbMeridien = 4;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mf = gameObject.GetComponent<MeshFilter>();

        CreateCylinder();
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    void CreateCylinder()
    {
        Mesh mesh = new Mesh { name = "Cylindre" };

        int ringCount = nbMeridien + 1;
        Vector3[] vertices = new Vector3[ringCount * 2 + 2];//+2 pour les centres

        int sideTriangleCount = nbMeridien * 6;
        int capsTriangleCount = nbMeridien * 6;
        int[] triangles = new int[sideTriangleCount + capsTriangleCount];

        float angleStep = 2 * Mathf.PI / nbMeridien;

        // Create vertices
        for (int i = 0; i <= nbMeridien; i++)
        {
            float angle = i * angleStep;
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);

            vertices[i] = new Vector3(x, 0, z); // Bottom circle
            vertices[i + ringCount] = new Vector3(x, height, z); // Top circle
        }

        //add center vertices
        int bottomCenterIndex = ringCount * 2;
        int topCenterIndex = bottomCenterIndex + 1;
        vertices[bottomCenterIndex] = new Vector3(0f, 0f, 0f); // Bottom center
        vertices[topCenterIndex] = new Vector3(0f, height, 0f); // Top center

        // Create side triangles
        int t = 0;
        for (int i = 0; i < nbMeridien; i++)
        {
            int bottomLeft = i;
            int bottomRight = (i + 1) % ringCount;
            int topLeft = i + ringCount;
            int topRight = ((i + 1) % ringCount) + ringCount;

            // First triangle
            triangles[t++] = bottomLeft;
            triangles[t++] = topLeft;
            triangles[t++] = bottomRight;

            // Second triangle
            triangles[t++] = bottomRight;
            triangles[t++] = topLeft;
            triangles[t++] = topRight;
        }
        // Create bottom cap triangles
        for (int i = 0; i < nbMeridien; i++)
        {
            int bottomLeft = i;
            int bottomRight = (i + 1) % ringCount;
            // Triangle
            triangles[t++] = bottomLeft;
            triangles[t++] = bottomRight;
            triangles[t++] = bottomCenterIndex;
        }

        // Create top cap triangles
        for (int i = 0; i < nbMeridien; i++)
        {
            int topLeft = i + ringCount;
            int topRight = ((i + 1) % ringCount) + ringCount;
            // Triangle
            triangles[t++] = topRight;
            triangles[t++] = topLeft;
            triangles[t++] = topCenterIndex;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;
    }
}
