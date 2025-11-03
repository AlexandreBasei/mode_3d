using UnityEngine;

public class TP1_Ex4 : MonoBehaviour
{
    private MeshFilter mf;
    public int radius = 2;
    public int height = 5;
    public int nbMeridien = 4;
    public int tronquedHeight = 2;
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

        float h = Mathf.Max(1f, (float)height);
        float topY = Mathf.Clamp((float)tronquedHeight, 0f, h); // hauteur de la coupe depuis la base
        // si topY >= h on retrouve le cône pointu ; sinon tronc de cône entre y=0 (base) et y=topY (haut)
        bool isNotTruncated = topY >= h - Mathf.Epsilon;

        float angleStep = 2 * Mathf.PI / nbMeridien;

        if (isNotTruncated)
        {
            // cône pointu
            int bottomCenterIndex = nbMeridien;
            int apexIndex = nbMeridien + 1;
            Vector3[] vertices = new Vector3[nbMeridien + 2];
            Vector2[] uv = new Vector2[vertices.Length];

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


            vertices[apexIndex] = new Vector3(0f, h, 0f);
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

            // base cap: center, next, current
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
        }
        else
        {
            // tronc de cône
            // top radius obtenu par interpolation linéaire entre base (y=0, radius) et apex (y=h, radius=0)
            float topRadius = radius * (1f - (topY / h));

            // indices:
            // 0..nbMeridien-1 -> bottom ring
            // nbMeridien..2*nbMeridien-1 -> top ring
            // bottomCenter = 2*nbMeridien
            // topCenter = 2*nbMeridien + 1
            int bottomCenterIndex = 2 * nbMeridien;
            int topCenterIndex = 2 * nbMeridien + 1;
            Vector3[] vertices = new Vector3[2 * nbMeridien + 2];
            Vector2[] uv = new Vector2[vertices.Length];

            // bottom ring
            for (int i = 0; i < nbMeridien; i++)
            {
                float angle = i * angleStep;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);
                vertices[i] = new Vector3(x, 0f, z);
                uv[i] = new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, 0f);
            }

            // top ring
            for (int i = 0; i < nbMeridien; i++)
            {
                float angle = i * angleStep;
                float x = topRadius * Mathf.Cos(angle);
                float z = topRadius * Mathf.Sin(angle);
                vertices[nbMeridien + i] = new Vector3(x, topY, z);
                uv[nbMeridien + i] = new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, 1f);
            }

            // centers
            vertices[bottomCenterIndex] = new Vector3(0f, 0f, 0f);
            uv[bottomCenterIndex] = new Vector2(0.5f, 0.5f);
            vertices[topCenterIndex] = new Vector3(0f, topY, 0f);
            uv[topCenterIndex] = new Vector2(0.5f, 0.5f);

            // triangles:
            // sides: nbMeridien * 2 triangles
            // base cap: nbMeridien triangles
            // top cap: nbMeridien triangles
            int[] triangles = new int[nbMeridien * 12];
            int ti = 0;

            // sides: for each meridian build two triangles (bottom_i, top_i, top_next) and (bottom_i, top_next, bottom_next)
            for (int i = 0; i < nbMeridien; i++)
            {
                int next = (i + 1) % nbMeridien;
                int bottomI = i;
                int bottomNext = next;
                int topI = nbMeridien + i;
                int topNext = nbMeridien + next;

                // triangle 1
                triangles[ti++] = bottomI;
                triangles[ti++] = topI;
                triangles[ti++] = topNext;

                // triangle 2
                triangles[ti++] = bottomI;
                triangles[ti++] = topNext;
                triangles[ti++] = bottomNext;
            }

            // base cap: center, next, current (normal down)
            for (int i = 0; i < nbMeridien; i++)
            {
                int next = (i + 1) % nbMeridien;
                triangles[ti++] = bottomCenterIndex;
                triangles[ti++] = next;
                triangles[ti++] = i;
            }

            // top cap: centerTop, currentTop, nextTop (normal up)
            for (int i = 0; i < nbMeridien; i++)
            {
                int next = (i + 1) % nbMeridien;
                int topI = nbMeridien + i;
                int topNext = nbMeridien + next;
                triangles[ti++] = topCenterIndex;
                triangles[ti++] = topNext;
                triangles[ti++] = topI;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (mf == null) mf = gameObject.GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;
    }
}
