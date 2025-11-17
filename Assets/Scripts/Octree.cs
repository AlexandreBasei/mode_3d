using UnityEngine; // import de l'API Unity (types comme MonoBehaviour, Vector3, Bounds, Gizmos)
using System.Collections.Generic; // import pour List<T>

// classe principale attachable à un GameObject Unity
public class Octree : MonoBehaviour
{
    //Origine
    //Rayon
    //Boîte avec une profondeur
    //Position de chaque cube

    [System.Serializable]
    public struct Sphere
    {
        public Vector3 center; // centre de la sphère
        public float radius; // rayon de la sphère
    }

    [SerializeField] private List<Sphere> spheres = new List<Sphere>(); // liste des sphères à représenter (exposé dans l'inspecteur)
    [SerializeField] private int octreeDepth = 2; // profondeur de subdivision de l'octree (exposé)
    [SerializeField] private GameObject cubePrefab; // prefab pour instancier des voxels (exposé)
    [SerializeField] private bool instantiatePrefabs = true; // si true, on instancie des prefabs pour chaque feuille occupée
    [SerializeField] private bool drawGizmos = true; // si true, dessiner les gizmos pour debug
    [SerializeField] private Color gizmoColor = Color.yellow; // couleur des gizmos

    private OctreeNode root; // racine de l'octree (type interne)
    private List<Bounds> occupiedLeaves = new List<Bounds>(); // liste des boîtes (feuilles) occupées par la sphère


    void Start()
    {
        // si aucune sphère configurée dans l'inspecteur, en ajouter deux par défaut
        if (spheres == null || spheres.Count == 0)
        {
            spheres = new List<Sphere>
            {
                new Sphere { center = Vector3.zero, radius = 2f },
                new Sphere { center = new Vector3(2.5f, 0f, 0f), radius = 1.5f }
            };
        }

        BuildOctree(); // construction initiale de l'octree au démarrage
    }

    // Update is called once per frame
    void Update()
    {
        // méthode vide pour l'instant (possibilité de rebuild dynamique ici)
    }

    void BuildOctree()
    {
        int depth = Mathf.Clamp(octreeDepth, 0, 8); // clamp de la profondeur pour éviter explosion mémoire
        int n = 1 << depth; // n = 2^depth (nombre de subdivisions par axe si on utilisait grille)
        if (n <= 0) { occupiedLeaves.Clear(); root = null; return; } // garde-fou si profondeur invalide

        // construire la boîte englobante qui couvre toutes les sphères (centre + rayon)
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        foreach (var sphere in spheres)
        {
            Vector3 sMin = sphere.center - Vector3.one * sphere.radius; // coin min de la sphère
            Vector3 sMax = sphere.center + Vector3.one * sphere.radius; // coin max de la sphère

            min = Vector3.Min(min, sMin); // mettre à jour le min global
            max = Vector3.Max(max, sMax); // mettre à jour le max global
        }

        Vector3 boxCenter = (min + max) * 0.5f; // centre de la boîte englobante
        Vector3 boxSize = max - min; // taille de la boîte englobante
        // si size très petit, garantir une taille minimale
        boxSize = Vector3.Max(boxSize, Vector3.one * 0.0001f);

        Bounds box = new Bounds(boxCenter, boxSize); // boîte englobante centrée sur boxCenter
        root = new OctreeNode(box); // création du noeud racine avec cette boîte
        occupiedLeaves.Clear(); // vider la liste des feuilles occupées avant reconstruction

        root.SubdivideRecursive(spheres, depth, occupiedLeaves); // lancer la subdivision récursive

        if (instantiatePrefabs)
        {
            ClearChildren(); // supprimer enfants actuels dans la hiérarchie
            InstantiateLeaves(); // instancier les prefabs pour chaque feuille occupée
        }

        Debug.Log($"Octree build: depth={depth}, leaf voxels (occupied)={occupiedLeaves.Count}"); // log résumé
    }

    void ClearChildren()
    {
        List<Transform> children = new List<Transform>(); // liste temporaire pour éviter modification pendant itération

        foreach (Transform t in transform) children.Add(t); // collecter tous les enfants actuels
        for (int i = 0; i < children.Count; i++) Destroy(children[i].gameObject); // détruire chaque enfant
    }

    void InstantiateLeaves()
    {
        if (cubePrefab == null) return; // rien à faire si pas de prefab assigné
        foreach (var b in occupiedLeaves)
        {
            GameObject go = Instantiate(cubePrefab, b.center, Quaternion.identity, transform); // corrigé : appeler Instantiate au lieu d'InstantiateLeaves
            go.transform.localScale = b.size; // définir l'échelle du prefab pour correspondre à la boîte
        }
    }

    void OnDrawGizmos()
    {
        if (spheres != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var s in spheres) Gizmos.DrawWireSphere(s.center, s.radius);
        }

        // éviter NullReferenceException si l'octree n'est pas encore construit
        if (root != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(root.bounds.center, root.bounds.size);
        }

        if (!drawGizmos || occupiedLeaves == null) return;
        Gizmos.color = gizmoColor;
        foreach (var b in occupiedLeaves) Gizmos.DrawWireCube(b.center, b.size);
    }

    private class OctreeNode
    {
        public Bounds bounds; // boîte englobante de ce noeud
        public OctreeNode[] children; // enfants (8) si subdivisé

        public OctreeNode(Bounds b) { bounds = b; } // constructeur simple qui stocke la boîte

        public void SubdivideRecursive(List<Sphere> spheres, int depthRemaining, List<Bounds> outOccupiedLeaves)
        {
            if (BoxFullyInsideAnySphere(bounds, spheres))
            {
                //fully inside => this entire node is occupied
                outOccupiedLeaves.Add(bounds); // si la boîte est entièrement dans la sphère, marquer occupée et arrêter
                return;
            }

            if (BoxFullyOutsideAllSphere(bounds, spheres))
            {
                //fully outside => nothing to do
                return; // si complètement en dehors, ne rien ajouter
            }

            if (depthRemaining == 0)
            {
                //leaf but intersects sphere => treat as occupied (surface voxel)
                outOccupiedLeaves.Add(bounds); // si profondeur atteinte et intersection, considérer feuille occupée
                return;
            }

            //subdivide
            children = new OctreeNode[8]; // allouer tableau pour 8 enfants
            Vector3 size = bounds.size * 0.5f; // taille des enfants = moitié de la taille du parent
            Vector3 min = bounds.min; // coin minimum de la boîte parent

            int idx = 0;
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 cmin = min + Vector3.Scale(new Vector3(x, y, z), size); // coin min du sous-voxel
                        Bounds b = new Bounds(cmin + size * 0.5f, size); // calcul de la boîte enfant
                        children[idx++] = new OctreeNode(b); // créer l'enfant avec sa boîte
                    }
                }
            }

            foreach (var child in children)
            {
                child.SubdivideRecursive(spheres, depthRemaining - 1, outOccupiedLeaves); // récurse pour chaque enfant
            }
        }

        //test si la boîte est complètement hors de la sphère
        private static bool BoxFullyOutsideAllSphere(Bounds b, List<Sphere> spheres)
        {
            foreach (var s in spheres)
            {
                Vector3 closest = b.ClosestPoint(s.center); // point le plus proche dans la boîte par rapport au centre de la sphère
                float dist2 = (closest - s.center).sqrMagnitude; // distance^2 entre centre de sphère et point le plus proche
                if (dist2 <= s.radius * s.radius) return false; // si distance^2 <= rayon^2, la boîte intersecte la sphère
            }

            return true; // si aucune sphère n'intersecte, la boîte est complètement en dehors
        }

        private static bool BoxFullyInsideAnySphere(Bounds b, List<Sphere> spheres)
        {
            // vérifier pour chaque sphère si les 8 coins sont à l'intérieur
            foreach (var s in spheres)
            {
                float r2 = s.radius * s.radius;
                Vector3 min = b.min;
                Vector3 max = b.max;
                bool allIn = true;
                for (int xi = 0; xi <= 1 && allIn; xi++)
                {
                    for (int yi = 0; yi <= 1 && allIn; yi++)
                    {
                        for (int zi = 0; zi <= 1; zi++)
                        {
                            Vector3 corner = new Vector3(xi == 0 ? min.x : max.x, yi == 0 ? min.y : max.y, zi == 0 ? min.z : max.z);
                            if ((corner - s.center).sqrMagnitude > r2) { allIn = false; break; }
                        }
                    }
                }
                if (allIn) return true; // cette boîte est entièrement contenue dans la sphère s
            }
            return false;
        }
    }
}
