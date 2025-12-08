using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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

    [SerializeField] private float epsilonValue = 0.1f;
    [SerializeField] private bool clustered = false;
    [SerializeField] private bool drawGizmos = true;

    private Bounds box;
    private List<Cluster> clusterGrid = new List<Cluster>();
    private Dictionary<int, int> vertexWeight = new Dictionary<int, int>();

    private MeshFilter mf;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        load_off();
        generateMesh();
        if (clustered)
        {
            createBox();
        }
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

    public void createBox()
    {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;

        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        if (mesh != null)
        {
            foreach (Vector3 vertex in mesh.vertices)
            {
                Vector3 worldVertex = transform.TransformPoint(vertex);
                min = Vector3.Min(min, worldVertex);
                max = Vector3.Max(max, worldVertex);
            }
        }

        Vector3 boxCenter = (min + max) * 0.5f;
        Vector3 boxSize = max - min;
        boxSize = Vector3.Max(boxSize, Vector3.one * 0.0001f);
        box = new Bounds(boxCenter, boxSize);

        Debug.Log($"Box créée - Centre: {box.center}, Taille: {box.size}");

        // -------------------------------------- Grille

        //Nombre de cubes de la grille
        int nbCubesX = Mathf.CeilToInt(box.size.x / epsilonValue);
        int nbCubesY = Mathf.CeilToInt(box.size.y / epsilonValue);
        int nbCubesZ = Mathf.CeilToInt(box.size.z / epsilonValue);

        Vector3 boxMinPoint = box.center - (box.size / 2);

        for (int i = 0; i < nbCubesX; i++)
        {
            for (int j = 0; j < nbCubesY; j++)
            {
                for (int k = 0; k < nbCubesZ; k++)
                {
                    float cubeCenterX = boxMinPoint.x + (i * epsilonValue) + (epsilonValue / 2);
                    float cubeCenterY = boxMinPoint.y + (j * epsilonValue) + (epsilonValue / 2);
                    float cubeCenterZ = boxMinPoint.z + (k * epsilonValue) + (epsilonValue / 2);

                    Bounds cubeBounds = new Bounds(new Vector3(cubeCenterX, cubeCenterY, cubeCenterZ), new Vector3(epsilonValue, epsilonValue, epsilonValue));

                    Cluster clusterCube = new Cluster();
                    clusterCube.bounds = cubeBounds;

                    clusterGrid.Add(clusterCube);
                }
            }
        }

        calculateVertexWeights();

        // ✅ CORRECTION 1 : Compter les sommets hors limites
        int outOfBoundsCount = 0;

        // Ajout des vertices du mesh dans la clusterGrid
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(mesh.vertices[i]);
            int indiceX = Mathf.FloorToInt((worldVertex.x - boxMinPoint.x) / epsilonValue);
            int indiceY = Mathf.FloorToInt((worldVertex.y - boxMinPoint.y) / epsilonValue);
            int indiceZ = Mathf.FloorToInt((worldVertex.z - boxMinPoint.z) / epsilonValue);

            if ((indiceX >= 0 && indiceX < nbCubesX) && (indiceY >= 0 && indiceY < nbCubesY) && (indiceZ >= 0 && indiceZ < nbCubesZ))
            {
                int index1D = indiceX + indiceY * nbCubesX + indiceZ * nbCubesX * nbCubesY;
                clusterGrid[index1D].vertices.Add(mesh.vertices[i]);
                clusterGrid[index1D].vertexIndices.Add(i);
            }
            else
            {
                outOfBoundsCount++;  // ✅ Juste compter, ne pas logger
            }
        }

        if (outOfBoundsCount > 0)
        {
            Debug.LogWarning($"{outOfBoundsCount} sommets hors des limites de la grille (ignorés)");
        }

        // Calculer le sommet représentatif que l'on va garder
        foreach (Cluster cluster in clusterGrid)
        {
            if (cluster.vertices.Count > 0)
            {
                float sumX = 0;
                float sumY = 0;
                float sumZ = 0;
                float totalWeight = 0;

                foreach (int vertexIndex in cluster.vertexIndices)
                {
                    int poids = vertexWeight[vertexIndex];
                    Vector3 sommet = sommets[vertexIndex];

                    sumX += sommet.x * poids;
                    sumY += sommet.y * poids;
                    sumZ += sommet.z * poids;
                    totalWeight += poids;
                }

                cluster.finalVertice = new Vector3(sumX / totalWeight, sumY / totalWeight, sumZ / totalWeight);
            }
        }

        int clustersWithVertices = clusterGrid.Count(c => c.vertexIndices.Count > 0);
        Debug.Log($"Clusters avec sommets : {clustersWithVertices} / {clusterGrid.Count}");
        Debug.Log($"Exemple de finalVertice : {clusterGrid.First(c => c.vertexIndices.Count > 0).finalVertice}");

        // ✅ CORRECTION 2 : Créer un mapping pour TOUS les sommets
        List<Vector3> newVertices = new List<Vector3>();
        Dictionary<int, int> mapping = new Dictionary<int, int>();
        int newIndice = 0;

        // D'abord, mapper les sommets qui sont dans des clusters
        foreach (Cluster cluster in clusterGrid)
        {
            if (cluster.vertices.Count > 0)
            {
                newVertices.Add(cluster.finalVertice);

                foreach (int oldVertexIndex in cluster.vertexIndices)
                {
                    mapping[oldVertexIndex] = newIndice;
                }
                newIndice++;
            }
        }

        // ✅ CORRECTION 3 : Gérer les sommets non mappés
        // Pour les sommets qui n'ont pas été assignés à un cluster, les garder tels quels
        for (int i = 0; i < sommets.Count; i++)
        {
            if (!mapping.ContainsKey(i))
            {
                // Ajouter le sommet original
                newVertices.Add(sommets[i]);
                mapping[i] = newIndice;
                newIndice++;
            }
        }

        Debug.Log($"Sommets originaux : {sommets.Count} -> Nouveaux sommets : {newVertices.Count}");
        Debug.Log($"Mapping créé pour {mapping.Count} sommets");

        // Reconstruire les facettes
        List<int[]> newFacettes = new List<int[]>();
        int facettesDegenerees = 0;

        foreach (int[] facette in facettes)
        {
            int[] newFacette = new int[3];

            for (int i = 0; i < 3; i++)
            {
                int oldIndice = facette[i];
                newFacette[i] = mapping[oldIndice];  // ✅ Plus d'erreur car tous les sommets sont mappés
            }

            if ((newFacette[0] != newFacette[1]) && (newFacette[0] != newFacette[2]) && (newFacette[1] != newFacette[2]))
            {
                newFacettes.Add(newFacette);
            }
            else
            {
                facettesDegenerees++;
            }
        }

        Debug.Log($"Facettes originales : {facettes.Count} → Nouvelles facettes : {newFacettes.Count}");
        Debug.Log($"Triangles dégénérés supprimés : {facettesDegenerees}");

        // Mettre à jour les données du mesh
        sommets = newVertices;
        facettes = newFacettes;

        // ✅ CORRECTION 4 : Recalculer les normales
        calculateNormals();

        // Régénérer le mesh simplifié
        generateMesh();

        Debug.Log("Mesh simplifié régénéré !");
    }

    private void calculateVertexWeights()
    {
        // Initialiser tous les sommets à 0
        for (int i = 0; i < sommets.Count; i++)
        {
            vertexWeight[i] = 0;
        }

        //  Parcourir les facettes et augmenter le poid des sommets utilisés
        foreach (var facette in facettes)
        {
            for (int i = 0; i < facette.Length; i++)
            {
                vertexWeight[facette[i]]++;
            }
        }

        // Remplacer les poids de 0 par 1 (pour éviter division par zéro)
        for (int i = 0; i < sommets.Count; i++)
        {
            if (vertexWeight[i] == 0)
            {
                vertexWeight[i] = 1;
            }
        }

        Debug.Log($"Poids calculés pour {vertexWeight.Count} sommets");
    }

    private void calculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[sommets.Count];
        int[] normalCounts = new int[sommets.Count];

        // Calculer les normales par facette et les accumuler
        for (int i = 0; i < facettes.Count; i++)
        {
            int[] face = facettes[i];

            Vector3 v0 = sommets[face[0]];
            Vector3 v1 = sommets[face[1]];
            Vector3 v2 = sommets[face[2]];

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 faceNormal = Vector3.Cross(edge1, edge2);

            // Accumuler la normale pour chaque sommet de la face
            for (int j = 0; j < face.Length; j++)
            {
                int idx = face[j];
                vertexNormals[idx] += faceNormal;
                normalCounts[idx]++;
            }
        }

        // Moyenner et normaliser
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

        Debug.Log($"Normales recalculées : {normales.Count}");
    }

    private void OnDrawGizmos()
    {
        // Bounds est un struct, donc on vérifie si la taille est non-nulle
        if (box.size != Vector3.zero && drawGizmos)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(box.center, box.size);

            foreach (var cluster in clusterGrid)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(cluster.bounds.center, cluster.bounds.size);
            }
        }
    }
}
public class Cluster
{
    public Bounds bounds;
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> vertexIndices = new List<int>(); // Pour garder les indices originaux
    public Vector3 finalVertice;
}