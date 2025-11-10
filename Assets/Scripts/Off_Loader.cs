using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
public class Off_Loader : MonoBehaviour
{
    private int nbSommets;
    private int nbFacettes;
    private int nbAretes;
    private List<Vector3> sommets = new List<Vector3>();
    private List<int[]> facettes = new List<int[]>();
    private List<Vector3> normales = new List<Vector3>();
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
        string filePath = "../Mode3d_tp2/Assets/Off_Meshes/" + offFileName + ".off";

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

            //Calcul de la coordonnée maximale absolue (ex3)
            maxAbsCoord = Mathf.Max(maxAbsCoord, Mathf.Abs(sommets[i][0]), Mathf.Abs(sommets[i][1]), Mathf.Abs(sommets[i][2]));

            //Produit vectoriel

        }

        for (int i = 0; i < sommets.Count; i++)
        {
            sommets[i] = sommets[i] / maxAbsCoord; //Normalisation de la taille (ex3)
        }

        //Calcul des normales pour chaque facette (ex4), il doit y avoir autant de normales que de vertices dans le mesh
        Vector3[] vertexNormals = new Vector3[sommets.Count];
        int[] normalCounts = new int[sommets.Count];

        for (int i = 0; i < facettes.Count; i++)
        {
            int[] face = facettes[i];

            Vector3 v0 = sommets[face[0]];
            Vector3 v1 = sommets[face[1]];
            Vector3 v2 = sommets[face[2]];

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 faceNormal = Vector3.Cross(edge1, edge2);

            // Accumulate face normal into each vertex of the face
            for (int j = 0; j < face.Length; j++)
            {
                int idx = face[j];
                vertexNormals[idx] += faceNormal;
                normalCounts[idx]++;
            }
        }

        normales.Clear();
        for (int i = 0; i < sommets.Count; i++)
        {
            if (normalCounts[i] > 0)
            {
                Vector3 averagedNormal = vertexNormals[i] / normalCounts[i];
                normales.Add(averagedNormal.normalized);
            }
            else
            {
                normales.Add(Vector3.up);
            }
        }
    }

    public void generateMesh()
    {
        mf = gameObject.GetComponent<MeshFilter>();

        Mesh mesh = new Mesh { name = "CustomMesh" };

        mesh.vertices = sommets.ToArray();
        int[] triangles = facettes.SelectMany(f => f).ToArray();
        mesh.triangles = triangles;
        mesh.normals = normales.ToArray();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
    }

    //Fonction d'export du mesh en OFF (ex4)
    public void export_off()
    {
        string filePath = "../Mode3d_tp2/Assets/Off_Meshes/" + offFileName + "_export.off";

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("OFF");
            writer.WriteLine($"{sommets.Count} {facettes.Count} 0");

            //Écriture des sommets
            foreach (Vector3 sommet in sommets)
            {
                writer.WriteLine($"{sommet.x.ToString(CultureInfo.InvariantCulture)} {sommet.y.ToString(CultureInfo.InvariantCulture)} {sommet.z.ToString(CultureInfo.InvariantCulture)}");
            }

            //Écriture des facettes
            foreach (int[] facette in facettes)
            {
                writer.Write(facette.Length.ToString(CultureInfo.InvariantCulture));
                foreach (int index in facette)
                {
                    writer.Write($" {index.ToString(CultureInfo.InvariantCulture)}");
                }
                writer.WriteLine();
            }
        }

        Debug.Log("Export OFF terminé : " + filePath);
    }
}
