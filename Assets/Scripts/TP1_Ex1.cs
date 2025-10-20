
using UnityEngine;


public class TP1_Ex1 : MonoBehaviour
{
    private MeshFilter mf;
    public int height = 5;
    public int width = 3;
    public int nbLines = 4;
    public int nbColumns = 4;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mf = gameObject.GetComponent<MeshFilter>();

        Mesh mesh = new Mesh { name = "Plan" };

        int nbVertices = (nbLines + 1) * (nbColumns + 1);
        Vector3[] vertices = new Vector3[nbVertices];

        // Create vertices
        for (int i = 0; i <= nbLines; i++)
        {
            for (int j = 0; j <= nbColumns; j++)
            {
                float x = (width / (float)nbColumns) * j;
                float y = (height / (float)nbLines) * i;
                vertices[i * (nbColumns + 1) + j] = new Vector3(x, y, 0);
            }
        }

        int[] triangles = new int[nbLines * nbColumns * 6];

        int t = 0;
        for (int i = 0; i < nbLines; i++)
        {
            for (int j = 0; j < nbColumns; j++)
            {
                int bl = i * (nbColumns + 1) + j;
                int br = bl + 1;
                int tl = bl + (nbColumns + 1);
                int tr = tl + 1;

                // First triangle
                triangles[t++] = bl;
                triangles[t++] = tl;
                triangles[t++] = br;

                // Second triangle
                triangles[t++] = br;
                triangles[t++] = tl;
                triangles[t++] = tr;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
