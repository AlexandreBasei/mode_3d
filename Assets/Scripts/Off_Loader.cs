using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;
public class Off_Loader : MonoBehaviour
{
    private int nbSommets;
    private int nbFacettes;
    private int nbAretes;
    private List<Vector3> sommets = new List<Vector3>();
    private List<int[]> facettes = new List<int[]>();
    [SerializeField] private string offFileName;
    private MeshFilter mf;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        load_off();
        generateMesh();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void load_off()
    {
        string filePath = "../Off_Meshes/" + offFileName + ".off";

        //Lire le fichier
        string[] lines = File.ReadAllLines(filePath);

        if (lines[0] != "OFF")
        {
            Debug.LogError("Le fichier n'est pas au format OFF");
            return;
        }

        string[] header = lines[1].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        nbSommets = int.Parse(header[0], CultureInfo.InvariantCulture);
        nbFacettes = int.Parse(header[1], CultureInfo.InvariantCulture);
        nbAretes = int.Parse(header[2], CultureInfo.InvariantCulture);

        Vector3 vecteurCentre = Vector3.zero;

        for (int i = 2; i < lines.Length; i++)
        {
            string[] line = lines[i].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (line.Length == 3) //Sommets
            {
                float x = float.Parse(line[0], CultureInfo.InvariantCulture);
                float y = float.Parse(line[1], CultureInfo.InvariantCulture);
                float z = float.Parse(line[2], CultureInfo.InvariantCulture);

                sommets.Add(new Vector3(x, y, z));

                //On additionne toutes les positions pour préparer le vecteur centre (ex2)
                vecteurCentre = new Vector3(vecteurCentre[0] + x, vecteurCentre[1] + y, vecteurCentre[2] + z);
            }
            if (line.Length >= 4) //Facettes
            {
                int count = int.Parse(line[0], CultureInfo.InvariantCulture);
                int[] face = new int[count];
                for (int j = 0; j < count; j++)
                {
                    face[j] = int.Parse(line[j + 1], CultureInfo.InvariantCulture);
                }
                facettes.Add(face);
            }
        }

        //Calcul final du vecteur centre (ex2)
        if (sommets.Count > 0)
        {
            vecteurCentre = new Vector3(vecteurCentre[0] / sommets.Count, vecteurCentre[1] / sommets.Count, vecteurCentre[2] / sommets.Count);
        }

        float maxAbsCoord = 0f;
        
        for (int i = 0; i < sommets.Count; i++)
        {
            sommets[i] = sommets[i] - vecteurCentre; //Application du vecteur centre à tous les sommets

            maxAbsCoord = max(maxAbsCoord, sommets[i][0], sommets[i][1], sommets[i][2]);
        }
    }

    public void generateMesh()
    {
        mf = gameObject.GetComponent<MeshFilter>();

        Mesh mesh = new Mesh { name = "CustomMesh" };

        mesh.vertices = sommets.ToArray();
        int[] triangles = facettes.SelectMany(f => f).ToArray();
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
    }
}
