using UnityEngine;
using System.Collections.Generic;

public class VoxelVolume : MonoBehaviour
{
    public Vector3 center = Vector3.zero;
    public Vector3 size = Vector3.one * 4f;
    public float voxelSize = 0.25f;
    public float threshold = 0.5f;
    public GameObject voxelPrefab;
    public bool autoRebuild = true;

    [Header("Debug")]
    public bool debugRaycast = false;
    public bool logAddPotential = false;

    int nx, ny, nz;
    float[] potentials;
    List<GameObject> instances = new List<GameObject>();

    // helper : voxel size in world units (use average scale)
    float GetWorldVoxelSize()
    {
        Vector3 ls = transform.lossyScale;
        float avg = (Mathf.Abs(ls.x) + Mathf.Abs(ls.y) + Mathf.Abs(ls.z)) / 3f;
        return Mathf.Max(1e-6f, voxelSize * avg);
    }

    Bounds GetWorldBounds()
    {
        Vector3 worldCenter = transform.TransformPoint(center);
        Vector3 worldSize = Vector3.Scale(size, transform.lossyScale);
        return new Bounds(worldCenter, worldSize);
    }

    public void InitGrid()
    {
        // compute grid resolution in world space so mapping ray->grid is consistent
        float wv = GetWorldVoxelSize();
        Vector3 worldSize = Vector3.Scale(size, transform.lossyScale);
        nx = Mathf.Max(1, Mathf.CeilToInt(worldSize.x / wv));
        ny = Mathf.Max(1, Mathf.CeilToInt(worldSize.y / wv));
        nz = Mathf.Max(1, Mathf.CeilToInt(worldSize.z / wv));
        potentials = new float[nx * ny * nz];
        if (autoRebuild) RebuildVisualization();
    }

    int Index(int x, int y, int z) => x + nx * (y + ny * z);

    public bool WorldToGrid(Vector3 world, out int gx, out int gy, out int gz)
    {
        Bounds wb = GetWorldBounds();
        Vector3 min = wb.min;
        float wv = GetWorldVoxelSize();
        Vector3 rel = world - min;
        gx = Mathf.FloorToInt(rel.x / wv);
        gy = Mathf.FloorToInt(rel.y / wv);
        gz = Mathf.FloorToInt(rel.z / wv);
        if (gx < 0 || gy < 0 || gz < 0 || gx >= nx || gy >= ny || gz >= nz) return false;
        return true;
    }

    public Vector3 GridToWorldCenter(int x, int y, int z)
    {
        Bounds wb = GetWorldBounds();
        Vector3 min = wb.min;
        float wv = GetWorldVoxelSize();
        return min + new Vector3((x + 0.5f) * wv, (y + 0.5f) * wv, (z + 0.5f) * wv);
    }

    // Add potential with simple linear falloff
    public void AddPotential(Vector3 worldPos, float amount, float radius)
    {
        if (potentials == null) InitGrid();
        Bounds wb = GetWorldBounds();
        float wv = GetWorldVoxelSize();

        Vector3 rel = worldPos - wb.min;
        int cx = Mathf.Clamp(Mathf.FloorToInt(rel.x / wv), 0, nx - 1);
        int cy = Mathf.Clamp(Mathf.FloorToInt(rel.y / wv), 0, ny - 1);
        int cz = Mathf.Clamp(Mathf.FloorToInt(rel.z / wv), 0, nz - 1);

        int rx = Mathf.CeilToInt(radius / wv);
        int x0 = Mathf.Max(0, cx - rx);
        int x1 = Mathf.Min(nx - 1, cx + rx);
        int y0 = Mathf.Max(0, cy - rx);
        int y1 = Mathf.Min(ny - 1, cy + rx);
        int z0 = Mathf.Max(0, cz - rx);
        int z1 = Mathf.Min(nz - 1, cz + rx);

        float r2 = radius * radius;
        int modified = 0;
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                for (int z = z0; z <= z1; z++)
                {
                    Vector3 wc = GridToWorldCenter(x, y, z);
                    float d2 = (wc - worldPos).sqrMagnitude;
                    if (d2 > r2) continue;
                    float d = Mathf.Sqrt(d2);
                    float falloff = 1f - (d / radius); // linear falloff
                    potentials[Index(x, y, z)] += amount * falloff;
                    modified++;
                }

        if (logAddPotential)
            Debug.Log($"VoxelVolume.AddPotential at {worldPos:F3}: modified {modified} voxels, amount={amount:F3}, radius={radius:F3}");

        if (autoRebuild) RebuildVisualization();
    }

    public float GetPotentialAtGrid(int x, int y, int z) => potentials[Index(x, y, z)];

    public void ClearVisualization()
    {
        for (int i = instances.Count - 1; i >= 0; i--)
            if (instances[i] != null) DestroyImmediate(instances[i]);
        instances.Clear();
    }

    public void RebuildVisualization()
    {
        ClearVisualization();
        if (voxelPrefab == null) return;
        float wv = GetWorldVoxelSize();
        for (int x = 0; x < nx; x++)
            for (int y = 0; y < ny; y++)
                for (int z = 0; z < nz; z++)
                {
                    float p = GetPotentialAtGrid(x, y, z);
                    if (p > threshold)
                    {
                        Vector3 pos = GridToWorldCenter(x, y, z);
                        GameObject go = Instantiate(voxelPrefab, pos, Quaternion.identity);
                        go.transform.localScale = Vector3.one * Mathf.Max(0.001f, wv * 0.95f);
                        // parent for hierarchy (keep world position)
                        go.transform.SetParent(transform, true);
                        instances.Add(go);
                    }
                }
    }

    // AABB ray intersection (safer): use Bounds.IntersectRay pour éviter divisions par zéro
    public bool RaycastLocal(Ray ray, out Vector3 hitPoint)
    {
        Bounds b = GetWorldBounds();
        if (debugRaycast)
        {
            Debug.Log($"VoxelVolume.RaycastLocal: Ray origin={ray.origin:F3} dir={ray.direction:F3} boundsCenter={b.center:F3} boundsSize={b.size:F3}");
        }

        if (b.IntersectRay(ray, out float t))
        {
            hitPoint = ray.GetPoint(t);
            if (debugRaycast)
                Debug.Log($"VoxelVolume.RaycastLocal: intersect t={t:F3} hit={hitPoint:F3}");
            return true;
        }
        hitPoint = Vector3.zero;
        if (debugRaycast)
            Debug.Log("VoxelVolume.RaycastLocal: no intersection");
        return false;
    }
}