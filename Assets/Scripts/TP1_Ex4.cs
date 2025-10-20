using UnityEngine;

public class TP1_Ex4 : MonoBehaviour
{
    private MeshFilter mf;
    public int radius = 2;
    public int height = 5;
    public int nbMeridien = 4;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mf = gameObject.GetComponent<MeshFilter>();

        CreateCone();
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    void CreateCone()
    {
        if (nbMeridien < 3) nbMeridien = 3;

        Mesh mesh = new Mesh { name = "Cone" };
        mesh.Clear();

        // vertices: nbMeridien (périmètre) + 1 (centre base) + 1 (apex)
        int bottomCenterIndex = nbMeridien;
        int apexIndex = nbMeridien + 1;
        Vector3[] vertices = new Vector3[nbMeridien + 2];
        Vector2[] uv = new Vector2[vertices.Length];

        float angleStep = 2 * Mathf.PI / nbMeridien;

        // bottom circle vertices
        for (int i = 0; i < nbMeridien; i++)
        {
            float angle = i * angleStep;
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);
            vertices[i] = new Vector3(x, 0f, z);
            uv[i] = new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f);
        }

        // center of base
        vertices[bottomCenterIndex] = new Vector3(0f, 0f, 0f);
        uv[bottomCenterIndex] = new Vector2(0.5f, 0.5f);

        // apex
        vertices[apexIndex] = new Vector3(0f, height, 0f);
        uv[apexIndex] = new Vector2(0.5f, 1f);

        // triangles: sides (nbMeridien) + base (nbMeridien)
        int[] triangles = new int[nbMeridien * 3 * 2];
        int t = 0;

        // sides: each face is a triangle (bottom_i, apex, bottom_i+1)
        for (int i = 0; i < nbMeridien; i++)
        {
            int next = (i + 1) % nbMeridien;
            triangles[t++] = i;
            triangles[t++] = apexIndex;
            triangles[t++] = next;
        }

        // base cap: center, next, current (winding so normal points down)
        for (int i = 0; i < nbMeridien; i++)
        {
            int next = (i + 1) % nbMeridien;
            triangles[t++] = bottomCenterIndex;
            triangles[t++] = next;
            triangles[t++] = i;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (mf == null) mf = gameObject.GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;
    }
}
