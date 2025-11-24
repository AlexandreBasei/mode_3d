using UnityEngine; // import de l'API Unity (types comme MonoBehaviour, Vector3, Bounds, Gizmos)
using System.Collections.Generic; // import pour List<T>

// classe principale attachable à un GameObject Unity
public class Octree : MonoBehaviour
{
    [System.Serializable]
    public struct Sphere
    {
        public Vector3 center; // centre de la sphère
        public float radius; // rayon de la sphère
    }

    [System.Serializable]
    public struct Cube
    {
        public Vector3 center; // centre du cube (AABB)
        public Vector3 size;   // taille (x,y,z)
        public Bounds ToBounds() => new Bounds(center, size);
    }

    public enum BlendMode
    {
        Union,
        Intersection
    }

    [SerializeField] private List<Sphere> spheres = new List<Sphere>(); // sphères
    [SerializeField] private List<Cube> cubes = new List<Cube>(); // cubes AABB
    [SerializeField] private int octreeDepth = 2; // profondeur de subdivision de l'octree (exposé)
    [SerializeField] private GameObject cubePrefab; // prefab pour instancier des voxels (exposé)
    [SerializeField] private bool instantiatePrefabs = true; // si true, on instancie des prefabs pour chaque feuille occupée
    [SerializeField] private bool drawGizmos = true; // si true, dessiner les gizmos pour debug
    [SerializeField] private Color gizmoColor = Color.yellow; // couleur des gizmos
    [SerializeField] private BlendMode blendMode = BlendMode.Union; // mode de mélange des sphères/cubes
    [SerializeField, Tooltip("Nombre minimal de formes (sphères ou cubes) qui doivent overlap un voxel pour être considéré en intersection (par défaut 2)")]
    private int minIntersectionCount = 2; // <-- paramètre

    private OctreeNode root; // racine de l'octree (type interne)
    private List<Bounds> occupiedLeaves = new List<Bounds>(); // liste des boîtes (feuilles) occupées par la forme

    void Start()
    {
        if ((spheres == null || spheres.Count == 0) && (cubes == null || cubes.Count == 0))
        {
            // exemples par défaut (vous pouvez supprimer)
            spheres = new List<Sphere>
            {
                new Sphere { center = Vector3.zero, radius = 2f },
                new Sphere { center = new Vector3(0f, 2f, 1f), radius = 1f },
                new Sphere { center = new Vector3(0f, -1f, -1f), radius = 2f }
            };
            cubes = new List<Cube>
            {
                new Cube { center = new Vector3(2.5f,-1f,0f), size = Vector3.one * 1.0f }
            };
        }

        BuildOctree(); // construction initiale de l'octree au démarrage
    }

    void Update() { }

    void BuildOctree()
    {
        int depth = Mathf.Clamp(octreeDepth, 0, 10); // clamp
        int n = 1 << depth;
        if (n <= 0) { occupiedLeaves.Clear(); root = null; return; }

        // construire la boîte englobante qui couvre toutes les sphères et cubes
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        foreach (var s in spheres)
        {
            Vector3 sMin = s.center - Vector3.one * s.radius;
            Vector3 sMax = s.center + Vector3.one * s.radius;
            min = Vector3.Min(min, sMin);
            max = Vector3.Max(max, sMax);
        }

        foreach (var c in cubes)
        {
            Bounds cb = c.ToBounds();
            min = Vector3.Min(min, cb.min);
            max = Vector3.Max(max, cb.max);
        }

        Vector3 boxCenter = (min + max) * 0.5f;
        Vector3 boxSize = max - min;
        boxSize = Vector3.Max(boxSize, Vector3.one * 0.0001f);

        Bounds box = new Bounds(boxCenter, boxSize);
        root = new OctreeNode(box);
        occupiedLeaves.Clear();

        root.SubdivideRecursive(spheres, cubes, depth, occupiedLeaves, blendMode, minIntersectionCount);

        if (instantiatePrefabs)
        {
            ClearChildren();
            InstantiateLeaves();
        }

        Debug.Log($"Octree build: depth={depth}, leaf voxels (occupied)={occupiedLeaves.Count}, blendMode={blendMode}, minIntersect={minIntersectionCount}");
    }

    void ClearChildren()
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform t in transform) children.Add(t);
        for (int i = 0; i < children.Count; i++) Destroy(children[i].gameObject);
    }

    void InstantiateLeaves()
    {
        if (cubePrefab == null) return;
        foreach (var b in occupiedLeaves)
        {
            GameObject go = Instantiate(cubePrefab, b.center, Quaternion.identity, transform);
            go.transform.localScale = b.size;
        }
    }

    void OnDrawGizmos()
    {
        if (spheres != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var s in spheres) Gizmos.DrawWireSphere(s.center, s.radius);
        }
        if (cubes != null)
        {
            Gizmos.color = Color.magenta;
            foreach (var c in cubes) Gizmos.DrawWireCube(c.center, c.size);
        }

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
        public Bounds bounds;
        public OctreeNode[] children;

        public OctreeNode(Bounds b) { bounds = b; }

        public void SubdivideRecursive(List<Sphere> spheres, List<Cube> cubes, int depthRemaining, List<Bounds> outOccupiedLeaves, BlendMode blendMode, int minIntersectionCount)
        {
            if (blendMode == BlendMode.Union)
            {
                if (BoxFullyInsideAnyShape(bounds, spheres, cubes))
                {
                    outOccupiedLeaves.Add(bounds);
                    return;
                }

                if (BoxFullyOutsideAllShapes(bounds, spheres, cubes))
                {
                    return;
                }

                if (depthRemaining == 0)
                {
                    outOccupiedLeaves.Add(bounds);
                    return;
                }
            }
            else // Intersection (>= minIntersectionCount)
            {
                int possible = CountIntersectingShapes(bounds, spheres, cubes);
                if (possible < minIntersectionCount) return;

                if (BoxFullyInsideAtLeastK(bounds, spheres, cubes, minIntersectionCount))
                {
                    outOccupiedLeaves.Add(bounds);
                    return;
                }

                if (depthRemaining == 0)
                {
                    if (BoxIntersectsAtLeastK(bounds, spheres, cubes, minIntersectionCount) || BoxCenterInAtLeastK(bounds, spheres, cubes, minIntersectionCount))
                        outOccupiedLeaves.Add(bounds);
                    return;
                }
            }

            children = new OctreeNode[8];
            Vector3 size = bounds.size * 0.5f;
            Vector3 min = bounds.min;

            int idx = 0;
            for (int x = 0; x <= 1; x++)
                for (int y = 0; y <= 1; y++)
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 cmin = min + Vector3.Scale(new Vector3(x, y, z), size);
                        Bounds b = new Bounds(cmin + size * 0.5f, size);
                        children[idx++] = new OctreeNode(b);
                    }

            foreach (var child in children)
                child.SubdivideRecursive(spheres, cubes, depthRemaining - 1, outOccupiedLeaves, blendMode, minIntersectionCount);
        }

        // ---- helpers that consider both spheres and cubes ----

        private static int CountIntersectingShapes(Bounds b, List<Sphere> spheres, List<Cube> cubes)
        {
            int c = 0;
            if (spheres != null)
            {
                foreach (var s in spheres)
                {
                    Vector3 closest = b.ClosestPoint(s.center);
                    if ((closest - s.center).sqrMagnitude <= s.radius * s.radius) c++;
                }
            }
            if (cubes != null)
            {
                foreach (var cube in cubes)
                {
                    if (b.Intersects(cube.ToBounds())) c++;
                }
            }
            return c;
        }

        private static bool BoxIntersectsAtLeastK(Bounds b, List<Sphere> spheres, List<Cube> cubes, int k)
        {
            int c = 0;
            if (spheres != null)
            {
                foreach (var s in spheres)
                {
                    Vector3 closest = b.ClosestPoint(s.center);
                    if ((closest - s.center).sqrMagnitude <= s.radius * s.radius)
                    {
                        c++; if (c >= k) return true;
                    }
                }
            }
            if (cubes != null)
            {
                foreach (var cube in cubes)
                {
                    if (b.Intersects(cube.ToBounds()))
                    {
                        c++; if (c >= k) return true;
                    }
                }
            }
            return false;
        }

        private static bool BoxCenterInAtLeastK(Bounds b, List<Sphere> spheres, List<Cube> cubes, int k)
        {
            Vector3 center = b.center;
            int c = 0;
            if (spheres != null)
            {
                foreach (var s in spheres)
                {
                    if ((center - s.center).sqrMagnitude <= s.radius * s.radius)
                    {
                        c++; if (c >= k) return true;
                    }
                }
            }
            if (cubes != null)
            {
                foreach (var cube in cubes)
                {
                    if (cube.ToBounds().Contains(center))
                    {
                        c++; if (c >= k) return true;
                    }
                }
            }
            return false;
        }

        private static bool BoxFullyInsideAtLeastK(Bounds b, List<Sphere> spheres, List<Cube> cubes, int k)
        {
            int c = 0;
            Vector3 min = b.min;
            Vector3 max = b.max;

            if (spheres != null)
            {
                foreach (var s in spheres)
                {
                    float r2 = s.radius * s.radius;
                    bool allIn = true;
                    for (int xi = 0; xi <= 1 && allIn; xi++)
                        for (int yi = 0; yi <= 1 && allIn; yi++)
                            for (int zi = 0; zi <= 1; zi++)
                            {
                                Vector3 corner = new Vector3(xi == 0 ? min.x : max.x, yi == 0 ? min.y : max.y, zi == 0 ? min.z : max.z);
                                if ((corner - s.center).sqrMagnitude > r2) { allIn = false; break; }
                            }
                    if (allIn) { c++; if (c >= k) return true; }
                }
            }

            if (cubes != null)
            {
                foreach (var cube in cubes)
                {
                    Bounds cb = cube.ToBounds();
                    if (cb.min.x <= min.x && cb.min.y <= min.y && cb.min.z <= min.z &&
                        cb.max.x >= max.x && cb.max.y >= max.y && cb.max.z >= max.z)
                    {
                        c++; if (c >= k) return true;
                    }
                }
            }

            return false;
        }

        private static bool BoxFullyInsideAnyShape(Bounds b, List<Sphere> spheres, List<Cube> cubes)
        {
            if (spheres != null)
            {
                foreach (var s in spheres)
                {
                    float r2 = s.radius * s.radius;
                    Vector3 min = b.min; Vector3 max = b.max;
                    bool allIn = true;
                    for (int xi = 0; xi <= 1 && allIn; xi++)
                        for (int yi = 0; yi <= 1 && allIn; yi++)
                            for (int zi = 0; zi <= 1; zi++)
                            {
                                Vector3 corner = new Vector3(xi == 0 ? min.x : max.x, yi == 0 ? min.y : max.y, zi == 0 ? min.z : max.z);
                                if ((corner - s.center).sqrMagnitude > r2) { allIn = false; break; }
                            }
                    if (allIn) return true;
                }
            }

            if (cubes != null)
            {
                foreach (var cube in cubes)
                {
                    Bounds cb = cube.ToBounds();
                    if (cb.min.x <= b.min.x && cb.min.y <= b.min.y && cb.min.z <= b.min.z &&
                        cb.max.x >= b.max.x && cb.max.y >= b.max.y && cb.max.z >= b.max.z)
                        return true;
                }
            }

            return false;
        }

        private static bool BoxFullyOutsideAllShapes(Bounds b, List<Sphere> spheres, List<Cube> cubes)
        {
            if (spheres != null)
            {
                foreach (var s in spheres)
                {
                    Vector3 closest = b.ClosestPoint(s.center);
                    if ((closest - s.center).sqrMagnitude <= s.radius * s.radius) return false;
                }
            }
            if (cubes != null)
            {
                foreach (var cube in cubes)
                {
                    if (b.Intersects(cube.ToBounds())) return false;
                }
            }
            return true;
        }

        // kept older helpers for completeness (not all used)
        private static bool BoxFullyOutsideAnySphere(Bounds b, List<Sphere> spheres)
        {
            if (spheres == null) return false;
            foreach (var s in spheres)
            {
                Vector3 closest = b.ClosestPoint(s.center);
                float dist2 = (closest - s.center).sqrMagnitude;
                if (dist2 > s.radius * s.radius) return true;
            }
            return false;
        }

        private static bool BoxIntersectsAllSpheres(Bounds b, List<Sphere> spheres)
        {
            if (spheres == null) return true;
            foreach (var s in spheres)
            {
                Vector3 closest = b.ClosestPoint(s.center);
                float dist2 = (closest - s.center).sqrMagnitude;
                if (dist2 > s.radius * s.radius) return false;
            }
            return true;
        }

        private static bool BoxCenterInsideAllSpheres(Bounds b, List<Sphere> spheres)
        {
            if (spheres == null) return true;
            Vector3 center = b.center;
            foreach (var s in spheres)
            {
                float r2 = s.radius * s.radius;
                if ((center - s.center).sqrMagnitude > r2) return false;
            }
            return true;
        }
    }
}
