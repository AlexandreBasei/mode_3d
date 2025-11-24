using UnityEngine;

public class VoxelBrush : MonoBehaviour
{
    public VoxelVolume volume;
    public float radius = 0.5f;
    public float amountPerSecond = 1f;
    public enum Mode { Add, Remove }
    public Mode mode = Mode.Add;
    public KeyCode toggleModeKey = KeyCode.Space; // basculer si besoin

    [Header("Debug")]
    public Camera debugCamera; // si null, on utilisera Camera.main
    public bool drawRay = true;
    public bool logOnClick = true;

    // last ray/hit for gizmo drawing
    Vector3 lastRayOrigin;
    Vector3 lastRayDir;
    bool hasLastHit = false;
    Vector3 lastHitPoint;

    void Update()
    {
        if (volume == null) return;

        if (Input.GetKeyDown(toggleModeKey))
            mode = mode == Mode.Add ? Mode.Remove : Mode.Add;

        Camera cam = debugCamera != null ? debugCamera : Camera.main;
        if (cam == null) return;

        // draw debug ray each frame when mouse held
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (drawRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * 50f, Color.yellow, 0.1f);
            }

            if (volume.RaycastLocal(ray, out Vector3 hit))
            {
                hasLastHit = true;
                lastHitPoint = hit;
                lastRayOrigin = ray.origin;
                lastRayDir = ray.direction;

                // compute sign & amount
                float sign = (Input.GetMouseButton(1) ^ (mode == Mode.Remove)) ? -1f : 1f;
                float amount = amountPerSecond * Time.deltaTime * sign;
                volume.AddPotential(hit, amount, radius);

                if (logOnClick && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
                    Debug.Log($"VoxelBrush: Ray hit volume at {hit:F3}, amount={amount:F3}, radius={radius}");
            }
            else
            {
                hasLastHit = false;
                if (logOnClick && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
                    Debug.Log("VoxelBrush: Ray did NOT hit the volume.");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!drawRay) return;
        Camera cam = debugCamera != null ? debugCamera : Camera.main;
        if (cam == null) return;

        // draw last ray and hit
        if (hasLastHit)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(lastRayOrigin, lastHitPoint);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastHitPoint, Mathf.Max(0.01f, radius * 0.1f));
        }
    }
}