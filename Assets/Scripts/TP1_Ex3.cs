using UnityEngine;

public class TP1_Ex3 : MonoBehaviour
{
    private MeshFilter mf;
    public int radius = 5;
    public int nbMeridien = 8;
    public int nbParallele = 6;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mf = gameObject.GetComponent<MeshFilter>();

        createSphere();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void createSphere()
    {
        Mesh mesh = new Mesh { name = "Sphere" };

        int vertPerSide = (nbMeridien + 1) * (nbParallele + 1);
        Vector3[] vertices = new Vector3[vertPerSide];
        int triangleCount = nbMeridien * nbParallele * 6;
        int[] triangles = new int[triangleCount];
        float angleMeridienStep = 2 * Mathf.PI / nbMeridien;
        float angleParalleleStep = Mathf.PI / nbParallele;
        
        // Create vertices
        for (int i = 0; i <= nbParallele; i++)
        {
            float angleParallele = i * angleParalleleStep;
            float y = radius * Mathf.Cos(angleParallele);
            float r = radius * Mathf.Sin(angleParallele);

            for (int j = 0; j <= nbMeridien; j++)
            {
                float angleMeridien = j * angleMeridienStep;
                float x = r * Mathf.Cos(angleMeridien);
                float z = r * Mathf.Sin(angleMeridien);

                vertices[i * (nbMeridien + 1) + j] = new Vector3(x, y, z);
            }
        }

        // Create triangles
        int t = 0;
        for (int i = 0; i < nbParallele; i++)
        {
            for (int j = 0; j < nbMeridien; j++)
            {
                int bl = i * (nbMeridien + 1) + j;
                int br = bl + 1;
                int tl = bl + (nbMeridien + 1);
                int tr = tl + 1;

                // First triangle
                triangles[t++] = br;
                triangles[t++] = tl;
                triangles[t++] = bl;

                // Second triangle
                triangles[t++] = tr;
                triangles[t++] = tl;
                triangles[t++] = br;
                
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

    }
}
