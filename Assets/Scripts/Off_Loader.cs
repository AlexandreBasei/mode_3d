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
        recalculerNormales(); // Ajouter cette ligne après load_off (qui appelle applyLoop)
        generateMesh();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void load_off()
    {
        string filePath = "../Mode3d_tp5/Assets/Off_Meshes/" + offFileName + ".off";

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

        applyLoop();
    }

    private void recalculerNormales()
    {
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

    public void applyLoop()
    {
        // Dictionnaire pour stocker les arêtes uniques : clé = (min, max), valeur = index du futur sommet milieu
        Dictionary<(int, int), int> aretes = new Dictionary<(int, int), int>();

        // Dictionnaire pour stocker les sommets opposés de chaque arête (v_left et v_right)
        Dictionary<(int, int), List<int>> aretesOpposes = new Dictionary<(int, int), List<int>>();

        // Dictionnaire pour stocker les voisins de chaque sommet (pour calculer la valence)
        Dictionary<int, HashSet<int>> voisins = new Dictionary<int, HashSet<int>>();

        // Initialiser les voisins pour chaque sommet
        for (int i = 0; i < sommets.Count; i++)
        {
            voisins[i] = new HashSet<int>();
        }

        // Collecter les arêtes, leurs sommets opposés et les voisins
        foreach (int[] face in facettes)
        {
            for (int i = 0; i < face.Length; i++)
            {
                int a = face[i];
                int b = face[(i + 1) % face.Length];
                int oppose = face[(i + 2) % face.Length];

                // Ajouter les voisins
                voisins[a].Add(b);
                voisins[b].Add(a);

                var cle = (Mathf.Min(a, b), Mathf.Max(a, b));

                if (!aretes.ContainsKey(cle))
                {
                    aretes.Add(cle, -1);
                    aretesOpposes.Add(cle, new List<int>());
                }

                aretesOpposes[cle].Add(oppose);
            }
        }

        // ============================================================
        // ÉTAPE 1 : Créer les nouveaux sommets pour chaque arête
        // v_nouveau = (3/8) * (v1 + v2) + (1/8) * (v_left + v_right)
        // ============================================================
        List<Vector3> nouveauxSommets = new List<Vector3>(sommets);

        foreach (var arete in aretes.Keys.ToList())
        {
            int v1 = arete.Item1;
            int v2 = arete.Item2;

            Vector3 posV1 = sommets[v1];
            Vector3 posV2 = sommets[v2];

            Vector3 vNouveau;

            if (aretesOpposes[arete].Count == 2) // Arête intérieure
            {
                int vLeft = aretesOpposes[arete][0];
                int vRight = aretesOpposes[arete][1];

                Vector3 posLeft = sommets[vLeft];
                Vector3 posRight = sommets[vRight];

                // Formule : e = (3/8)(v1 + v2) + (1/8)(v_left + v_right)
                vNouveau = (3f / 8f) * (posV1 + posV2) + (1f / 8f) * (posLeft + posRight);
            }
            else // Arête de bord
            {
                vNouveau = (posV1 + posV2) / 2f;
            }

            aretes[arete] = nouveauxSommets.Count;
            nouveauxSommets.Add(vNouveau);
        }

        // ============================================================
        // ÉTAPE 2 : Mettre à jour les positions des sommets existants
        // v' = (1 - n*alpha) * v + alpha * SommeVoisins(v)
        // ============================================================
        for (int i = 0; i < sommets.Count; i++)
        {
            int n = voisins[i].Count; // Valence

            if (n < 2) continue;

            // Calcul de alpha selon la formule de Loop
            float alpha;
            if (n == 3)
            {
                alpha = 3f / 16f;
            }
            else
            {
                float temp = (3f / 8f) + (1f / 4f) * Mathf.Cos(2f * Mathf.PI / n);
                alpha = (1f / n) * ((5f / 8f) - (temp * temp));
            }

            // Calcul de la somme des voisins
            Vector3 sommeVoisins = Vector3.zero;
            foreach (int voisin in voisins[i])
            {
                sommeVoisins += sommets[voisin];
            }

            // Nouvelle position : v' = (1 - n*alpha) * v + alpha * SommeVoisins
            nouveauxSommets[i] = (1f - n * alpha) * sommets[i] + alpha * sommeVoisins;
        }

        // ============================================================
        // ÉTAPE 3 : Créer les nouvelles faces (1 triangle → 4 triangles)
        // ============================================================
        List<int[]> nouvellesFacettes = new List<int[]>();

        foreach (int[] face in facettes)
        {
            int x1 = face[0];
            int x2 = face[1];
            int x3 = face[2];

            // Récupérer les indices des nouveaux sommets sur les arêtes
            int x1x2 = aretes[(Mathf.Min(x1, x2), Mathf.Max(x1, x2))];
            int x2x3 = aretes[(Mathf.Min(x2, x3), Mathf.Max(x2, x3))];
            int x3x1 = aretes[(Mathf.Min(x3, x1), Mathf.Max(x3, x1))];

            // Créer les 4 nouveaux triangles
            nouvellesFacettes.Add(new int[] { x1, x1x2, x3x1 });
            nouvellesFacettes.Add(new int[] { x2, x2x3, x1x2 });
            nouvellesFacettes.Add(new int[] { x3, x3x1, x2x3 });
            nouvellesFacettes.Add(new int[] { x1x2, x2x3, x3x1 }); // Triangle central
        }

        // Remplacer les anciennes données par les nouvelles
        sommets = nouveauxSommets;
        facettes = nouvellesFacettes;

        Debug.Log($"Subdivision Loop terminée : {sommets.Count} sommets, {facettes.Count} facettes");

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
