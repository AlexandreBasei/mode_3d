
using UnityEngine;


public class Mode3D_TP1 : MonoBehaviour
{
    public int height = 5;
    public int width = 3;

    public int nbLignes = 10;
    public int nbColonnes = 10;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var mf = gameObject.GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        var mr = gameObject.GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh { name = "Plans" };

    
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(width, 0, 0),
            new Vector3(0, height, 0),
            new Vector3(width, height, 0)
        };

        int[] triangles = new int[] { 0, 1, 2, 2, 1, 3 };

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
